// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        protected override TestWorkspace CreateWorkspace(string markup)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromFile(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public void TestCurlies()
        {
            TestBraceHighlighting("public class C$$ {\r\n} ");
            TestBraceHighlighting("public class C $$[|{|]\r\n[|}|] ");
            TestBraceHighlighting("public class C {$$\r\n} ");
            TestBraceHighlighting("public class C {\r\n$$} ");
            TestBraceHighlighting("public class C [|{|]\r\n[|}|]$$ ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public void TestTouchingItems()
        {
            TestBraceHighlighting("public class C $$[|{|]\r\n  public void Foo(){}\r\n[|}|] ");
            TestBraceHighlighting("public class C {$$\r\n  public void Foo(){}\r\n} ");
            TestBraceHighlighting("public class C {\r\n  public void Foo$$[|(|][|)|]{}\r\n} ");
            TestBraceHighlighting("public class C {\r\n  public void Foo($$){}\r\n} ");
            TestBraceHighlighting("public class C {\r\n  public void Foo[|(|][|)|]$$[|{|][|}|]\r\n} ");
            TestBraceHighlighting("public class C {\r\n  public void Foo(){$$}\r\n} ");
            TestBraceHighlighting("public class C {\r\n  public void Foo()[|{|][|}|]$$\r\n} ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public void TestAngles()
        {
            TestBraceHighlighting("/// $$<summary>Foo</summary>");
            TestBraceHighlighting("/// <$$summary>Foo</summary>");
            TestBraceHighlighting("/// <summary$$>Foo</summary>");
            TestBraceHighlighting("/// <summary>$$Foo</summary>");
            TestBraceHighlighting("/// <summary>Foo$$</summary>");
            TestBraceHighlighting("/// <summary>Foo<$$/summary>");
            TestBraceHighlighting("/// <summary>Foo</$$summary>");
            TestBraceHighlighting("/// <summary>Foo</summary$$>");
            TestBraceHighlighting("/// <summary>Foo</summary>$$");

            TestBraceHighlighting("public class C$$[|<|]T[|>|] { }");
            TestBraceHighlighting("public class C<$$T> { }");
            TestBraceHighlighting("public class C<T$$> { }");
            TestBraceHighlighting("public class C[|<|]T[|>|]$$ { }");

            TestBraceHighlighting("class C { void Foo() { bool a = b $$< c; bool d = e > f; } }");
            TestBraceHighlighting("class C { void Foo() { bool a = b <$$ c; bool d = e > f; } }");
            TestBraceHighlighting("class C { void Foo() { bool a = b < c; bool d = e $$> f; } }");
            TestBraceHighlighting("class C { void Foo() { bool a = b < c; bool d = e >$$ f; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public void TestSwitch()
        {
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
            TestBraceHighlighting(@"
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
        public void TestEOF()
        {
            TestBraceHighlighting("public class C [|{|]\r\n[|}|]$$");
            TestBraceHighlighting("public class C [|{|]\r\n void Foo(){}[|}|]$$");
        }
    }
}