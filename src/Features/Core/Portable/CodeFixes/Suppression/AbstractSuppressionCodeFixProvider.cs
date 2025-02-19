﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        public const string SuppressMessageAttributeName = "System.Diagnostics.CodeAnalysis.SuppressMessageAttribute";
        private static readonly string s_globalSuppressionsFileName = "GlobalSuppressions";
        private static readonly string s_suppressionsFileCommentTemplate =
@"
{0} This file is used by Code Analysis to maintain SuppressMessage 
{0} attributes that are applied to this project.
{0} Project-level suppressions either have no target or are given 
{0} a specific target and scoped to a namespace, type, member, etc.

";
        protected AbstractSuppressionCodeFixProvider()
        {
        }

        public FixAllProvider GetFixAllProvider()
        {
            return SuppressionFixAllProvider.Instance;
        }

        public bool CanBeSuppressedOrUnsuppressed(Diagnostic diagnostic)
        {
            return SuppressionHelpers.CanBeSuppressed(diagnostic) || SuppressionHelpers.CanBeUnsuppressed(diagnostic);
        }

        protected abstract SyntaxTriviaList CreatePragmaDisableDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine);
        protected abstract SyntaxTriviaList CreatePragmaRestoreDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine);

        protected abstract SyntaxNode AddGlobalSuppressMessageAttribute(SyntaxNode newRoot, ISymbol targetSymbol, Diagnostic diagnostic);

        protected abstract string DefaultFileExtension { get; }
        protected abstract string SingleLineCommentStart { get; }
        protected abstract bool IsAttributeListWithAssemblyAttributes(SyntaxNode node);
        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);
        protected abstract bool IsEndOfFileToken(SyntaxToken token);
        protected abstract bool IsSingleAttributeInAttributeList(SyntaxNode attribute);
        protected abstract bool IsAnyPragmaDirectiveForId(SyntaxTrivia trivia, string id, out bool enableDirective, out bool hasMultipleIds);
        protected abstract SyntaxTrivia TogglePragmaDirective(SyntaxTrivia trivia);

        protected string GlobalSuppressionsFileHeaderComment
        {
            get
            {
                return string.Format(s_suppressionsFileCommentTemplate, this.SingleLineCommentStart);
            }
        }

        protected virtual SyntaxToken GetAdjustedTokenForPragmaDisable(SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
        {
            return token;
        }

        protected virtual SyntaxToken GetAdjustedTokenForPragmaRestore(SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
        {
            return token;
        }

        public Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            return GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: false, skipUnsuppress: false, cancellationToken: cancellationToken);
        }

        internal async Task<IEnumerable<PragmaWarningCodeAction>> GetPragmaSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var codeFixes = await GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: true, skipUnsuppress: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return codeFixes.SelectMany(fix => fix.Action.GetCodeActions()).OfType<PragmaWarningCodeAction>();
        }

        private async Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, bool skipSuppressMessage, bool skipUnsuppress, CancellationToken cancellationToken)
        {
            // We only care about diagnostics that can be suppressed/unsuppressed.
            diagnostics = diagnostics.Where(CanBeSuppressedOrUnsuppressed);
            if (diagnostics.IsEmpty())
            {
                return SpecializedCollections.EmptyEnumerable<CodeFix>();
            }

            var suppressionTargetInfo = await GetSuppressionTargetInfoAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (suppressionTargetInfo == null)
            {
                return SpecializedCollections.EmptyEnumerable<CodeFix>();
            }

            if (!skipSuppressMessage)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var suppressMessageAttribute = semanticModel.Compilation.SuppressMessageAttributeType();
                skipSuppressMessage = suppressMessageAttribute == null || !suppressMessageAttribute.IsAttribute();
            }

            var result = new List<CodeFix>();
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.IsSuppressed)
                {
                    var nestedActions = new List<NestedSuppressionCodeAction>();

                    // pragma warning disable.
                    nestedActions.Add(new PragmaWarningCodeAction(suppressionTargetInfo, document, diagnostic, this));

                    // SuppressMessageAttribute suppression is not supported for compiler diagnostics.
                    if (!skipSuppressMessage && !SuppressionHelpers.IsCompilerDiagnostic(diagnostic))
                    {
                        // global assembly-level suppress message attribute.
                        nestedActions.Add(new GlobalSuppressMessageCodeAction(suppressionTargetInfo.TargetSymbol, document.Project, diagnostic, this));
                    }

                    result.Add(new CodeFix(new SuppressionCodeAction(diagnostic, nestedActions), diagnostic));
                }
                else if (!skipUnsuppress)
                {
                    var codeAcion = await RemoveSuppressionCodeAction.CreateAsync(suppressionTargetInfo, document, diagnostic, this, cancellationToken).ConfigureAwait(false);
                    result.Add(new CodeFix(codeAcion, diagnostic));
                }
            }

            return result;
        }

        internal class SuppressionTargetInfo
        {
            public ISymbol TargetSymbol { get; set; }
            public SyntaxToken StartToken { get; set; }
            public SyntaxToken EndToken { get; set; }
            public SyntaxNode NodeWithTokens { get; set; }
        }

        private async Task<SuppressionTargetInfo> GetSuppressionTargetInfoAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.GetLineVisibility(span.Start, cancellationToken) == LineVisibility.Hidden)
            {
                return null;
            }

            // Find the start token to attach leading pragma disable warning directive.
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxTrivia containingTrivia = root.FindTrivia(span.Start);
            var lines = syntaxTree.GetText(cancellationToken).Lines;
            int indexOfLine;
            if (containingTrivia == default(SyntaxTrivia))
            {
                indexOfLine = lines.IndexOf(span.Start);
            }
            else
            {
                indexOfLine = lines.IndexOf(containingTrivia.Token.SpanStart);
            }

            var lineAtPos = lines[indexOfLine];
            var startToken = root.FindToken(lineAtPos.Start);
            startToken = GetAdjustedTokenForPragmaDisable(startToken, root, lines, indexOfLine);

            // Find the end token to attach pragma restore warning directive.
            // This should be the last token on the line that contains the start token.
            indexOfLine = lines.IndexOf(startToken.Span.End);
            lineAtPos = lines[indexOfLine];
            var endToken = root.FindToken(lineAtPos.End);
            endToken = GetAdjustedTokenForPragmaRestore(endToken, root, lines, indexOfLine);

            var nodeWithTokens = GetNodeWithTokens(startToken, endToken, root);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            ISymbol targetSymbol = null;
            var targetMemberNode = syntaxFacts.GetContainingMemberDeclaration(root, startToken.SpanStart);
            if (targetMemberNode != null)
            {
                targetSymbol = semanticModel.GetDeclaredSymbol(targetMemberNode, cancellationToken);

                if (targetSymbol == null)
                {
                    var analyzerDriverService = document.GetLanguageService<IAnalyzerDriverService>();

                    // targetMemberNode could be a declaration node with multiple decls (e.g. field declaration defining multiple variables).
                    // Let us compute all the declarations intersecting the span.
                    var decls = new List<DeclarationInfo>();
                    analyzerDriverService.ComputeDeclarationsInSpan(semanticModel, span, true, decls, cancellationToken);
                    if (decls.Any())
                    {
                        var containedDecls = decls.Where(d => span.Contains(d.DeclaredNode.Span));
                        if (containedDecls.Count() == 1)
                        {
                            // Single containing declaration, use this symbol.
                            var decl = containedDecls.Single();
                            targetSymbol = decl.DeclaredSymbol;
                        }
                        else
                        {
                            // Otherwise, use the most enclosing declaration.
                            TextSpan? minContainingSpan = null;
                            foreach (var decl in decls)
                            {
                                var declSpan = decl.DeclaredNode.Span;
                                if (declSpan.Contains(span) &&
                                    (!minContainingSpan.HasValue || minContainingSpan.Value.Contains(declSpan)))
                                {
                                    minContainingSpan = declSpan;
                                    targetSymbol = decl.DeclaredSymbol;
                                }
                            }
                        }
                    }
                }
            }

            if (targetSymbol == null)
            {
                // Outside of a member declaration, suppress diagnostic for the entire assembly.
                targetSymbol = semanticModel.Compilation.Assembly;
            }

            return new SuppressionTargetInfo() { TargetSymbol = targetSymbol, NodeWithTokens = nodeWithTokens, StartToken = startToken, EndToken = endToken };
        }

        internal SyntaxNode GetNodeWithTokens(SyntaxToken startToken, SyntaxToken endToken, SyntaxNode root)
        {
            if (IsEndOfFileToken(endToken))
            {
                return root;
            }
            else
            {
                return startToken.GetCommonRoot(endToken);
            }
        }

        protected string GetScopeString(SymbolKind targetSymbolKind)
        {
            switch (targetSymbolKind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    return "member";

                case SymbolKind.NamedType:
                    return "type";

                case SymbolKind.Namespace:
                    return "namespace";

                default:
                    return null;
            }
        }

        protected string GetTargetString(ISymbol targetSymbol)
        {
            return "~" + DocumentationCommentId.CreateDeclarationId(targetSymbol);
        }
    }
}
