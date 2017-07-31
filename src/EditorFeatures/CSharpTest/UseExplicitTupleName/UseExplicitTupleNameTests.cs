﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.UseExplicitTupleName;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExplicitTupleName
{
    public class UseExplicitTupleNameTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExplicitTupleNameDiagnosticAnalyzer(), new UseExplicitTupleNameCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestNamedTuple1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.[|Item1|];
    }
}",
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestInArgument()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        Foo(v1.[|Item1|]);
    }

    void Foo(int i) { }
}",
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        Foo(v1.i);
    }

    void Foo(int i) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestNamedTuple2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.[|Item2|];
    }
}",
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestMissingOnMatchingName1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int, string s) v1 = default((int, string));
        var v2 = v1.[|Item1|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestMissingOnMatchingName2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int Item1, string s) v1 = default((int, string));
        var v2 = v1.[|Item1|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestWrongCasing()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int item1, string s) v1 = default((int, string));
        var v2 = v1.[|Item1|];
    }
}",
@"
class C
{
    void M()
    {
        (int item1, string s) v1 = default((int, string));
        var v2 = v1.item1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.{|FixAllInDocument:Item1|};
        var v3 = v1.Item2;
    }
}",
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.i;
        var v3 = v1.s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        (int i, int s) v1 = default((int, int));
        v1.{|FixAllInDocument:Item1|} = v1.Item2;
    }
}",
@"
class C
{
    void M()
    {
        (int i, int s) v1 = default((int, int));
        v1.i = v1.s;
    }
}");
        }
    }
}
