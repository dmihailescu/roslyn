﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments
{
    public class DocumentationCommentTests : AbstractDocumentationCommentTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_Class()
        {
            var code =
@"//$$
class C
{
}";

            var expected =
@"/// <summary>
/// $$
/// </summary>
class C
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_Method()
        {
            var code =
@"class C
{
    //$$
    int M<T>(int foo) { return 0; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""foo""></param>
    /// <returns></returns>
    int M<T>(int foo) { return 0; }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_Method_WithVerbatimParams()
        {
            var code =
@"class C
{
    //$$
    int M<@int>(int @foo) { return 0; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""int""></typeparam>
    /// <param name=""foo""></param>
    /// <returns></returns>
    int M<@int>(int @foo) { return 0; }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_AutoProperty()
        {
            var code =
@"class C
{
    //$$
    int P { get; set; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    int P { get; set; }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_Property()
        {
            var code =
@"class C
{
    //$$
    int P
    {
        get { return 0; }
        set { }
    }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    int P
    {
        get { return 0; }
        set { }
    }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_Indexer()
        {
            var code =
@"class C
{
    //$$
    int this[int index]
    {
        get { return 0; }
        set { }
    }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <param name=""index""></param>
    /// <returns></returns>
    int this[int index]
    {
        get { return 0; }
        set { }
    }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_VoidMethod1()
        {
            var code =
@"class C
{
    //$$
    void M<T>(int foo) {  }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""foo""></param>
    void M<T>(int foo) {  }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_VoidMethod_WithVerbatimParams()
        {
            var code =
@"class C
{
    //$$
    void M<@T>(int @int) {  }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""int""></param>
    void M<@T>(int @int) {  }
}";

            VerifyTypingCharacter(code, expected);
        }

        [WorkItem(538699)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_VoidMethod2()
        {
            var code =
@"class C
{
    //$$
    void Method() { }
}";
            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    void Method() { }
}";
            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotWhenDocCommentExists1()
        {
            var code = @"
///
//$$
class C
{
}";

            var expected = @"
///
///$$
class C
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotWhenDocCommentExists2()
        {
            var code = @"
///

//$$
class C
{
}";

            var expected = @"
///

///$$
class C
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotWhenDocCommentExists3()
        {
            var code = @"
class B { } ///

//$$
class C
{
}";

            var expected = @"
class B { } ///

///$$
class C
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotWhenDocCommentExists4()
        {
            var code =
@"//$$
/// <summary></summary>
class C
{
}";

            var expected =
@"///$$
/// <summary></summary>
class C
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotWhenDocCommentExists5()
        {
            var code =
@"class C
{
    //$$
    /// <summary></summary>
    int M<T>(int foo) { return 0; }
}";

            var expected =
@"class C
{
    ///$$
    /// <summary></summary>
    int M<T>(int foo) { return 0; }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotInsideMethodBody1()
        {
            var code =
@"class C
{
    void M(int foo)
    {
      //$$
    }
}";

            var expected =
@"class C
{
    void M(int foo)
    {
      ///$$
    }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotInsideMethodBody2()
        {
            var code =
@"class C
{
    /// <summary></summary>
    void M(int foo)
    {
      //$$
    }
}";

            var expected =
@"class C
{
    /// <summary></summary>
    void M(int foo)
    {
      ///$$
    }
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotAfterClassName()
        {
            var code =
@"class C//$$
{
}";

            var expected =
@"class C///$$
{
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotAfterOpenBrace()
        {
            var code =
@"class C
{//$$
}";

            var expected =
@"class C
{///$$
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotAfterCtorName()
        {
            var code =
@"class C
{
C() //$$
}";

            var expected =
@"class C
{
C() ///$$
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TypingCharacter_NotInsideCtor()
        {
            var code =
@"class C
{
C()
{
//$$
}
}";

            var expected =
@"class C
{
C()
{
///$$
}
}";

            VerifyTypingCharacter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_Class1()
        {
            var code =
@"///$$
class C
{
}";

            var expected =
@"/// <summary>
/// $$
/// </summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_Class2()
        {
            var code =
@"///$$class C
{
}";

            var expected =
@"/// <summary>
/// $$
/// </summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_Class3()
        {
            var code =
@"///$$[Foo] class C
{
}";

            var expected =
@"/// <summary>
/// $$
/// </summary>
[Foo] class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_NotAfterWhitespace()
        {
            var code =
            @"///    $$class C
{
}";

            var expected =
@"///    
/// $$class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_Method1()
        {
            var code =
@"class C
{
    ///$$
    int M<T>(int foo) { return 0; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""foo""></param>
    /// <returns></returns>
    int M<T>(int foo) { return 0; }
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertComment_Method2()
        {
            var code =
@"class C
{
    ///$$int M<T>(int foo) { return 0; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""foo""></param>
    /// <returns></returns>
    int M<T>(int foo) { return 0; }
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotInMethodBody1()
        {
            var code =
@"class C
{
void Foo()
{
///$$
}
}";

            var expected =
@"class C
{
void Foo()
{
///
$$
}
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537513)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotInterleavedInClassName1()
        {
            var code =
@"class///$$ C
{
}";

            var expected =
@"class///
$$ C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537513)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotInterleavedInClassName2()
        {
            var code =
@"class ///$$C
{
}";

            var expected =
@"class ///
$$C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537513)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotInterleavedInClassName3()
        {
            var code =
@"class /// $$C
{
}";

            var expected =
@"class /// 
$$C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537514)]
        [WorkItem(537532)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotAfterClassName1()
        {
            var code =
@"class C ///$$
{
}";

            var expected =
@"class C ///
$$
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537552)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotAfterClassName2()
        {
            var code =
@"class C /** $$
{
}";

            var expected =
@"class C /** 
$$
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537535)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotAfterCtorName()
        {
            var code =
@"class C
{
C() ///$$
}";

            var expected =
@"class C
{
C() ///
$$
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537511)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotInsideCtor()
        {
            var code =
@"class C
{
C()
{
///$$
}
}";

            var expected =
@"class C
{
C()
{
///
$$
}
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(537550)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_NotBeforeDocComment()
        {
            var code =
@"    class c1
    {
$$/// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void foo()
        {
            var x = 1;
        }
    }";

            var expected =
@"    class c1
    {

$$/// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void foo()
        {
            var x = 1;
        }
    }";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes1()
        {
            var code =
@"///$$
/// <summary></summary>
class C
{
}";

            var expected =
@"///
/// $$
/// <summary></summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes2()
        {
            var code =
@"/// <summary>
/// $$
/// </summary>
class C
{
}";

            var expected =
@"/// <summary>
/// 
/// $$
/// </summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes3()
        {
            var code =
@"    /// <summary>
    /// $$
    /// </summary>
    class C
    {
    }";

            var expected =
@"    /// <summary>
    /// 
    /// $$
    /// </summary>
    class C
    {
    }";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes4()
        {
            var code =
@"/// <summary>$$</summary>
class C
{
}";

            var expected =
@"/// <summary>
/// $$</summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes5()
        {
            var code =
@"    /// <summary>
    /// $$
    /// </summary>
    class C
    {
    }";

            var expected =
@"    /// <summary>
    /// 
    /// $$
    /// </summary>
    class C
    {
    }";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes6()
        {
            var code =
@"/// <summary></summary>$$
class C
{
}";

            var expected =
@"/// <summary></summary>
/// $$
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes7()
        {
            var code =
@"    /// <summary>$$</summary>
    class C
    {
    }";

            var expected =
@"    /// <summary>
    /// $$</summary>
    class C
    {
    }";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(538702)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes8()
        {
            var code =
@"/// <summary>
/// 
/// </summary>
///$$class C {}";
            var expected =
@"/// <summary>
/// 
/// </summary>
///
/// $$class C {}";
            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes9()
        {
            var code =
@"class C
{
    ///$$
    /// <summary></summary>
    int M<T>(int foo) { return 0; }
}";

            var expected =
@"class C
{
    ///
    /// $$
    /// <summary></summary>
    int M<T>(int foo) { return 0; }
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes10()
        {
            var code =
@"/// <summary>
/// 
/// </summary>
///$$Go ahead and add some slashes";
            var expected =
@"/// <summary>
/// 
/// </summary>
///
/// $$Go ahead and add some slashes";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InsertSlashes11()
        {
            var code =
@"class C
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i"">$$</param>
    void Foo(int i)
    {
    }
}";

            var expected =
@"class C
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i"">
    /// $$</param>
    void Foo(int i)
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_DontInsertSlashes1()
        {
            var code =
@"/// <summary></summary>
/// $$
class C
{
}";

            var expected =
@"/// <summary></summary>
/// 
$$
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(538701)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_DontInsertSlashes2()
        {
            var code =
@"///<summary></summary>

///$$
class C{}";
            var expected =
@"///<summary></summary>

///
$$
class C{}";
            VerifyPressingEnter(code, expected);
        }

        [WorkItem(542426)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_PreserveParams()
        {
            var code =
@"/// <summary>
/// 
/// </summary>
/// <param name=""args"">$$</param>
static void Main(string[] args)
{ }";
            var expected =
@"/// <summary>
/// 
/// </summary>
/// <param name=""args"">
/// $$</param>
static void Main(string[] args)
{ }";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2091, "https://github.com/dotnet/roslyn/issues/2091")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_InTextBeforeSpace()
        {
            const string code =
@"class C
{
    /// <summary>
    /// hello$$ world
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    /// hello
    /// $$world
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Indentation1()
        {
            const string code =
@"class C
{
    /// <summary>
    ///     hello world$$
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    ///     hello world
    ///     $$
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Indentation2()
        {
            const string code =
@"class C
{
    /// <summary>
    ///     hello $$world
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    ///     hello 
    ///     $$world
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Indentation3()
        {
            const string code =
@"class C
{
    /// <summary>
    ///     hello$$ world
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    ///     hello
    ///     $$world
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Indentation4()
        {
            const string code =
@"class C
{
    /// <summary>
    ///     $$hello world
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    ///     
    /// $$hello world
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Indentation5_UseTabs()
        {
            const string code =
@"class C
{
    /// <summary>
	///     hello world$$
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
	///     hello world
	///     $$
    /// </summary>
    void M()
    {
    }
}";

            VerifyPressingEnter(code, expected, useTabs: true);
        }

        [WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Selection1()
        {
            var code =
@"/// <summary>
/// Hello [|World|]$$!
/// </summary>
class C
{
}";
            var expected =
@"/// <summary>
/// Hello 
/// $$!
/// </summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void PressingEnter_Selection2()
        {
            var code =
@"/// <summary>
/// Hello $$[|World|]!
/// </summary>
class C
{
}";
            var expected =
@"/// <summary>
/// Hello 
/// $$!
/// </summary>
class C
{
}";

            VerifyPressingEnter(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_Class()
        {
            var code =
@"class C
{$$
}";

            var expected =
@"/// <summary>
/// $$
/// </summary>
class C
{
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538714)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_BeforeClass1()
        {
            var code =
@"$$
class C { }";
            var expected =
@"
/// <summary>
/// $$
/// </summary>
class C { }";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538714)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_BeforeClass2()
        {
            var code =
@"class B { }
$$
class C { }";
            var expected =
@"class B { }

/// <summary>
/// $$
/// </summary>
class C { }";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538714)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_BeforeClass3()
        {
            var code =
@"class B
{
    $$
    class C { }
}";
            var expected =
@"class B
{
    
    /// <summary>
    /// $$
    /// </summary>
    class C { }
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(527604)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_Class_NotIfMultilineDocCommentExists()
        {
            var code =
@"/**
*/
class C { $$ }";

            var expected =
@"/**
*/
class C { $$ }";
            VerifyInsertCommentCommand(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_Method()
        {
            var code =
@"class C
{
    int M<T>(int foo) { $$return 0; }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""foo""></param>
    /// <returns></returns>
    int M<T>(int foo) { return 0; }
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_Class_NotIfCommentExists()
        {
            var code =
@"/// <summary></summary>
class C
{$$
}";

            var expected =
@"/// <summary></summary>
class C
{$$
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_Method_NotIfCommentExists()
        {
            var code =
@"class C
{
    /// <summary></summary>
    int M<T>(int foo) { $$return 0; }
}";

            var expected =
@"class C
{
    /// <summary></summary>
    int M<T>(int foo) { $$return 0; }
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538482)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_FirstClassOnLine()
        {
            var code = @"$$class C { } class D { }";

            var expected =
 @"/// <summary>
/// $$
/// </summary>
class C { } class D { }";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538482)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_NotOnSecondClassOnLine()
        {
            var code = @"class C { } $$class D { }";

            var expected = @"class C { } $$class D { }";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538482)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_FirstMethodOnLine()
        {
            var code =
@"class C
{
    protected abstract void $$Foo(); protected abstract void Bar();
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// </summary>
    protected abstract void Foo(); protected abstract void Bar();
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(538482)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void Command_NotOnSecondMethodOnLine()
        {
            var code =
@"class C
{
    protected abstract void Foo(); protected abstract void $$Bar();
}";

            var expected =
@"class C
{
    protected abstract void Foo(); protected abstract void $$Bar();
}";

            VerifyInsertCommentCommand(code, expected);
        }

        [WorkItem(917904)]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestUseTab()
        {
            var code =
@"using System;

public class Class1
{
	//$$
	public Class1()
	{
	}
}";

            var expected =
@"using System;

public class Class1
{
	/// <summary>
	/// $$
	/// </summary>
	public Class1()
	{
	}
}";

            VerifyTypingCharacter(code, expected, useTabs: true);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineAbove1()
        {
            const string code =
@"class C
{
    /// <summary>
    /// stuff$$
    /// </summary>
    void M()
    {
    }
}";

            var expected =
@"class C
{
    /// <summary>
    /// $$
    /// stuff
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineAbove(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineAbove2()
        {
            const string code =
@"class C
{
    /// <summary>
    /// $$stuff
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    /// $$
    /// stuff
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineAbove(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineAbove3()
        {
            const string code =
@"class C
{
    /// $$<summary>
    /// stuff
    /// </summary>
    void M()
    {
    }
}";

            // Note that the caret position specified below does not look correct because
            // it is in virtual space in this case.
            const string expected =
@"class C
{
$$
    /// <summary>
    /// stuff
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineAbove(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineAbove4_Tabs()
        {
            const string code =
@"class C
{
		  /// <summary>
    /// $$stuff
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
		  /// <summary>
		  /// $$
    /// stuff
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineAbove(code, expected, useTabs: true);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineBelow1()
        {
            const string code =
@"class C
{
    /// <summary>
    /// stuff$$
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    /// stuff
    /// $$
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineBelow(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineBelow2()
        {
            const string code =
@"class C
{
    /// <summary>
    /// $$stuff
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
    /// stuff
    /// $$
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineBelow(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineBelow3()
        {
            const string code =
@"/// <summary>
/// stuff
/// $$</summary>
";

            const string expected =
@"/// <summary>
/// stuff
/// </summary>
/// $$
";

            VerifyOpenLineBelow(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public void TestOpenLineBelow4_Tabs()
        {
            const string code =
@"class C
{
    /// <summary>
		  /// $$stuff
    /// </summary>
    void M()
    {
    }
}";

            const string expected =
@"class C
{
    /// <summary>
		  /// stuff
		  /// $$
    /// </summary>
    void M()
    {
    }
}";

            VerifyOpenLineBelow(code, expected, useTabs: true);
        }

        protected override char DocumentationCommentCharacter
        {
            get { return '/'; }
        }

        internal override ICommandHandler CreateCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IAsyncCompletionService completionService)
        {
            return new DocumentationCommentCommandHandler(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService, completionService);
        }

        protected override TestWorkspace CreateTestWorkspace(string code)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromLines(code);
        }
    }
}
