// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues
{
    public partial class RemoveUnusedValueExpressionStatementTests : RemoveUnusedValuesTestsBase
    {
        protected override IDictionary<OptionKey, object> PreferNone =>
            Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.None));

        protected override IDictionary<OptionKey, object> PreferDiscard =>
            Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.Silent));

        protected override IDictionary<OptionKey, object> PreferUnusedLocal =>
            Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption.Silent));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_Suppressed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    int M2() => 0;
}", options: PreferNone);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_PreferDiscard_CSharp6()
        {
            // Discard not supported in C# 6.0, so we fallback to unused local variable.
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        var unused = M2();
    }

    int M2() => 0;
}", options: PreferDiscard,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_VariableInitialization(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x = [|M2()|];
    }

    int M2() => 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task ExpressionStatement_NonConstantPrimitiveTypeValue(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    int M2() => 0;
}",
$@"class C
{{
    void M()
    {{
        {fix} = M2();
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task ExpressionStatement_UserDefinedType(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    C M2() => new C();
}",
$@"class C
{{
    void M()
    {{
        {fix} = M2();
    }}

    C M2() => new C();
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_ConstantValue(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|1|];
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_SyntaxError(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2(,)|];
    }

    int M2() => 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_SemanticError(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }
}", optionName);
        }

        [WorkItem(33073, "https://github.com/dotnet/roslyn/issues/33073")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_SemanticError_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    UndefinedType M2() => null;
}", optionName);
        }

        [WorkItem(33073, "https://github.com/dotnet/roslyn/issues/33073")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_SemanticError_03(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    private async Task M()
    {
        // error CS0103: The name 'CancellationToken' does not exist in the current context
        [|await Task.Delay(0, CancellationToken.None).ConfigureAwait(false)|];
    }
}", optionName);
        }

        [WorkItem(33073, "https://github.com/dotnet/roslyn/issues/33073")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_SemanticError_04(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private async Task M()
    {
        // error CS0103: The name 'Task' does not exist in the current context
        // error CS0103: The name 'CancellationToken' does not exist in the current context
        // error CS1983: The return type of an async method must be void, Task or Task<T>
        [|await Task.Delay(0, CancellationToken.None).ConfigureAwait(false)|];
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionStatement_VoidReturningMethodCall(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
    }

    void M2() { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("=")]
        [InlineData("+=")]
        public async Task ExpressionStatement_AssignmentExpression(string op)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    void M(int x)
    {{
        x {op} [|M2()|];
    }}

    int M2() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("x++")]
        [InlineData("x--")]
        [InlineData("++x")]
        [InlineData("--x")]
        public async Task ExpressionStatement_IncrementOrDecrement(string incrementOrDecrement)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int x)
    {{
        [|{incrementOrDecrement}|];
        return x;
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_UnusedLocal_NameAlreadyUsed()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var unused = M2();
        [|M2()|];
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        var unused = M2();
        var unused1 = M2();
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M2()|];
        var unused = M2();
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        var unused1 = M2();
        var unused = M2();
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_03()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int p)
    {
        [|M2()|];
        if (p > 0)
        {
            var unused = M2();
        }
    }

    int M2() => 0;
}",
@"class C
{
    void M(int p)
    {
        var unused1 = M2();
        if (p > 0)
        {
            var unused = M2();
        }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int p)
    {
        if (p > 0)
        {
            [|M2()|];
        }
        else
        {
            var unused = M2();
        }
    }

    int M2() => 0;
}",
@"class C
{
    void M(int p)
    {
        if (p > 0)
        {
            var unused1 = M2();
        }
        else
        {
            var unused = M2();
        }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_", "_", "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused", "var unused", "var unused3")]
        public async Task ExpressionStatement_FixAll(string optionName, string fix1, string fix2, string fix3)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        M2();           // Separate code block
    }

    void M(int unused1, int unused2)
    {
        {|FixAllInDocument:M2()|};
        M2();           // Another instance in same code block
        _ = M2();       // Already fixed
        var x = M2();   // Different unused value diagnostic
    }

    int M2() => 0;
}",
$@"class C
{{
    public C()
    {{
        {fix1} = M2();           // Separate code block
    }}

    void M(int unused1, int unused2)
    {{
        {fix2} = M2();
        {fix3} = M2();           // Another instance in same code block
        _ = M2();       // Already fixed
        var x = M2();   // Different unused value diagnostic
    }}

    int M2() => 0;
}}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_Trivia_PreferDiscard_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // C1
        [|M2()|];   // C2
        // C3
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        // C1
        _ = M2();   // C2
        // C3
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_Trivia_PreferDiscard_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {/*C0*/
        /*C1*/[|M2()|]/*C2*/;/*C3*/
     /*C4*/
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {/*C0*/
        /*C1*/
        _ = M2()/*C2*/;/*C3*/
     /*C4*/
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_Trivia_PreferUnusedLocal_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // C1
        [|M2()|];   // C2
        // C3
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        // C1
        var unused = M2();   // C2
        // C3
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExpressionStatement_Trivia_PreferUnusedLocal_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {/*C0*/
        /*C1*/[|M2()|]/*C2*/;/*C3*/
     /*C4*/
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {/*C0*/
        /*C1*/
        var unused = M2()/*C2*/;/*C3*/
        /*C4*/
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [WorkItem(32942, "https://github.com/dotnet/roslyn/issues/32942")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionBodiedMember_01(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M() => [|M2()|];
    int M2() => 0;
}", optionName);
        }

        [WorkItem(32942, "https://github.com/dotnet/roslyn/issues/32942")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionBodiedMember_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        System.Action a = () => [|M2()|];
    }

    int M2() => 0;
}", optionName);
        }

        [WorkItem(32942, "https://github.com/dotnet/roslyn/issues/32942")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ExpressionBodiedMember_03(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        LocalFunction();
        return;

        void LocalFunction() => [|M2()|];
    }

    int M2() => 0;
}", optionName);
        }
    }
}
