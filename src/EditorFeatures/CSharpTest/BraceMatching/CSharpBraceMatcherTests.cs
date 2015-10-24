// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceMatching
{
    public class CSharpBraceMatcherTests : AbstractBraceMatcherTests
    {
        protected override TestWorkspace CreateWorkspaceFromCode(string code)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromLines(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestEmptyFile()
        {
            var code = @"$$";
            var expected = @"";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAtFirstPositionInFile()
        {
            var code = @"$$public class C { }";
            var expected = @"public class C { }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAtLastPositionInFile()
        {
            var code = @"public class C { }$$";
            var expected = @"public class C [|{|] }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestCurlyBrace1()
        {
            var code = @"public class C $${ }";
            var expected = @"public class C { [|}|]";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestCurlyBrace2()
        {
            var code = @"public class C {$$ }";
            var expected = @"public class C { [|}|]";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestCurlyBrace3()
        {
            var code = @"public class C { $$}";
            var expected = @"public class C [|{|] }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestCurlyBrace4()
        {
            var code = @"public class C { }$$";
            var expected = @"public class C [|{|] }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen1()
        {
            var code = @"public class C { void Foo$$() { } }";
            var expected = @"public class C { void Foo([|)|] { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen2()
        {
            var code = @"public class C { void Foo($$) { } }";
            var expected = @"public class C { void Foo([|)|] { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen3()
        {
            var code = @"public class C { void Foo($$ ) { } }";
            var expected = @"public class C { void Foo( [|)|] { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen4()
        {
            var code = @"public class C { void Foo( $$) { } }";
            var expected = @"public class C { void Foo[|(|] ) { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen5()
        {
            var code = @"public class C { void Foo( )$$ { } }";
            var expected = @"public class C { void Foo[|(|] ) { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestParen6()
        {
            var code = @"public class C { void Foo()$$ { } }";
            var expected = @"public class C { void Foo[|(|]) { } }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket1()
        {
            var code = @"public class C { int$$[] i; }";
            var expected = @"public class C { int[[|]|] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket2()
        {
            var code = @"public class C { int[$$] i; }";
            var expected = @"public class C { int[[|]|] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket3()
        {
            var code = @"public class C { int[$$ ] i; }";
            var expected = @"public class C { int[ [|]|] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket4()
        {
            var code = @"public class C { int[ $$] i; }";
            var expected = @"public class C { int[|[|] ] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket5()
        {
            var code = @"public class C { int[ ]$$ i; }";
            var expected = @"public class C { int[|[|] ] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestSquareBracket6()
        {
            var code = @"public class C { int[]$$ i; }";
            var expected = @"public class C { int[|[|]] i; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAngleBracket1()
        {
            var code = @"public class C { Foo$$<int> f; }";
            var expected = @"public class C { Foo<int[|>|] f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAngleBracket2()
        {
            var code = @"public class C { Foo<$$int> f; }";
            var expected = @"public class C { Foo<int[|>|] f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAngleBracket3()
        {
            var code = @"public class C { Foo<int$$> f; }";
            var expected = @"public class C { Foo[|<|]int> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestAngleBracket4()
        {
            var code = @"public class C { Foo<int>$$ f; }";
            var expected = @"public class C { Foo[|<|]int> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket1()
        {
            var code = @"public class C { Func$$<Func<int,int>> f; }";
            var expected = @"public class C { Func<Func<int,int>[|>|] f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket2()
        {
            var code = @"public class C { Func<$$Func<int,int>> f; }";
            var expected = @"public class C { Func<Func<int,int>[|>|] f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket3()
        {
            var code = @"public class C { Func<Func$$<int,int>> f; }";
            var expected = @"public class C { Func<Func<int,int[|>|]> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket4()
        {
            var code = @"public class C { Func<Func<$$int,int>> f; }";
            var expected = @"public class C { Func<Func<int,int[|>|]> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket5()
        {
            var code = @"public class C { Func<Func<int,int$$>> f; }";
            var expected = @"public class C { Func<Func[|<|]int,int>> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket6()
        {
            var code = @"public class C { Func<Func<int,int>$$> f; }";
            var expected = @"public class C { Func<Func[|<|]int,int>> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket7()
        {
            var code = @"public class C { Func<Func<int,int> $$> f; }";
            var expected = @"public class C { Func[|<|]Func<int,int> > f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestNestedAngleBracket8()
        {
            var code = @"public class C { Func<Func<int,int>>$$ f; }";
            var expected = @"public class C { Func[|<|]Func<int,int>> f; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestString1()
        {
            var code = @"public class C { string s = $$""Foo""; }";
            var expected = @"public class C { string s = ""Foo[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestString2()
        {
            var code = @"public class C { string s = ""$$Foo""; }";
            var expected = @"public class C { string s = ""Foo[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestString3()
        {
            var code = @"public class C { string s = ""Foo$$""; }";
            var expected = @"public class C { string s = [|""|]Foo""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestString4()
        {
            var code = @"public class C { string s = ""Foo""$$; }";
            var expected = @"public class C { string s = [|""|]Foo""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestString5()
        {
            var code = @"public class C { string s = ""Foo$$ ";
            var expected = @"public class C { string s = ""Foo ";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestVerbatimString1()
        {
            var code = @"public class C { string s = $$@""Foo""; }";
            var expected = @"public class C { string s = @""Foo[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestVerbatimString2()
        {
            var code = @"public class C { string s = @$$""Foo""; }";
            var expected = @"public class C { string s = @""Foo[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestVerbatimString3()
        {
            var code = @"public class C { string s = @""$$Foo""; }";
            var expected = @"public class C { string s = @""Foo[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestVerbatimString4()
        {
            var code = @"public class C { string s = @""Foo$$""; }";
            var expected = @"public class C { string s = [|@""|]Foo""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestVerbatimString5()
        {
            var code = @"public class C { string s = @""Foo""$$; }";
            var expected = @"public class C { string s = [|@""|]Foo""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString1()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""$${x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString2()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{$$x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x[|}|], {y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString3()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x$$}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString4()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}$$, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""[|{|]x}, {y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString5()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, $${y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString6()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {$$y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y[|}|]""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString7()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y$$}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString8()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}$$""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, [|{|]y}""; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString9()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$""{x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString10()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$""{x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $""{x}, {y}[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString11()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $$[||]$@""{x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString12()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $[||]$$@""{x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }";

            Test(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)]
        public void TestInterpolatedString13()
        {
            var code = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@$$""{x}, {y}""; }";
            var expected = @"public class C { void M() { var x = ""Hello""; var y = ""World""; var s = $@""{x}, {y}[|""|]; }";

            Test(code, expected);
        }
    }
}
