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
    public async Task TestEmptyFile()
    {
        await TestAsync(@"$$", @"");
    }

    [Fact]
    public async Task TestAtFirstPositionInFile()
    {
        await TestAsync(@"$$public class C { }", @"public class C { }");
    }

    [Fact]
    public async Task TestAtLastPositionInFile()
    {
        await TestAsync(@"public class C { }$$", @"public class C [|{|] }");
    }

    [Fact]
    public async Task TestCurlyBrace1()
    {
        await TestAsync(@"public class C $${ }", @"public class C { [|}|]");
    }

    [Fact]
    public async Task TestCurlyBrace2()
    {
        await TestAsync(@"public class C {$$ }", @"public class C { [|}|]");
    }

    [Fact]
    public async Task TestCurlyBrace3()
    {
        await TestAsync(@"public class C { $$}", @"public class C [|{|] }");
    }

    [Fact]
    public async Task TestCurlyBrace4()
    {
        await TestAsync(@"public class C { }$$", @"public class C [|{|] }");
    }

    [Fact]
    public async Task TestParen1()
    {
        await TestAsync(@"public class C { void Goo$$() { } }", @"public class C { void Goo([|)|] { } }");
    }

    [Fact]
    public async Task TestParen2()
    {
        await TestAsync(@"public class C { void Goo($$) { } }", @"public class C { void Goo([|)|] { } }");
    }

    [Fact]
    public async Task TestParen3()
    {
        await TestAsync(@"public class C { void Goo($$ ) { } }", @"public class C { void Goo( [|)|] { } }");
    }

    [Fact]
    public async Task TestParen4()
    {
        await TestAsync(@"public class C { void Goo( $$) { } }", @"public class C { void Goo[|(|] ) { } }");
    }

    [Fact]
    public async Task TestParen5()
    {
        await TestAsync(@"public class C { void Goo( )$$ { } }", @"public class C { void Goo[|(|] ) { } }");
    }

    [Fact]
    public async Task TestParen6()
    {
        await TestAsync(@"public class C { void Goo()$$ { } }", @"public class C { void Goo[|(|]) { } }");
    }

    [Fact]
    public async Task TestSquareBracket1()
    {
        await TestAsync(@"public class C { int$$[] i; }", @"public class C { int[[|]|] i; }");
    }

    [Fact]
    public async Task TestSquareBracket2()
    {
        await TestAsync(@"public class C { int[$$] i; }", @"public class C { int[[|]|] i; }");
    }

    [Fact]
    public async Task TestSquareBracket3()
    {
        await TestAsync(@"public class C { int[$$ ] i; }", @"public class C { int[ [|]|] i; }");
    }

    [Fact]
    public async Task TestSquareBracket4()
    {
        await TestAsync(@"public class C { int[ $$] i; }", @"public class C { int[|[|] ] i; }");
    }

    [Fact]
    public async Task TestSquareBracket5()
    {
        await TestAsync(@"public class C { int[ ]$$ i; }", @"public class C { int[|[|] ] i; }");
    }

    [Fact]
    public async Task TestSquareBracket6()
    {
        await TestAsync(@"public class C { int[]$$ i; }", @"public class C { int[|[|]] i; }");
    }

    [Fact]
    public async Task TestAngleBracket1()
    {
        await TestAsync(@"public class C { Goo$$<int> f; }", @"public class C { Goo<int[|>|] f; }");
    }

    [Fact]
    public async Task TestAngleBracket2()
    {
        await TestAsync(@"public class C { Goo<$$int> f; }", @"public class C { Goo<int[|>|] f; }");
    }

    [Fact]
    public async Task TestAngleBracket3()
    {
        await TestAsync(@"public class C { Goo<int$$> f; }", @"public class C { Goo[|<|]int> f; }");
    }

    [Fact]
    public async Task TestAngleBracket4()
    {
        await TestAsync(@"public class C { Goo<int>$$ f; }", @"public class C { Goo[|<|]int> f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket1()
    {
        await TestAsync(@"public class C { Func$$<Func<int,int>> f; }", @"public class C { Func<Func<int,int>[|>|] f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket2()
    {
        await TestAsync(@"public class C { Func<$$Func<int,int>> f; }", @"public class C { Func<Func<int,int>[|>|] f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket3()
    {
        await TestAsync(@"public class C { Func<Func$$<int,int>> f; }", @"public class C { Func<Func<int,int[|>|]> f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket4()
    {
        await TestAsync(@"public class C { Func<Func<$$int,int>> f; }", @"public class C { Func<Func<int,int[|>|]> f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket5()
    {
        await TestAsync(@"public class C { Func<Func<int,int$$>> f; }", @"public class C { Func<Func[|<|]int,int>> f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket6()
    {
        await TestAsync(@"public class C { Func<Func<int,int>$$> f; }", @"public class C { Func<Func[|<|]int,int>> f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket7()
    {
        await TestAsync(@"public class C { Func<Func<int,int> $$> f; }", @"public class C { Func[|<|]Func<int,int> > f; }");
    }

    [Fact]
    public async Task TestNestedAngleBracket8()
    {
        await TestAsync(@"public class C { Func<Func<int,int>>$$ f; }", @"public class C { Func[|<|]Func<int,int>> f; }");
    }

    [Fact]
    public async Task TestString1()
    {
        await TestAsync(@"public class C { string s = $$""Goo""; }", @"public class C { string s = ""Goo[|""|]; }");
    }

    [Fact]
    public async Task TestString2()
    {
        await TestAsync(@"public class C { string s = ""$$Goo""; }", @"public class C { string s = ""Goo[|""|]; }");
    }

    [Fact]
    public async Task TestString3()
    {
        await TestAsync(@"public class C { string s = ""Goo$$""; }", @"public class C { string s = [|""|]Goo""; }");
    }

    [Fact]
    public async Task TestString4()
    {
        await TestAsync(@"public class C { string s = ""Goo""$$; }", @"public class C { string s = [|""|]Goo""; }");
    }

    [Fact]
    public async Task TestString5()
    {
        await TestAsync(@"public class C { string s = ""Goo$$ ", @"public class C { string s = ""Goo ");
    }

    [Fact]
    public async Task TestVerbatimString1()
    {
        await TestAsync(@"public class C { string s = $$@""Goo""; }", @"public class C { string s = @""Goo[|""|]; }");
    }

    [Fact]
    public async Task TestVerbatimString2()
    {
        await TestAsync(@"public class C { string s = @$$""Goo""; }", @"public class C { string s = @""Goo[|""|]; }");
    }

    [Fact]
    public async Task TestVerbatimString3()
    {
        await TestAsync(@"public class C { string s = @""$$Goo""; }", @"public class C { string s = @""Goo[|""|]; }");
    }

    [Fact]
    public async Task TestVerbatimString4()
    {
        await TestAsync(@"public class C { string s = @""Goo$$""; }", @"public class C { string s = [|@""|]Goo""; }");
    }

    [Fact]
    public async Task TestVerbatimString5()
    {
        await TestAsync(@"public class C { string s = @""Goo""$$; }", @"public class C { string s = [|@""|]Goo""; }");
    }

    [Fact]
    public async Task TestInterpolatedString1()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""$${x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString2()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{$$x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString3()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x$$}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString4()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}$$, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString5()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, $${y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }");
    }

    [Fact]
    public async Task TestInterpolatedString6()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {$$y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }");
    }

    [Fact]
    public async Task TestInterpolatedString7()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y$$}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString8()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}$$""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }");
    }

    [Fact]
    public async Task TestInterpolatedString9()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }");
    }

    [Fact]
    public async Task TestInterpolatedString10()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }");
    }

    [Fact]
    public async Task TestInterpolatedString11()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$@""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");
    }

    [Fact]
    public async Task TestInterpolatedString12()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$@""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");
    }

    [Fact]
    public async Task TestInterpolatedString13()
    {
        await TestAsync(@"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@$$""{x}, {y}""; }", @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }");
    }

    [Fact]
    public async Task TestUtf8String1()
    {
        await TestAsync(@"public class C { string s = $$""Goo""u8; }", @"public class C { string s = ""Goo[|""u8|]; }");
    }

    [Fact]
    public async Task TestUtf8String2()
    {
        await TestAsync(@"public class C { string s = ""$$Goo""u8; }", @"public class C { string s = ""Goo[|""u8|]; }");
    }

    [Fact]
    public async Task TestUtf8String3()
    {
        await TestAsync(@"public class C { string s = ""Goo$$""u8; }", @"public class C { string s = [|""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestUtf8String4()
    {
        await TestAsync(@"public class C { string s = ""Goo""$$u8; }", @"public class C { string s = [|""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestUtf8String5()
    {
        await TestAsync(@"public class C { string s = ""Goo""u$$8; }", @"public class C { string s = [|""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestUtf8String6()
    {
        await TestAsync(@"public class C { string s = ""Goo""u8$$; }", @"public class C { string s = [|""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String1()
    {
        await TestAsync(@"public class C { string s = $$@""Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String2()
    {
        await TestAsync(@"public class C { string s = @$$""Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String3()
    {
        await TestAsync(@"public class C { string s = @""$$Goo""u8; }", @"public class C { string s = @""Goo[|""u8|]; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String4()
    {
        await TestAsync(@"public class C { string s = @""Goo$$""u8; }", @"public class C { string s = [|@""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String5()
    {
        await TestAsync(@"public class C { string s = @""Goo""$$u8; }", @"public class C { string s = [|@""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String6()
    {
        await TestAsync(@"public class C { string s = @""Goo""u$$8; }", @"public class C { string s = [|@""|]Goo""u8; }");
    }

    [Fact]
    public async Task TestVerbatimUtf8String7()
    {
        await TestAsync(@"public class C { string s = @""Goo""u8$$; }", @"public class C { string s = [|@""|]Goo""u8; }");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestConditionalDirectiveWithSingleMatchingDirective()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestConditionalDirectiveWithTwoMatchingDirectives()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestConditionalDirectiveWithAllMatchingDirectives()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestRegionDirective()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestInterleavedDirectivesInner()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestInterleavedDirectivesOuter()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestUnmatchedDirective1()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7120")]
    public async Task TestUnmatchedDirective2()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7534")]
    public async Task TestUnmatchedConditionalDirective()
    {
        await TestAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7534")]
    public async Task TestUnmatchedConditionalDirective2()
    {
        await TestAsync("""
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
    }

    [Fact]
    public async Task StartTupleDeclaration()
    {
        await TestAsync(@"public class C { $$(int, int, int, int, int, int, int, int) x; }", @"public class C { (int, int, int, int, int, int, int, int[|)|] x; }", TestOptions.Regular);
    }

    [Fact]
    public async Task EndTupleDeclaration()
    {
        await TestAsync(@"public class C { (int, int, int, int, int, int, int, int)$$ x; }", @"public class C { [|(|]int, int, int, int, int, int, int, int) x; }", TestOptions.Regular);
    }

    [Fact]
    public async Task StartTupleLiteral()
    {
        await TestAsync(@"public class C { var x = $$(1, 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = (1, 2, 3, 4, 5, 6, 7, 8[|)|]; }", TestOptions.Regular);
    }

    [Fact]
    public async Task EndTupleLiteral()
    {
        await TestAsync(@"public class C { var x = (1, 2, 3, 4, 5, 6, 7, 8)$$; }", @"public class C { var x = [|(|]1, 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);
    }

    [Fact]
    public async Task StartNestedTupleLiteral()
    {
        await TestAsync(@"public class C { var x = $$((1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ((1, 1, 1), 2, 3, 4, 5, 6, 7, 8[|)|]; }", TestOptions.Regular);
    }

    [Fact]
    public async Task StartInnerNestedTupleLiteral()
    {
        await TestAsync(@"public class C { var x = ($$(1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ((1, 1, 1[|)|], 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);
    }

    [Fact]
    public async Task EndNestedTupleLiteral()
    {
        await TestAsync(@"public class C { var x = (1, 2, 3, 4, 5, 6, 7, (8, 8, 8))$$; }", @"public class C { var x = [|(|]1, 2, 3, 4, 5, 6, 7, (8, 8, 8)); }", TestOptions.Regular);
    }

    [Fact]
    public async Task EndInnerNestedTupleLiteral()
    {
        await TestAsync(@"public class C { var x = ((1, 1, 1)$$, 2, 3, 4, 5, 6, 7, 8); }", @"public class C { var x = ([|(|]1, 1, 1), 2, 3, 4, 5, 6, 7, 8); }", TestOptions.Regular);
    }

    [Fact]
    public async Task TestFunctionPointer()
    {
        await TestAsync(@"public unsafe class C { delegate*<$$int, int> functionPointer; }", @"public unsafe class C { delegate*<int, int[|>|] functionPointer; }", TestOptions.Regular);
    }
}
