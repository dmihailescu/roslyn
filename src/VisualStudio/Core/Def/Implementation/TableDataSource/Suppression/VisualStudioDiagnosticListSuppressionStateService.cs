﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Service to maintain information about the suppression state of specific set of items in the error list.
    /// </summary>
    [Export(typeof(IVisualStudioDiagnosticListSuppressionStateService))]
    internal class VisualStudioDiagnosticListSuppressionStateService : IVisualStudioDiagnosticListSuppressionStateService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IVsUIShell _shellService;
        private readonly IWpfTableControl _tableControl;

        private int _selectedActiveItems;
        private int _selectedSuppressedItems;
        private int _selectedRoslynItems;
        private int _selectedCompilerDiagnosticItems;
        private int _selectedNonSuppressionStateItems;

        private const string SynthesizedFxCopDiagnostic = "SynthesizedFxCopDiagnostic";
        private readonly string[] SynthesizedFxCopDiagnosticCustomTags = new string[] { SynthesizedFxCopDiagnostic };

        [ImportingConstructor]
        public VisualStudioDiagnosticListSuppressionStateService(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _shellService = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
            var errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            _tableControl = errorList?.TableControl;

            ClearState();
            InitializeFromTableControlIfNeeded();
        }

        private int SelectedItems => _selectedActiveItems + _selectedSuppressedItems + _selectedNonSuppressionStateItems;

        // If we can suppress either in source or in suppression file, we enable suppress context menu.
        public bool CanSuppressSelectedEntries => CanSuppressSelectedEntriesInSource || CanSuppressSelectedEntriesInSuppressionFiles;

        // If only suppressed items are selected, we enable remove suppressions.
        public bool CanRemoveSuppressionsSelectedEntries => _selectedActiveItems == 0 && _selectedSuppressedItems > 0;

        // If only Roslyn active items are selected, we enable suppress in source.
        public bool CanSuppressSelectedEntriesInSource => _selectedActiveItems > 0 &&
            _selectedSuppressedItems == 0 &&
            _selectedRoslynItems == _selectedActiveItems;

        // If only active items are selected, and there is at least one Roslyn item, we enable suppress in suppression file.
        // Also, compiler diagnostics cannot be suppressed in suppression file, so there must be at least one non-compiler item.
        public bool CanSuppressSelectedEntriesInSuppressionFiles => _selectedActiveItems > 0 &&
            _selectedSuppressedItems == 0 &&
            (_selectedRoslynItems - _selectedCompilerDiagnosticItems) > 0;

        private void ClearState()
        {
            _selectedActiveItems = 0;
            _selectedSuppressedItems = 0;
            _selectedRoslynItems = 0;
            _selectedCompilerDiagnosticItems = 0;
            _selectedNonSuppressionStateItems = 0;
        }

        private void InitializeFromTableControlIfNeeded()
        {
            if (_tableControl == null)
            {
                return;
            }

            if (SelectedItems == _tableControl.SelectedEntries.Count())
            {
                // We already have up-to-date state data, so don't need to re-compute.
                return;
            }

            ClearState();
            if (ProcessEntries(_tableControl.SelectedEntries, added: true))
            {
                UpdateQueryStatus();
            }
        }

        /// <summary>
        /// Updates suppression state information when the selected entries change in the error list.
        /// </summary>
        public void ProcessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            var hasAddedSuppressionStateEntry = ProcessEntries(e.AddedEntries, added: true);
            var hasRemovedSuppressionStateEntry = ProcessEntries(e.RemovedEntries, added: false);

            // If any entry that supports suppression state was ever involved, update query status since each item in the error list
            // can have different context menu.
            if (hasAddedSuppressionStateEntry || hasRemovedSuppressionStateEntry)
            {
                UpdateQueryStatus();
            }

            InitializeFromTableControlIfNeeded();
        }

        private bool ProcessEntries(IEnumerable<ITableEntryHandle> entryHandles, bool added)
        {
            bool isRoslynEntry, isSuppressedEntry, isCompilerDiagnosticEntry;
            var hasSuppressionStateEntry = false;
            foreach (var entryHandle in entryHandles)
            {
                if (EntrySupportsSuppressionState(entryHandle, out isRoslynEntry, out isSuppressedEntry, out isCompilerDiagnosticEntry))
                {
                    hasSuppressionStateEntry = true;
                    HandleSuppressionStateEntry(isRoslynEntry, isSuppressedEntry, isCompilerDiagnosticEntry, added);
                }
                else
                {
                    HandleNonSuppressionStateEntry(added);
                }
            }

            return hasSuppressionStateEntry;
        }

        private static bool EntrySupportsSuppressionState(ITableEntryHandle entryHandle, out bool isRoslynEntry, out bool isSuppressedEntry, out bool isCompilerDiagnosticEntry)
        {
            int index;
            var roslynSnapshot = GetEntriesSnapshot(entryHandle, out index);
            if (roslynSnapshot == null)
            {
                isRoslynEntry = false;
                isCompilerDiagnosticEntry = false;
                return IsNonRoslynEntrySupportingSuppressionState(entryHandle, out isSuppressedEntry);
            }

            var diagnosticData = roslynSnapshot?.GetItem(index)?.Primary;
            if (!IsEntryWithConfigurableSuppressionState(diagnosticData))
            {
                isRoslynEntry = false;
                isSuppressedEntry = false;
                isCompilerDiagnosticEntry = false;
                return false;
            }

            isRoslynEntry = true;
            isSuppressedEntry = diagnosticData.IsSuppressed;
            isCompilerDiagnosticEntry = SuppressionHelpers.IsCompilerDiagnostic(diagnosticData);
            return true;
        }

        private static bool IsNonRoslynEntrySupportingSuppressionState(ITableEntryHandle entryHandle, out bool isSuppressedEntry)
        {
            string suppressionStateValue;
            if (entryHandle.TryGetValue(SuppressionStateColumnDefinition.ColumnName, out suppressionStateValue))
            {
                isSuppressedEntry = suppressionStateValue == ServicesVSResources.SuppressionStateSuppressed;
                return true;
            }

            isSuppressedEntry = false;
            return false;
        }

        /// <summary>
        /// Returns true if an entry's suppression state can be modified.
        /// </summary>
        /// <returns></returns>
        private static bool IsEntryWithConfigurableSuppressionState(DiagnosticData entry)
        {
            return entry != null &&
                entry.HasTextSpan &&
                !SuppressionHelpers.IsNotConfigurableDiagnostic(entry);
        }

        private static AbstractTableEntriesSnapshot<DiagnosticData> GetEntriesSnapshot(ITableEntryHandle entryHandle)
        {
            int index;
            return GetEntriesSnapshot(entryHandle, out index);
        }

        private static AbstractTableEntriesSnapshot<DiagnosticData> GetEntriesSnapshot(ITableEntryHandle entryHandle, out int index)
        {
            ITableEntriesSnapshot snapshot;
            if (!entryHandle.TryGetSnapshot(out snapshot, out index))
            {
                return null;
            }

            return snapshot as AbstractTableEntriesSnapshot<DiagnosticData>;
        }

        public bool IsSynthesizedNonRoslynDiagnostic(DiagnosticData diagnostic)
        {
            var tags = diagnostic.CustomTags;
            return tags != null && tags.Contains(SynthesizedFxCopDiagnostic);
        }

        /// <summary>
        /// Gets <see cref="DiagnosticData"/> objects for error list entries, filtered based on the given parameters.
        /// </summary>
        public async Task<ImmutableArray<DiagnosticData>> GetItemsAsync(bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            Dictionary<string, Project> projectNameToProjectMapOpt = null;
            Dictionary<Project, ImmutableDictionary<string, Document>> filePathToDocumentMapOpt = null;

            var entries = selectedEntriesOnly ? _tableControl.SelectedEntries : _tableControl.Entries;
            foreach (var entryHandle in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DiagnosticData diagnosticData = null;
                int index;
                var roslynSnapshot = GetEntriesSnapshot(entryHandle, out index);
                if (roslynSnapshot != null)
                {
                    diagnosticData = roslynSnapshot.GetItem(index)?.Primary;
                }
                else if (!isAddSuppression)
                {
                    // For suppression removal, we also need to handle FxCop entries.
                    bool isSuppressedEntry;
                    if (!IsNonRoslynEntrySupportingSuppressionState(entryHandle, out isSuppressedEntry) ||
                        !isSuppressedEntry)
                    {
                        continue;
                    }

                    string errorCode = null, category = null, message = null, filePath = null, projectName = null;
                    int line = -1; // FxCop only supports line, not column.
                    var location = Location.None;

                    if (entryHandle.TryGetValue(StandardTableColumnDefinitions.ErrorCode, out errorCode) && !string.IsNullOrEmpty(errorCode) &&
                        entryHandle.TryGetValue(StandardTableColumnDefinitions.ErrorCategory, out category) && !string.IsNullOrEmpty(category) &&
                        entryHandle.TryGetValue(StandardTableColumnDefinitions.Text, out message) && !string.IsNullOrEmpty(message) &&
                        entryHandle.TryGetValue(StandardTableColumnDefinitions.ProjectName, out projectName) && !string.IsNullOrEmpty(projectName))
                    {
                        if (projectNameToProjectMapOpt == null)
                        {
                            projectNameToProjectMapOpt = new Dictionary<string, Project>();
                            foreach (var p in _workspace.CurrentSolution.Projects)
                            {
                                projectNameToProjectMapOpt[p.Name] = p;
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        Project project;
                        if (!projectNameToProjectMapOpt.TryGetValue(projectName, out project))
                        {
                            // bail out
                            continue;
                        }

                        Document document = null;
                        var hasLocation = (entryHandle.TryGetValue(StandardTableColumnDefinitions.DocumentName, out filePath) && !string.IsNullOrEmpty(filePath)) &&
                            (entryHandle.TryGetValue(StandardTableColumnDefinitions.Line, out line) && line >= 0);
                        if (hasLocation)
                        {
                            if (string.IsNullOrEmpty(filePath) || line < 0)
                            {
                                // bail out
                                continue;
                            }

                            ImmutableDictionary<string, Document> filePathMap;
                            filePathToDocumentMapOpt = filePathToDocumentMapOpt ?? new Dictionary<Project, ImmutableDictionary<string, Document>>();
                            if (!filePathToDocumentMapOpt.TryGetValue(project, out filePathMap))
                            {
                                filePathMap = await GetFilePathToDocumentMapAsync(project, cancellationToken).ConfigureAwait(false);
                                filePathToDocumentMapOpt[project] = filePathMap;
                            }

                            if (!filePathMap.TryGetValue(filePath, out document))
                            {
                                // bail out
                                continue;
                            }

                            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                            var linePosition = new LinePosition(line, 0);
                            var linePositionSpan = new LinePositionSpan(start: linePosition, end: linePosition);
                            var textSpan = (await tree.GetTextAsync(cancellationToken).ConfigureAwait(false)).Lines.GetTextSpan(linePositionSpan);
                            location = tree.GetLocation(textSpan);
                        }

                        Contract.ThrowIfNull(project);
                        Contract.ThrowIfFalse((document != null) == location.IsInSource);

                        // Create a diagnostic with correct values for fields we care about: id, category, message, isSuppressed, location
                        // and default values for the rest of the fields (not used by suppression fixer).
                        var diagnostic = Diagnostic.Create(
                            id: errorCode,
                            category: category,
                            message: message,
                            severity: DiagnosticSeverity.Warning,
                            defaultSeverity: DiagnosticSeverity.Warning,
                            isEnabledByDefault: true,
                            warningLevel: 1,
                            isSuppressed: isSuppressedEntry,
                            title: message,
                            location: location,
                            customTags: SynthesizedFxCopDiagnosticCustomTags);

                        diagnosticData = document != null ?
                            DiagnosticData.Create(document, diagnostic) :
                            DiagnosticData.Create(project, diagnostic);
                    }
                }

                if (IsEntryWithConfigurableSuppressionState(diagnosticData))
                {
                    var isCompilerDiagnostic = SuppressionHelpers.IsCompilerDiagnostic(diagnosticData);
                    if (onlyCompilerDiagnostics && !isCompilerDiagnostic)
                    {
                        continue;
                    }

                    if (isAddSuppression)
                    {
                        // Compiler diagnostics can only be suppressed in source.
                        if (!diagnosticData.IsSuppressed &&
                            (isSuppressionInSource || !isCompilerDiagnostic))
                        {
                            builder.Add(diagnosticData);
                        }
                    }
                    else if (diagnosticData.IsSuppressed)
                    {
                        builder.Add(diagnosticData);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static async Task<ImmutableDictionary<string, Document>> GetFilePathToDocumentMapAsync(Project project, CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, Document>();
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var filePath = tree.FilePath;
                if (filePath != null)
                {
                    builder.Add(filePath, document);
                }
            }

            return builder.ToImmutable();
        }

        private static void UpdateSelectedItems(bool added, ref int count)
        {
            if (added)
            {
                count++;
            }
            else
            {
                count--;
            }
        }

        private void HandleSuppressionStateEntry(bool isRoslynEntry, bool isSuppressedEntry, bool isCompilerDiagnosticEntry, bool added)
        {
            if (isRoslynEntry)
            {
                UpdateSelectedItems(added, ref _selectedRoslynItems);
            }

            if (isCompilerDiagnosticEntry)
            {
                UpdateSelectedItems(added, ref _selectedCompilerDiagnosticItems);
            }

            if (isSuppressedEntry)
            {
                UpdateSelectedItems(added, ref _selectedSuppressedItems);
            }
            else
            {
                UpdateSelectedItems(added, ref _selectedActiveItems);
            }
        }

        private void HandleNonSuppressionStateEntry(bool added)
        {
            UpdateSelectedItems(added, ref _selectedNonSuppressionStateItems);
        }

        private void UpdateQueryStatus()
        {
            // Force the shell to refresh the QueryStatus for all the command since default behavior is it only does query
            // when focus on error list has changed, not individual items.
            if (_shellService != null)
            {
                _shellService.UpdateCommandUI(0);
            }
        }
    }
}
