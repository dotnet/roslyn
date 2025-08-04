// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceMatching;

[Trait(Traits.Feature, Traits.Features.BraceMatching)]
public sealed class CSharpBraceMatcherTests : AbstractBraceMatcherTests
{
    protected override EditorTestWorkspace CreateWorkspaceFromCode(string code, ParseOptions options)
        => EditorTestWorkspace.CreateCSharp(code, options);

    [Fact]
    public Task TestEmptyFile()
        => TestAsync(@"$$", @"");

    [Fact]
    public Task TestAtFirstPositionInFile()
        => TestAsync(@"$$public class C { }", @"public class C { }");

    [Fact]
    public Task TestAtLastPositionInFile()
        => TestAsync(@"public class C { }$$", @"public class C [|{|] }");

    [Fact]
    public Task TestCurlyBrace1()
        => TestAsync(@"public class C $${ }", @"public class C { [|}|]");

    [Fact]
    public Task TestCurlyBrace2()
        => TestAsync(@"public class C {$$ }", @"public class C { [|}|]");

    [Fact]
    public Task TestCurlyBrace3()
        => TestAsync(@"public class C { $$}", @"public class C [|{|] }");

    [Fact]
    public Task TestCurlyBrace4()
        => TestAsync(@"public class C { }$$", @"public class C [|{|] }");

    [Fact]
    public Task TestParen1()
        => TestAsync(@"public class C { void Goo$$() { } }", @"public class C { void Goo([|)|] { } }");

    [Fact]
    public Task TestParen2()
        => TestAsync(@"public class C { void Goo($$) { } }", @"public class C { void Goo([|)|] { } }");

    [Fact]
    public Task TestParen3()
        => TestAsync(@"public class C { void Goo($$ ) { } }", @"public class C { void Goo( [|)|] { } }");

    [Fact]
    public Task TestParen4()
        => TestAsync(@"public class C { void Goo( $$) { } }", @"public class C { void Goo[|(|] ) { } }");

    [Fact]
    public Task TestParen5()
        => TestAsync(@"public class C { void Goo( )$$ { } }", @"public class C { void Goo[|(|] ) { } }");

    [Fact]
    public Task TestParen6()
        => TestAsync(@"public class C { void Goo()$$ { } }", @"public class C { void Goo[|(|]) { } }");

    [Fact]
    public Task TestSquareBracket1()
        => TestAsync(@"public class C { int$$[] i; }", @"public class C { int[[|]|] i; }");

    [Fact]
    public Task TestSquareBracket2()
        => TestAsync(@"public class C { int[$$] i; }", @"public class C { int[[|]|] i; }");

    [Fact]
    public Task TestSquareBracket3()
        => TestAsync(@"public class C { int[$$ ] i; }", @"public class C { int[ [|]|] i; }");

    [Fact]
    public Task TestSquareBracket4()
        => TestAsync(@"public class C { int[ $$] i; }", @"public class C { int[|[|] ] i; }");

    [Fact]
    public Task TestSquareBracket5()
        => TestAsync(@"public class C { int[ ]$$ i; }", @"public class C { int[|[|] ] i; }");

    [Fact]
    public Task TestSquareBracket6()
        => TestAsync(@"public class C { int[]$$ i; }", @"public class C { int[|[|]] i; }");

    [Fact]
    public Task TestAngleBracket1()
        => TestAsync(@"public class C { Goo$$<int> f; }", @"public class C { Goo<int[|>|] f; }");

    [Fact]
    public Task TestAngleBracket2()
        => TestAsync(@"public class C { Goo<$$int> f; }", @"public class C { Goo<int[|>|] f; }");

    [Fact]
    public Task TestAngleBracket3()
        => TestAsync(@"public class C { Goo<int$$> f; }", @"public class C { Goo[|<|]int> f; }");

    [Fact]
    public Task TestAngleBracket4()
        => TestAsync(@"public class C { Goo<int>$$ f; }", @"public class C { Goo[|<|]int> f; }");

    [Fact]
    public Task TestNestedAngleBracket1()
        => TestAsync(@"public class C { Func$$<Func<int,int>> f; }", @"public class C { Func<Func<int,int>[|>|] f; }");

    [Fact]
    public Task TestNestedAngleBracket2()
        => TestAsync(@"public class C { Func<$$Func<int,int>> f; }", @"public class C { Func<Func<int,int>[|>|] f; }");

    [Fact]
    public Task TestNestedAngleBracket3()
        => TestAsync(@"public class C { Func<Func$$<int,int>> f; }", @"public class C { Func<Func<int,int[|>|]> f; }");

    [Fact]
    public Task TestNestedAngleBracket4()
        => TestAsync(@"public class C { Func<Func<$$int,int>> f; }", @"public class C { Func<Func<int,int[|>|]> f; }");

    [Fact]
    public Task TestNestedAngleBracket5()
        => TestAsync(@"public class C { Func<Func<int,int$$>> f; }", @"public class C { Func<Func[|<|]int,int>> f; }");

    [Fact]
    public Task TestNestedAngleBracket6()
        => TestAsync(@"public class C { Func<Func<int,int>$$> f; }", @"public class C { Func<Func[|<|]int,int>> f; }");

    [Fact]
    public Task TestNestedAngleBracket7()
        => TestAsync(@"public class C { Func<Func<int,int> $$> f; }", @"public class C { Func[|<|]Func<int,int> > f; }");

    [Fact]
    public Task TestNestedAngleBracket8()
        => TestAsync(@"public class C { Func<Func<int,int>>$$ f; }", @"public class C { Func[|<|]Func<int,int>> f; }");

    [Fact]
    public Task TestString1()
        => TestAsync(@"public class C { string s = $$""Goo""; }", @"public class C { string s = ""Goo[|""|]; }");

    [Fact]
    public Task TestString2()
        => TestAsync(@"public class C { string s = ""$$Goo""; }", @"public class C { string s = ""Goo[|""|]; }");

    [Fact]
    public Task TestString3()
        => TestAsync(@"public class C { string s = ""Goo$$""; }", @"public class C { string s = [|""|]Goo""; }");

    [Fact]
    public Task TestString4()
        => TestAsync(@"public class C { string s = ""Goo""$$; }", @"public class C { string s = [|""|]Goo""; }");

    [Fact]
    public Task TestString5()
        => TestAsync(@"public class C { string s = ""Goo$$ ", @"public class C { string s = ""Goo ");

    [Fact]
    public Task TestVerbatimString1()
        => TestAsync(@"public class C { string s = $$@""Goo""; }", @"public class C { string s = @""Goo[|""|]; }");

    [Fact]
    public Task TestVerbatimString2()
        => TestAsync(@"public class C { string s = @$$""Goo""; }", @"public class C { string s = @""Goo[|""|]; }");

    [Fact]
    public Task TestVerbatimString3()
        => TestAsync(@"public class C { string s = @""$$Goo""; }", @"public class C { string s = @""Goo[|""|]; }");

    [Fact]
    public Task TestVerbatimString4()
        => TestAsync(@"public class C { string s = @""Goo$$""; }", @"public class C { string s = [|@""|]Goo""; }");

    [Fact]
    public Task TestVerbatimString5()
        => TestAsync(@"public class C { string s = @""Goo""$$; }", @"public class C { string s = [|@""|]Goo""; }");

    [Fact]
    public Task TestInterpolatedString1()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""$${x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }");

    [Fact]
    public Task TestInterpolatedString2()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{$$x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }");

    [Fact]
    public Task TestInterpolatedString3()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x$$}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }");

    [Fact]
    public Task TestInterpolatedString4()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}$$, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }");

    [Fact]
    public Task TestInterpolatedString5()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, $${y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }");

    [Fact]
    public Task TestInterpolatedString6()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {$$y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }");

    [Fact]
    public Task TestInterpolatedString7()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y$$}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }");

    [Fact]
    public Task TestInterpolatedString8()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}$$""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }");

    [Fact]
    public Task TestInterpolatedString9()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }");

    [Fact]
    public Task TestInterpolatedString10()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }");

    [Fact]
    public Task TestInterpolatedString11()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$@""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");

    [Fact]
    public Task TestInterpolatedString12()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$@""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");

    [Fact]
    public Task TestInterpolatedString13()
        => TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@$$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");

    [Fact]
    public Task TestUtf8String1()
        => TestAsync(@"public class C { string s = $$""Goo""u8; }", @"public class C { string s = ""Goo[|""u8|]; }");

    [Fact]
    public Task TestUtf8String2()
        => TestAsync(@"public class C { string s = ""$$Goo""u8; }", @"public class C { string s = ""Goo[|""u8|]; }");

    [Fact]
    public Task TestUtf8String3()
        => TestAsync(@"public class C { string s = ""Goo$$""u8; }", @"public class C { string s = [|""|]Goo""u8; }");

    [Fact]
    public Task TestUtf8String4()
        => TestAsync(@"public class C { string s = ""Goo""$$u8; }", @"public class C { string s = [|""|]Goo""u8; }");

    [Fact]
    public Task TestUtf8String5()
        => TestAsync(@"public class C { string s = ""Goo""u$$8; }", @"public class C { string s = [|""|]Goo""u8; }");

    [Fact]
    public Task TestUtf8String6()
        => TestAsync(@"public class C { string s = ""Goo""u8$$; }", @"public class C { string s = [|""|]Goo""u8; }");

    [Fact]
    public Task TestVerbatimUtf8String1()
        => TestAsync(@"public class C { string s = $$@""Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");

    [Fact]
    public Task TestVerbatimUtf8String2()
        => TestAsync(@"public class C { string s = @$$""Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");

    [Fact]
    public Task TestVerbatimUtf8String3()
        => TestAsync(@"public class C { string s = @""$$Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");

    [Fact]
    public Task TestVerbatimUtf8String4()
        => TestAsync(@"public class C { string s = @""Goo$$""u8; }", @"public class C { string s = [|@""|]Goo""u8; }");

    [Fact]
    public Task TestVerbatimUtf8String5()
        => TestAsync(@"public class C { string s = @""Goo""$$u8; }", @"public class C { string s = [|@""|]Goo""u8; }");

    [Fact]
    public Task TestVerbatimUtf8String6()
        => TestAsync(@"public class C { string s = @""Goo""u$$8; }", @"public class C { string s = [|@""|]Goo""u8; }");

    [Fact]
    public Task TestVerbatimUtf8String7()
        => TestAsync(@"public class C { string s = @""Goo""u8$$; }", @"public class C { string s = [|@""|]Goo""u8; }");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestConditionalDirectiveWithSingleMatchingDirective()
        => TestAsync("""
            public class C 
            {
            #if$$ CHK 
            #endif
            }
            """, """
            public class C 
            {
            #if$$ CHK 
            [|#endif|]
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestConditionalDirectiveWithTwoMatchingDirectives()
        => TestAsync("""
            public class C 
            {
            #if$$ CHK 
            #else
            #endif
            }
            """, """
            public class C 
            {
            #if$$ CHK 
            [|#else|]
            #endif
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestConditionalDirectiveWithAllMatchingDirectives()
        => TestAsync("""
            public class C 
            {
            #if CHK 
            #elif RET
            #else
            #endif$$
            }
            """, """
            public class C 
            {
            [|#if|] CHK 
            #elif RET
            #else
            #endif
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestRegionDirective()
        => TestAsync("""
            public class C 
            {
            $$#region test
            #endregion
            }
            """, """
            public class C 
            {
            #region test
            [|#endregion|]
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestInterleavedDirectivesInner()
        => TestAsync("""
            #define CHK
            public class C 
            {
                void Test()
                {
            #if CHK
            $$#region test
                var x = 5;
            #endregion
            #else
                var y = 6;
            #endif
                }
            }
            """, """
            #define CHK
            public class C 
            {
                void Test()
                {
            #if CHK
            #region test
                var x = 5;
            [|#endregion|]
            #else
                var y = 6;
            #endif
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestInterleavedDirectivesOuter()
        => TestAsync("""
            #define CHK
            public class C 
            {
                void Test()
                {
            #if$$ CHK
            #region test
                var x = 5;
            #endregion
            #else
                var y = 6;
            #endif
                }
            }
            """, """
            #define CHK
            public class C 
            {
                void Test()
                {
            #if CHK
            #region test
                var x = 5;
            #endregion
            [|#else|]
                var y = 6;
            #endif
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestUnmatchedDirective1()
        => TestAsync("""
            public class C 
            {
            $$#region test
            }
            """, """
            public class C 
            {
            #region test
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public Task TestUnmatchedDirective2()
        => TestAsync("""
            #d$$efine CHK
            public class C 
            {
            }
            """, """
            #define CHK
            public class C 
            {
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7534")]
    public Task TestUnmatchedConditionalDirective()
        => TestAsync("""
            class Program
            {
                static void Main(string[] args)
                {#if$$

                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {#if

                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7534")]
    public Task TestUnmatchedConditionalDirective2()
        => TestAsync("""
            class Program
            {
                static void Main(string[] args)
                {#else$$

                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {#else

                }
            }
            """);

    [Fact]
    public Task StartTupleDeclaration()
        => TestAsync(@"public class C { $$(int, int, int, int, int, int, int, int) x; }", @"public class C { (int, int, int, int, int, int, int, int[|)|] x; }", TestOptions.Regular);

    [Fact]
    public Task EndTupleDeclaration()
        => TestAsync(@"public class C { (int, int, int, int, int, int, int, int)$$ x; }", @"public class C { [|(|]int, int, int, int, int, int, int, int) x; }", TestOptions.Regular);

    [Fact]
    public Task StartTupleLiteral()
        => TestAsync(@"public class C { var x = $$(1, 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = (1, 2, 3, 4, 5, 6, 7, 8[|)|]; }", TestOptions.Regular);

    [Fact]
    public Task EndTupleLiteral()
        => TestAsync(@"public class C { var x = (1, 2, 3, 4, 5, 6, 7, 8)$$; }", @"public class C { var x = [|(|]1, 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);

    [Fact]
    public Task StartNestedTupleLiteral()
        => TestAsync(@"public class C { var x = $$((1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ((1, 1, 1), 2, 3, 4, 5, 6, 7, 8[|)|]; }", TestOptions.Regular);

    [Fact]
    public Task StartInnerNestedTupleLiteral()
        => TestAsync(@"public class C { var x = ($$(1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ((1, 1, 1[|)|], 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);

    [Fact]
    public Task EndNestedTupleLiteral()
        => TestAsync(@"public class C { var x = (1, 2, 3, 4, 5, 6, 7, (8, 8, 8))$$; }", @"public class C { var x = [|(|]1, 2, 3, 4, 5, 6, 7, (8, 8, 8)); }", TestOptions.Regular);

    [Fact]
    public Task EndInnerNestedTupleLiteral()
        => TestAsync(@"public class C { var x = ((1, 1, 1)$$, 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ([|(|]1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);

    [Fact]
    public Task TestFunctionPointer()
        => TestAsync(@"public unsafe class C { delegate*<$$int, int> functionPointer; }", @"public unsafe class C { delegate*<int, int[|>|] functionPointer; }", TestOptions.Regular);
}
