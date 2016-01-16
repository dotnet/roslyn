// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceHighlighting
{
    public class BraceHighlightingTests : AbstractBraceHighlightingTests
    {
        protected override Task<TestWorkspace> CreateWorkspaceAsync(string markup)
        {
            return TestWorkspaceFactory.CreateCSharpWorkspaceAsync(markup);
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
    }
}