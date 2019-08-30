// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseExplicitTupleName;
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
        Goo(v1.[|Item1|]);
    }

    void Goo(int i) { }
}",
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        Goo(v1.i);
    }

    void Goo(int i) { }
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestFalseOptionImplicitTuple()
        {
            await TestDiagnosticMissingAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.[|Item1|];
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferExplicitTupleNames, false, NotificationOption.Warning)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestFalseOptionExplicitTuple()
        {
            await TestDiagnosticMissingAsync(
@"
class C
{
    void M()
    {
        (int i, string s) v1 = default((int, string));
        var v2 = v1.[|i|];
    }
}", new TestParameters(options: Option(CodeStyleOptions.PreferExplicitTupleNames, false, NotificationOption.Warning)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)]
        public async Task TestOnRestField()
        {
            string valueTuple8 = @"
namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
        }
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
        }
    }
}
";
            await TestDiagnosticMissingAsync(
@"
class C
{
    void M()
    {
        (int, int, int, int, int, int, int, int) x = default;
        _ = x.[|Rest|];
    }
}" + valueTuple8);
        }
    }
}
