// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceHighlighting
{
    public class BraceHighlightingTests : AbstractBraceHighlightingTests
    {
        protected override Task<TestWorkspace> CreateWorkspaceAsync(string markup, ParseOptions options)
        {
            return TestWorkspace.CreateCSharpAsync(markup, parseOptions: options);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestCurlies()
        {
            await TestBraceHighlightingAsync("public class C$$ {\r\n} ");
            await TestBraceHighlightingAsync("public class C $$[|{|]\r\n[|}|] ");
            await TestBraceHighlightingAsync("public class C {$$\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n$$} ");
            await TestBraceHighlightingAsync("public class C [|{|]\r\n[|}|]$$ ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestTouchingItems()
        {
            await TestBraceHighlightingAsync("public class C $$[|{|]\r\n  public void Foo(){}\r\n[|}|] ");
            await TestBraceHighlightingAsync("public class C {$$\r\n  public void Foo(){}\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n  public void Foo$$[|(|][|)|]{}\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n  public void Foo($$){}\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n  public void Foo[|(|][|)|]$$[|{|][|}|]\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n  public void Foo(){$$}\r\n} ");
            await TestBraceHighlightingAsync("public class C {\r\n  public void Foo()[|{|][|}|]$$\r\n} ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestAngles()
        {
            await TestBraceHighlightingAsync("/// $$<summary>Foo</summary>");
            await TestBraceHighlightingAsync("/// <$$summary>Foo</summary>");
            await TestBraceHighlightingAsync("/// <summary$$>Foo</summary>");
            await TestBraceHighlightingAsync("/// <summary>$$Foo</summary>");
            await TestBraceHighlightingAsync("/// <summary>Foo$$</summary>");
            await TestBraceHighlightingAsync("/// <summary>Foo<$$/summary>");
            await TestBraceHighlightingAsync("/// <summary>Foo</$$summary>");
            await TestBraceHighlightingAsync("/// <summary>Foo</summary$$>");
            await TestBraceHighlightingAsync("/// <summary>Foo</summary>$$");

            await TestBraceHighlightingAsync("public class C$$[|<|]T[|>|] { }");
            await TestBraceHighlightingAsync("public class C<$$T> { }");
            await TestBraceHighlightingAsync("public class C<T$$> { }");
            await TestBraceHighlightingAsync("public class C[|<|]T[|>|]$$ { }");

            await TestBraceHighlightingAsync("class C { void Foo() { bool a = b $$< c; bool d = e > f; } }");
            await TestBraceHighlightingAsync("class C { void Foo() { bool a = b <$$ c; bool d = e > f; } }");
            await TestBraceHighlightingAsync("class C { void Foo() { bool a = b < c; bool d = e $$> f; } }");
            await TestBraceHighlightingAsync("class C { void Foo() { bool a = b < c; bool d = e >$$ f; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestSwitch()
        {
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch $$[|(|]variable[|)|]
        {
            case 0:
                break;
        }
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch ($$variable)
        {
            case 0:
                break;
        }
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch (variable$$)
        {
            case 0:
                break;
        }
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch [|(|]variable[|)|]$$
        {
            case 0:
                break;
        }
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch (variable)
       $$[|{|]
            case 0:
                break;
        [|}|]
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch (variable)
        {$$
            case 0:
                break;
        }
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch (variable)
        {
            case 0:
                break;
        $$}
    }
} ");
            await TestBraceHighlightingAsync(@"
class C
{
    void M(int variable)
    {
        switch (variable)
       [|{|]
            case 0:
                break;
        [|}|]$$
    }
} ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestEOF()
        {
            await TestBraceHighlightingAsync("public class C [|{|]\r\n[|}|]$$");
            await TestBraceHighlightingAsync("public class C [|{|]\r\n void Foo(){}[|}|]$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestTuples()
        {
            await TestBraceHighlightingAsync(@" class C { [|(|]int, int[|)|]$$ x = (1, 2); } ", TestOptions.Regular);
            await TestBraceHighlightingAsync(@" class C { (int, int) x = [|(|]1, 2[|)|]$$; } ", TestOptions.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestNestedTuples()
        {
            await TestBraceHighlightingAsync(@" class C { ([|(|]int, int[|)|]$$, string) x = ((1, 2), ""hello""; } ", TestOptions.Regular);
            await TestBraceHighlightingAsync(@" class C { ((int, int), string) x = ([|(|]1, 2[|)|]$$, ""hello""; } ", TestOptions.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestTuplesWithGenerics()
        {
            await TestBraceHighlightingAsync(@" class C { [|(|]Dictionary<int, string>, List<int>[|)|]$$ x = (null, null); } ", TestOptions.Regular);
            await TestBraceHighlightingAsync(@" class C { var x = [|(|]new Dictionary<int, string>(), new List<int>()[|)|]$$; } ", TestOptions.Regular);
        }
    }
}