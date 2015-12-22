// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Class()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Class_AutoGenerateXmlDocCommentsOff()
        {
            var code =
@"//$$
class C
{
}";

            var expected =
@"///$$
class C
{
}";

            await VerifyTypingCharacterAsync(code, expected, autoGenerateXmlDocComments: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Method()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Method_WithVerbatimParams()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_AutoProperty()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Property()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_Indexer()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_VoidMethod1()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_VoidMethod_WithVerbatimParams()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WorkItem(538699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_VoidMethod2()
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
            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotWhenDocCommentExists1()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotWhenDocCommentExists2()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotWhenDocCommentExists3()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotWhenDocCommentExists4()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotWhenDocCommentExists5()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotInsideMethodBody1()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotInsideMethodBody2()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotAfterClassName()
        {
            var code =
@"class C//$$
{
}";

            var expected =
@"class C///$$
{
}";

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotAfterOpenBrace()
        {
            var code =
@"class C
{//$$
}";

            var expected =
@"class C
{///$$
}";

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotAfterCtorName()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TypingCharacter_NotInsideCtor()
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

            await VerifyTypingCharacterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Class1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Class1_AutoGenerateXmlDocCommentsOff()
        {
            var code =
@"///$$
class C
{
}";

            var expected =
@"///
$$
class C
{
}";

            await VerifyPressingEnterAsync(code, expected, autoGenerateXmlDocComments: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Class2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Class3()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_NotAfterWhitespace()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Method1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertComment_Method2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotInMethodBody1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537513)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotInterleavedInClassName1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537513)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotInterleavedInClassName2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537513)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotInterleavedInClassName3()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537514)]
        [WorkItem(537532)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotAfterClassName1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537552)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotAfterClassName2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537535)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotAfterCtorName()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537511)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotInsideCtor()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(537550)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_NotBeforeDocComment()
        {
            var code =
@"    class c1
    {
$$/// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task foo()
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
        public async Task foo()
        {
            var x = 1;
        }
    }";

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes3()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes4()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes5()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes6()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes7()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(538702)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes8()
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
            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes9()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes10()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes11()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InsertSlashes12_AutoGenerateXmlDocCommentsOff()
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

            await VerifyPressingEnterAsync(code, expected, autoGenerateXmlDocComments: false);
        }


        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_DontInsertSlashes1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(538701)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_DontInsertSlashes2()
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
            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(542426)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_PreserveParams()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2091, "https://github.com/dotnet/roslyn/issues/2091")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_InTextBeforeSpace()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Indentation1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Indentation2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Indentation3()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Indentation4()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Indentation5_UseTabs()
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

            await VerifyPressingEnterAsync(code, expected, useTabs: true);
        }

        [WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Selection1()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task PressingEnter_Selection2()
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

            await VerifyPressingEnterAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Class()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Class_AutoGenerateXmlDocCommentsOff()
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

            await VerifyInsertCommentCommandAsync(code, expected, autoGenerateXmlDocComments: false);
        }

        [WorkItem(538714)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_BeforeClass1()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538714)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_BeforeClass2()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538714)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_BeforeClass3()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(527604)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Class_NotIfMultilineDocCommentExists()
        {
            var code =
@"/**
*/
class C { $$ }";

            var expected =
@"/**
*/
class C { $$ }";
            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Method()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Class_NotIfCommentExists()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_Method_NotIfCommentExists()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538482)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_FirstClassOnLine()
        {
            var code = @"$$class C { } class D { }";

            var expected =
 @"/// <summary>
/// $$
/// </summary>
class C { } class D { }";

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538482)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_NotOnSecondClassOnLine()
        {
            var code = @"class C { } $$class D { }";

            var expected = @"class C { } $$class D { }";

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538482)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_FirstMethodOnLine()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(538482)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task Command_NotOnSecondMethodOnLine()
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

            await VerifyInsertCommentCommandAsync(code, expected);
        }

        [WorkItem(917904)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestUseTab()
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

            await VerifyTypingCharacterAsync(code, expected, useTabs: true);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineAbove1()
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

            await VerifyOpenLineAboveAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineAbove2()
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

            await VerifyOpenLineAboveAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineAbove3()
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

            await VerifyOpenLineAboveAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineAbove4_Tabs()
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

            await VerifyOpenLineAboveAsync(code, expected, useTabs: true);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineBelow1()
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

            await VerifyOpenLineBelowAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineBelow2()
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

            await VerifyOpenLineBelowAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineBelow3()
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

            await VerifyOpenLineBelowAsync(code, expected);
        }

        [WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task TestOpenLineBelow4_Tabs()
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

            await VerifyOpenLineBelowAsync(code, expected, useTabs: true);
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

        protected override Task<TestWorkspace> CreateTestWorkspaceAsync(string code)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code);
        }
    }
}
