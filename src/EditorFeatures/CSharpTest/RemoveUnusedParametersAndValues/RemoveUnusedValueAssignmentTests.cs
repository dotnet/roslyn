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
    public partial class RemoveUnusedValueAssignmentTests : RemoveUnusedValuesTestsBase
    {
        protected override IDictionary<OptionKey, object> PreferNone =>
            Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.None));

        protected override IDictionary<OptionKey, object> PreferDiscard =>
            Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption.Suggestion));

        protected override IDictionary<OptionKey, object> PreferUnusedLocal =>
            Option(CSharpCodeStyleOptions.UnusedValueAssignment,
                   new CodeStyleOption<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption.Suggestion));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_Suppressed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1;
        x = 2;
        return x;
    }
}", options: PreferNone);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Assignment_Suppressed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        x = 2;
        return x;
    }
}", options: PreferNone);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_ConstantValue_RemoveUnsuedParametersSuppressed()
        {
            var removeUnusedParametersSuppressed = Option(CodeStyleOptions.UnusedParameters,
                new CodeStyleOption<UnusedParametersPreference>(UnusedParametersPreference.NonPublicMethods, NotificationOption.None));

            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x = 2;
        return x;
    }
}", options: removeUnusedParametersSuppressed);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_ConstantValue_RemoveUnsuedParametersNotApplicable()
        {
            var removeUnusedParametersNotApplicable = Option(CodeStyleOptions.UnusedParameters,
                new CodeStyleOption<UnusedParametersPreference>(UnusedParametersPreference.NonPublicMethods, NotificationOption.Silent));

            await TestInRegularAndScriptAsync(
@"class C
{
    public int M(int z)
    {
        int [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    public int M(int z)
    {
        int x = 2;
        return x;
    }
}", options: removeUnusedParametersNotApplicable);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_ConstantValue(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x;
        x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_ConstantValue_NoReads(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        [|x|] = 1;
    }
}",
@"class C
{
    void M()
    {
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Assignment_NonConstantValue_NoReads_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        [|x|] = M2();
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        _ = M2();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Assignment_NonConstantValue_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        [|x|] = M2();
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_NonConstantValue_ParameterReference(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int p)
    {
        int [|x|] = p;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M(int p)
    {
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_NonConstantValue_ParameterReference(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int p)
    {
        int x;
        [|x|] = p;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M(int p)
    {
        int x;
        x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_NonConstantValue_LocalReference(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int local = 0;
        int [|x|] = local;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int local = 0;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_NonConstantValue_LocalReference(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int local = 0;
        int x;
        [|x|] = local;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int local = 0;
        int x;
        x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_NonConstantValue_DefaultExpression(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"struct C
{
    C M()
    {
        C [|c|] = default(C);
        c = new C();
        return c;
    }
}",
@"struct C
{
    C M()
    {
        C c = new C();
        return c;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_NonConstantValue_CastExpression(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"struct C
{
    C M(object obj)
    {
        C [|c|] = (C)obj;
        c = new C();
        return c;
    }
}",
@"struct C
{
    C M(object obj)
    {
        C c = new C();
        return c;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_NonConstantValue_FieldReferenceWithThisReceiver(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int field;
    int M()
    {
        int [|x|] = field;
        x = 2;
        return x;
    }
}",
@"class C
{
    private int field;
    int M()
    {
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_NonConstantValue_FieldReferenceWithNullReceiver(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private static int field;
    int M()
    {
        int x;
        [|x|] = field;
        x = 2;
        return x;
    }
}",
@"class C
{
    private static int field;
    int M()
    {
        int x;
        x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task Assignment_NonConstantValue_FieldReferenceWithReceiver(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int field;
    int M(C c)
    {
        int x;
        [|x|] = c.field;
        x = 2;
        return x;
    }
}",
$@"class C
{{
    private int field;
    int M(C c)
    {{
        int x;
        {fix} = c.field;
        x = 2;
        return x;
    }}
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task Initialization_NonConstantValue_PropertyReference(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int Property { get { throw new System.Exception(); } }
    int M()
    {
        int x;
        [|x|] = Property;
        x = 2;
        return x;
    }
}",
$@"class C
{{
    private int Property {{ get {{ throw new System.Exception(); }} }}
    int M()
    {{
        int x;
        {fix} = Property;
        x = 2;
        return x;
    }}
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task Initialization_NonConstantValue_MethodInvocation(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
$@"class C
{{
    int M()
    {{
        {fix} = M2();
        int x = 2;
        return x;
    }}

    int M2() => 0;
}}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_PreferDiscard_CSharp6()
        {
            // Discard not supported in C# 6.0, so we fallback to unused local variable.
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        int unused = M2();
        int x = 2;
        return x;
    }

    int M2() => 0;
}", options: PreferDiscard,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task Assignment_NonConstantValue_MethodInvocation(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
$@"class C
{{
    int M()
    {{
        int x;
        {fix} = M2();
        x = 2;
        return x;
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_NonConstantValue_ImplicitConversion(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x, short s)
    {
        [|x|] = s;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M(int x, short s)
    {
        x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task Assignment_NonConstantValue_UserDefinedConversion(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x, C c)
    {
        [|x|] = (int)c;
        x = 2;
        return x;
    }

    public static explicit operator int(C c)
    {
        return 0;
    }

    public static explicit operator C(int i)
    {
        return default(C);
    }
}",
$@"class C
{{
    int M(int x, C c)
    {{
        {fix} = (int)c;
        x = 2;
        return x;
    }}

    public static explicit operator int(C c)
    {{
        return 0;
    }}

    public static explicit operator C(int i)
    {{
        return default(C);
    }}
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NestedAssignment_ConstantValue(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x, int y)
    {
        y = [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M(int x, int y)
    {
        y = 1;
        x = 2;
        return x;
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NestedAssignment_NonConstantValue_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x, int y)
    {
        y = [|x|] = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
@"class C
{
    int M(int x, int y)
    {
        y = _ = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NestedAssignment_NonConstantValue_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x, int y)
    {
        y = [|x|] = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}",
@"class C
{
    int M(int x, int y)
    {
        int unused;
        y = unused = M2();
        x = 2;
        return x;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ReadAndWriteInSameExpression_MethodInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1;
        x = M2(x);
        return x;
    }

    int M2(int x) => x;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("++", "")]
        [InlineData("", "++")]
        [InlineData("--", "")]
        [InlineData("", "--")]
        public async Task IncrementOrDecrementOperator_ValueUsed_SameStatement(string prefix, string postfix)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    void M(int x)
    {{
        var y = {prefix}[|x|]{postfix};
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("++", "")]
        [InlineData("", "++")]
        [InlineData("--", "")]
        [InlineData("", "--")]
        public async Task IncrementOrDecrementOperator_ValueUsed_LaterStatement(string prefix, string postfix)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int x)
    {{
        {prefix}[|x|]{postfix};
        return x;
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("++", "")]
        [InlineData("", "++")]
        [InlineData("--", "")]
        [InlineData("", "--")]
        public async Task IncrementOrDecrementOperator_ValueUnused(string prefix, string postfix)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    void M(int x)
    {{
        {prefix}[|x|]{postfix};
    }}
}}",
@"class C
{
    void M(int x)
    {
    }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("1")]       // Constant
        [InlineData("M2()")]    // Non-constant
        public async Task CompoundAssignmentOperator_ValueUsed_SameStatement(string rightHandSide)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    void M(int x)
    {{
        var y = [|x|] += {rightHandSide};
    }}

    int M2() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("1")]       // Constant
        [InlineData("M2()")]    // Non-constant
        public async Task CompoundAssignmentOperator_ValueUsed_LaterStatement(string rightHandSide)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int x)
    {{
        [|x|] += {rightHandSide};
        return x;
    }}

    int M2() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("true")]    // Constant
        [InlineData("M2()")]    // Non-constant
        public async Task CompoundLogicalOrOperator_ValueUsed_LaterStatement(string rightHandSide)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    bool M(bool x)
    {{
        [|x|] |= {rightHandSide} && {rightHandSide};
        return x;
    }}

    bool M2() => true;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("true")]    // Constant
        [InlineData("M2()")]    // Non-constant
        public async Task CompoundLogicalOrOperator_ValueUsed_LaterStatement_02(string rightHandSide)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    bool M()
    {{
        bool [|x|] = false;
        x |= {rightHandSide} && {rightHandSide};
        return x;
    }}

    bool M2() => true;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task CompoundAssignmentOperator_ValueNotUsed_ConstantValue(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        [|x|] += 1;
    }
}",
@"class C
{
    int M(int x)
    {
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task CompoundAssignmentOperator_ValueNotUsed_NonConstantValue(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    int M(int x)
    {{
        [|x|] += M2();
    }}

    int M2() => 0;
}}",
$@"class C
{{
    int M(int x)
    {{
        {fix} = M2();
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NullCoalescing_ReadWrite(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M()
    {
        C [|x|] = M2();
        x = x ?? new C();
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedTarget(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y)
    {
        C [|x|] = M2();
        (x ?? y) = y;
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedValue_01(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        var [|x|] = M2();
        x = y ?? z;
        return x;
    }

    C M2() => null;
}",
$@"class C
{{
    C M(C y, C z)
    {{
        {fix} = M2();
        C x = y ?? z;
        return x;
    }}

    C M2() => null;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_Assignment_ControlFlowInAssignedValue_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        C [|x|] = M2();
        x = y ?? (x ?? z);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedTarget(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y)
    {
        C [|x|] = M2();
        ((x ?? y), _) = (y, y);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedValue_01(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        var [|x|] = M2();
        (x, y) = (y ?? z, z);
        return x;
    }

    C M2() => null;
}",
$@"class C
{{
    C M(C y, C z)
    {{
        {fix} = M2();
        C x;
        (x, y) = (y ?? z, z);
        return x;
    }}

    C M2() => null;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task LValueFlowCapture_DeconstructionAssignment_ControlFlowInAssignedValue_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C M(C y, C z)
    {
        C [|x|] = M2();
        (x, y) = (y ?? x, z);
        return x;
    }

    C M2() => null;
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_NoReferences_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        _ = M2();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_NoReferences_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
    }

    int M2() => 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_NoReadReferences_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
        x = 0;
    }

    int M2() => 0;
}",
@"class C
{
    void M()
    {
        _ = M2();
        int x = 0;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_NoReadReferences_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int [|x|] = M2();
        x = 0;
    }

    int M2() => 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_FirstField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 1, y = 2;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_MiddleField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = 1, y = 2;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Initialization_ConstantValue_LastField(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = 1;
        x = 2;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        int x = 2;
        return x;
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_FirstField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        _ = M2();
        int y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_FirstField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int unused = M2(), y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_MiddleField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0;
        _ = M2();
        int y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_MiddleField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, [|x|] = M2(), y = 2;
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, unused = M2(), y = 2;
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_LastField_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = M2();
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2;
        _ = M2();
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task Initialization_NonConstantValue_LastField_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int i = 0, y = 2, [|x|] = M2();
        x = 2;
        return x;
    }

    void M2() => 0;
}",
@"class C
{
    int M()
    {
        int i = 0, y = 2, unused = M2();
        int x = 2;
        return x;
    }

    void M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Assignment_BeforeUseAsOutArgument(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(out x);
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        M2(out x);
        return x;
    }

    void M2(out int x) => x = 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_BeforeUseAsRefArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(ref x);
        return x;
    }

    void M2(ref int x) => x = 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantAssignment_BeforeUseAsInArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        [|x|] = 1;
        M2(in x);
        return x;
    }

    void M2(in int x) { }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task OutArgument_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        M2(out [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        M2(out _);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task OutArgument_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x;
        M2(out [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    int M()
    {
        int x;
        int unused;
        M2(out unused);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task OutVarArgument_ExpressionBody_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M() => M2(out var [|x|]);
    void M2(out int x) => x = 0;
}",
@"class C
{
    void M() => M2(out _);
    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task OutArgument_NoReads_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        M2(out [|x|]);

        // Unrelated, unused local should not be removed.
        int unused;
    }

    void M2(out int x) => x = 0;
}",
@"class C
{
    void M()
    {
        M2(out _);

        // Unrelated, unused local should not be removed.
        int unused;
    }

    void M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task OutArgument_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        M2(out [|x|]);
    }

    void M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task OutDeclarationExpressionArgument(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        M2(out var [|x|]);
        x = 1;
        return x;
    }

    void M2(out int x) => x = 0;
}",
$@"class C
{{
    int M()
    {{
        M2(out {fix});
        int x = 1;
        return x;
    }}

    void M2(out int x) => x = 0;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantRefArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        M2(ref [|x|]);
        x = 1;
        return x;
    }

    void M2(ref int x) => x = 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NonRedundantInArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int x)
    {
        M2(in [|x|]);
        x = 1;
        return x;
    }

    void M2(in int x) { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "unused")]
        public async Task DeconstructionDeclarationExpression(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        var ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
$@"class C
{{
    int M()
    {{
        var ({fix}, y) = (1, 1);
        int x = 1;
        return x;
    }}
}}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeconstructionAssignment_01_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x, y;
        ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x, y;
        (_, y) = (1, 1);
        x = 1;
        return x;
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeconstructionAssignment_01_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int x, y;
        ([|x|], y) = (1, 1);
        x = 1;
        return x;
    }
}",
@"class C
{
    int M()
    {
        int x, y;
        int unused;
        (unused, y) = (1, 1);
        x = 1;
        return x;
    }
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task DeconstructionAssignment_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        int [|x|] = 0, y = 0;
        (x, y) = (x, y);
        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "var unused")]
        public async Task TupleExpressionWithDeclarationExpressions(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        (var [|x|], var y) = (1, 1);
        x = 1;
        return x;
    }
}",
$@"class C
{{
    int M()
    {{
        ({fix}, var y) = (1, 1);
        int x = 1;
        return x;
    }}
}}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                break;
        };
    }
}",
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int _:
                int x;
                x = 1;
                break;
        };
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInSwitchCase_WithOnlyWriteReference_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                break;
        };
    }
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInIsPattern_WithNoReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
        }
    }
}",
@"class C
{
    void M(object p)
    {
        if (p is C)
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInIsPattern_WithNoReference_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
        }
    }
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInIsPattern_WithOnlyWriteReference_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
        }
    }
}",
@"class C
{
    void M(object p)
    {
        if (p is C)
        {
            C x = null;
        }
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task DeclarationPatternInIsPattern_WithOnlyWriteReference_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
        }
    }
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "C")]
        [InlineData(nameof(PreferUnusedLocal), "C unused")]
        public async Task DeclarationPatternInIsPattern_WithReadAndWriteReference(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        if (p is C [|x|])
        {
            x = null;
            p = x;
        }
    }
}",
$@"class C
{{
    void M(object p)
    {{
        if (p is {fix})
        {{
            C x = null;
            p = x;
        }}
    }}
}}", optionName: optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;
        lambda();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithInvocation_DefinedAtStart(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithInvocation_DefinedAtEnd(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        LocalFunction();

        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        Action lambda = () =>
        {
            var x = p;
        };
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
        };
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithoutInvocation_DefinedAtStart(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
        [|p|] = null;
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithoutInvocation_DefinedAtEnd(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        void LocalFunction()
        {
            var x = p;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLambda_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        [|p|] = null;
        lambda();
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        lambda();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLambda_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        Action lambda = () =>
        {
        };
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithoutInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        void LocalFunction()
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLambda_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
        };
        lambda();
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
        };
        lambda();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLocalFunction_WithInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
            [|p|] = null;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WriteThenReadInLambda_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
            var x = p;
        };
        lambda();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WriteThenReadInLocalFunction_WithInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        LocalFunction();
        void LocalFunction()
        {
            [|p|] = null;
            var x = p;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLambda_WithoutInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            [|p|] = null;
        };
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task RedundantWriteInLocalFunction_WithoutInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            [|p|] = null;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_Nested(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action outerLambda = () =>
        {
            Action innerLambda = () =>
            {
                var x = p;
            };

            innerLambda();
        });

        [|p|] = null;
        outerLambda();
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedLocalFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        OuterLocalFunction();

        void OuterLocalFunction()
        {
            InnerLocalFunction();

            void InnerLocalFunction()
            {
                var x = p;
            }
        });
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_NestedLocalFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<Action> outerDelegate)
    {
        [|p|] = null;
        outerDelegate(() =>
        {
            InnerLocalFunction();
            void InnerLocalFunction()
            {
                var x = p;
            }
        });
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<Action> myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();

        void OuterLocalFunction()
        {
            myDelegate(() =>
            {
                var x = p;
            });
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInNestedLambda_InvokedInOuterFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();
        myDelegate();

        void OuterLocalFunction()
        {
            myDelegate = () =>
            {
                var x = p;
            };
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInNestedLocalFunction_InvokedInOuterFunction(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action myDelegate)
    {
        [|p|] = null;
        OuterLocalFunction();
        myDelegate();

        void OuterLocalFunction()
        {
            myDelegate = NestedLocalFunction;
            void NestedLocalFunction()
            {
                var x = p;
            }
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ArgumentToLambda(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, Action<object> myDelegate)
    {
        [|p|] = null;
        myDelegate(p);
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ArgumentToLambda_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action<int> M(object p, Action<object> myDelegate)
    {
        [|p|] = null;
        return d => { myDelegate(0); };
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(() =>
        {
            var x = p;
        });
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_02(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    public C(bool flag)
    {
        Flag = flag;
    }

    public bool Flag { get; }
    public static bool M()
    {
        bool flag = true;
        var c = Create(() => flag);

        M2(c);
        [|flag|] = false;
        return M2(c);
    }

    private static C Create(Func<bool> isFlagTrue) { return new C(isFlagTrue()); }
    private static bool M2(C c) => c.Flag;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_CustomDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public delegate void MyAction();

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(() =>
        {
            var x = p;
        });
    }

    void M2(MyAction a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument_CustomDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public delegate void MyAction();

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(MyAction a) => a();
}", optionName);
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UnusedInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3());
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3() { return 0; }
}", optionName);
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ReadInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3(p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(object o) { return 0; }
}", optionName);
        }

        [WorkItem(31744, "https://github.com/dotnet/roslyn/issues/31744")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task OnlyWrittenInExpressionTree_PassedAsArgument(string optionName)
        {
            // Currently we bail out of analysis in presence of expression trees.
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    public static void M1()
    {
        object [|p|] = null;
        M2(x => x.M3(out p));
    }

    private static C M2(Expression<Func<C, int>> a) { return null; }
    private int M3(out object o) { o = null; return 0; }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_PassedAsArgument_CastFromDelegateType(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action a = () =>
        {
            var x = p;
        };

        object o = a;
        [|p|] = null;
        M2(o);
    }

    void M2(object a) => ((Action)a)();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_PassedAsArgument_CastFromDelegateType(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        object o = (Action)LocalFunction;
        [|p|] = null;
        M2(o);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(object a) => ((Action)a)();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_DelegateCreationPassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(new Action(() =>
        {
            var x = p;
        }));
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_DelegateCreationPassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        [|p|] = null;
        M2(new Action(LocalFunction));

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action local = () =>
        {
            var x = p;
        };

        [|p|] = null;
        M2(local);
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action local = LocalFunction;
        [|p|] = null;
        M2(local);

        void LocalFunction()
        {
            var x = p;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLambda_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action local = () =>
        {
            p = p2;
        };

        [|p|] = null;
        M2(local);

        var x = p;
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLocalFunction_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action local = LocalFunction;
        [|p|] = null;
        M2(local);
        var x = p;

        void LocalFunction()
        {
            p = p2;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task WrittenInLambdaAndLocalFunctionTargets_DelegatePassedAsArgument(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, object p2)
    {
        Action lambda = () =>
        {
            p = p2;
        };

        Action myDelegate;
        if (p2 != null)
        {
            myDelegate = lambda;
        }
        else
        {
            myDelegate = LocalFunction;
        }

        [|p|] = null;
        M2(myDelegate);

        var x = p;

        void LocalFunction()
        {
            p = p2;
        }
    }

    void M2(Action a) => a();
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ReturnedDelegateCreation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return new Action(() =>
        {
            var x = p;
        });
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_ReturnedDelegateCreation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return new Action(LocalFunction);

        void LocalFunction()
        {
            var x = p;
        };
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_ReturnedDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        Action local = () =>
        {
            var x = p;
        };

        [|p|] = null;
        return local;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_ReturnedDelegate(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    Action M(object p)
    {
        [|p|] = null;
        return LocalFunction;

        void LocalFunction()
        {
            var x = p;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_InvokedDelegate_ControlFlow(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag)
    {
        Action local1 = () =>
        {
            var x = p;
        };

        Action local2 = () => { };

        [|p|] = null;
        var y = flag ? local1 : local2;
        y();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_InvokedDelegate_ControlFlow(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag)
    {
        [|p|] = null;
        (flag ? LocalFunction1 : (Action)LocalFunction2)();

        void LocalFunction1()
        {
            var x = p;
        }

        void LocalFunction2()
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_LambdaAndLocalFunctionTargets(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;
        var y = flag ? lambda : (flag2 ? (Action)LocalFunction : M2);
        y();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUsed_LambdaAndLocalFunctionTargets(string optionName)
        {
            // Below should be changed to verify diagnostic/fix once we
            // perform points-to-analysis for accurate delegate target tracking.
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2)
    {
        Action lambda = () =>
        {
        };

        [|p|] = null;
        var y = flag ? lambda : (flag2 ? (Action)LocalFunction : M2);
        y();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLambda_LambdaAndLocalFunctionTargets_ThroughLocalsAndParameters(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
            var x = p;
        };

        [|p|] = null;

        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUsed_LambdaAndLocalFunctionTargets_ThroughLocalsAndParameters(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
        };

        [|p|] = null;

        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}",
@"using System;

class C
{
    void M(object p, bool flag, bool flag2, Action param)
    {
        Action lambda = () =>
        {
        };
        Action y;
        if (flag)
        {
            if (flag2)
            {
                y = (Action)LocalFunction;
            }
            else
            {
                y = M2;
            }
        }
        else
        {
            y = null;
            if (flag2)
            {
                param = lambda;
            }
            else
            {
                param = M2;
            }
        }

        Action z;
        if (y != null)
        {
            z = y;
        }
        else
        {
            z = param;
        }

        z();

        void LocalFunction() { }
    }

    void M2() { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_UsedAfterInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };
        a();

        return x;
    }
}
", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_UsedAfterInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        a();

        return x;

        void a()
        {
            [|x|] = 1;
        }
    }
}
", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_UsedAfterSecondInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };

        a();
        a();

        return x;
    }
}
", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_UsedAfterSecondInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x)
    {
        a();
        a();

        return x;

        void a()
        {
            [|x|] = 1;
        }
    }
}
", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_MayBeUsedAfterOneOfTheInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x, bool flag, bool flag2)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };

        a();

        if (flag)
        {
            a();
            if (flag2)
            {
                return x;
            }
        }

        return 0;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_MayBeUsedAfterOneOfTheInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    int M(int x, bool flag, bool flag2)
    {
        a();

        if (flag)
        {
            a();
            if (flag2)
            {
                return x;
            }
        }

        return 0;

        void a()
        {
            [|x|] = 1;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLambda_NotUsedAfterInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        Action a = () =>
        {
            [|x|] = 1;
        };
        a();
    }
}
",
@"using System;

class C
{
    void M(int x)
    {
        Action a = () =>
        {
        };
        a();
    }
}
", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInLocalFunction_NotUsedAfterInvocation(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        a();

        void a()
        {
            [|x|] = 1;
        }
    }
}",
@"using System;

class C
{
    void M(int x)
    {
        a();

        void a()
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_WithRecursiveInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            var x = p;
            LocalFunction();
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task NotUseInLocalFunction_WithRecursiveInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        void LocalFunction()
        {
            LocalFunction();
        }

        [|p|] = null;
        LocalFunction();
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Lambda_WithNonReachableExit(string optionName)
        {
            // We bail out from analysis for delegate passed an argument.
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action throwEx = () =>
        {
            throw new Exception();
        };

        [|p|] = null;
        M2(throwEx);
    }

    void M2(Action a) { }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task Lambda_WithMultipleInvocations(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        Action lambda = () =>
        {
            var x = p;
            [|p|] = null;   // This write is read on next invocation of lambda.
        };

        M2(lambda);
    }

    void M2(Action a)
    {
        a();
        a();
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task UnusedValue_DelegateTypeOptionalParameter_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [|x|] = M2();
    }

    C M2(Action c = null) => null;
}",
@"using System;

class C
{
    void M()
    {
        _ = M2();
    }

    C M2(Action c = null) => null;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task UnusedValue_DelegateTypeOptionalParameter_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var [|x|] = M2();
    }

    C M2(Action c = null) => null;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UseInLocalFunction_NestedInvocation(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        LocalFunction();

        bool LocalFunction2()
        {
            return true;
        }

        void LocalFunction()
        {
            object [|p|] = null;
            if (LocalFunction2())
            {
            }

            if (p != null)
            {
            }
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "unused")]
        public async Task DeclarationPatternInSwitchCase_WithReadAndWriteReferences(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object p)
    {
        switch (p)
        {
            case int [|x|]:
                x = 1;
                p = x;
                break;
        }
    }
}",
$@"class C
{{
    void M(object p)
    {{
        switch (p)
        {{
            case int {fix}:
                int x;
                x = 1;
                p = x;
                break;
        }}
    }}
}}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task CatchClause_ExceptionVariable_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception)
        {
        }
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task CatchClause_ExceptionVariable_PreferUnusedLocal_01()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
        }
    }
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task CatchClause_ExceptionVariable_PreferUnusedLocal_02()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception [|ex|])
        {
            ex = null;
            var x = ex;
        }
    }
}",
@"using System;

class C
{
    void M(object p)
    {
        try
        {
        }
        catch (Exception unused)
        {
            Exception ex = null;
            var x = ex;
        }
    }
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInCatchFilter(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        catch (Exception) when (x != 0)
        {
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedOutsideTry_UsedOnlyInFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        [|x|] = 0;
        try
        {
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInsideTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            [|x|] = 0;
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInsideNestedBlockInTry_UsedOnlyInCatchClause(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            if (x > 0)
            {
                [|x|] = 0;
            }
        }
        catch (Exception)
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchClause_UsedAfterTryCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception)
        {
            [|x|] = 0;
        }

        var y = x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInNestedCatchClause_UsedInOuterFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            try
            {
            }
            catch (Exception)
            {
                [|x|] = 0;
            }
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchClause_UsedInFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception)
        {
            [|x|] = 0;
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInCatchFilter_UsedAfterTryCatch(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        catch (Exception) when (M2(out [|x|]))
        {
        }

        var y = x;
    }

    bool M2(out int x) { x = 0; return true; }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInFinally_UsedAfterTryFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
        }
        finally
        {
            [|x|] = 0;
        }

        var y = x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task AssignedInNestedFinally_UsedInOuterFinally(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(int x)
    {
        try
        {
            try
            {
            }
            finally
            {
                [|x|] = 0;
            }
        }
        finally
        {
            var y = x;
        }
    }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElse_AssignedInCondition_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out _))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElse_DeclaredInCondition_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out _))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElseAssignedInCondition_ReadAfter_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    int M(bool flag)
    {
        int x;
        int unused;
        if (M2(out unused))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElse_AssignedInCondition_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        int x;
        if (M2(out [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElse_DeclaredInCondition_ReadAfter_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}",
@"class C
{
    int M(bool flag)
    {
        int x;
        if (M2(out var unused))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }

        return x;
    }

    bool M2(out int x) => x = 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task IfElse_DeclaredInCondition_NoReads_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool flag)
    {
        if (M2(out var [|x|]))
        {
            x = 2;
        }
        else
        {
            x = 3;
        }
    }

    bool M2(out int x) => x = 0;
}", new TestParameters(options: PreferUnusedLocal));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // Simple if-else.
        [InlineData("x = 1;", "x = 2;")]
        // Nested if-else.
        [InlineData("if(flag) { x = 1; } else { x = 2; }",
                    "x = 3;")]
        // Multiple nested paths.
        [InlineData("if(flag) { x = 1; } else { x = 2; }",
                    "if(flag) { x = 3; } else { x = 4; }")]
        // Nested if-elseif-else.
        [InlineData("if(flag) { x = 1; } else if(flag2) { x = 2; } else { x = 3; }",
                    "if(flag) { x = 5; } else { x = 6; }")]
        //Multi-level nesting.
        [InlineData(@"if(flag) { x = 1; } else { if(flag2) { if(flag3) { x = 2; } else { x = 3; } } else { x = 4; } }",
                    @"x = 5;")]
        public async Task IfElse_OverwrittenInAllControlFlowPaths(string ifBranchCode, string elseBranchCode)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int [|x|] = 1;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}",
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int x;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // Overwrite missing in if path.
        [InlineData(";", "x = 2;")]
        // Overwrite missing in else path.
        [InlineData("x = 2;", "")]
        // Overwrite missing in nested else path.
        [InlineData("if(flag) { x = 1; }",
                    "x = 2;")]
        // Overwrite missing in multiple nested paths.
        [InlineData("if(flag) { x = 1; }",
                    "if(flag) { x = 2; }")]
        // Overwrite missing with nested if-elseif-else.
        [InlineData("if(flag) { x = 1; } else if(flag2) { x = 2; }",
                    "if(flag) { x = 3; } else { x = 4; }")]
        // Overwrite missing in one path with multi-level nesting.
        [InlineData(@"if(flag) { x = 1; } else { if(flag2) { if(flag3) { x = 2; } } else { x = 3; } }",
                    @"x = 4;")]
        public async Task IfElse_OverwrittenInSomeControlFlowPaths(string ifBranchCode, string elseBranchCode)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag, bool flag2, bool flag3)
    {{
        int [|x|] = 1;
        if (flag4)
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // Overitten in condition when true, overwritten in else code block when false.
        [InlineData("flag && M2(out x)", ";", "x = 2;")]
        // Overitten in condition when false, overwritten in if code block when true.
        [InlineData("flag || M2(out x)", "x = 2;", ";")]
        public async Task IfElse_Overwritten_CodeInOneBranch_ConditionInOtherBranch(string condition, string ifBranchCode, string elseBranchCode)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}",
$@"class C
{{
    int M(bool flag)
    {{
        int x;
        if ({condition})
        {{
            {ifBranchCode}
        }}
        else
        {{
            {elseBranchCode}
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // Overwrite missing in condition when left of || is true.
        [InlineData("flag || M2(out x)")]
        // Overwrite missing in condition when left of && is true.
        [InlineData("flag && M2(out x)")]
        // Overwrite missing in condition when left of || is true, but both both sides of && have an overwrite.
        [InlineData("flag || M2(out x) && (x = M3()) > 0")]
        public async Task IfElse_MayBeOverwrittenInCondition_LogicalOperators(string condition)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("M2(out x) || flag")]
        [InlineData("M2(out x) && flag")]
        [InlineData("M2(out x) || M2(out x)")]
        [InlineData("M2(out x) && M2(out x)")]
        [InlineData("flag && M2(out x) || (x = M3()) > 0")]
        [InlineData("(flag || M2(out x)) && (x = M3()) > 0")]
        [InlineData("M2(out x) && flag || (x = M3()) > 0")]
        [InlineData("flag && M2(out x) || (x = M3()) > 0 && flag")]
        public async Task IfElse_OverwrittenInCondition_LogicalOperators(string condition)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(bool flag)
    {{
        int [|x|] = 1;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}",
        $@"class C
{{
    int M(bool flag)
    {{
        int x;
        if ({condition})
        {{
        }}
        else
        {{
        }}

        return x;
    }}

    bool M2(out int x) {{ x = 0; return true; }}
    int M3() => 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ElselessIf(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(bool flag)
    {
        int [|x|] = 1;
        if (flag)
        {
            x = 1;
        }

        return x;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UnusedDefinition_NotFlagged_InUnreachableBlock(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int x;
        if (true)
        {
            x = 0;
        }
        else
        {
            [|x|] = 1;
        }

        return x;
    }
}

    bool M2(out int x) { x = 0; return true; }
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task SwitchCase_UnusedValueWithOnlyWrite_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                _ = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task SwitchCase_UnusedValueWithOnlyWrite_PreferUnusedLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                return 0;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task SwitchCase_UnusedConstantValue_WithReadsAndWrites(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = 0;
                x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int x;
                x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard), "_")]
        [InlineData(nameof(PreferUnusedLocal), "int unused")]
        public async Task SwitchCase_UnusedNonConstantValue_WithReadsAndWrites(string optionName, string fix)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(int flag)
    {
        switch(flag)
        {
            case 0:
                int [|x|] = M2();
                x = 1;
                return x;

            default:
                return flag;
        }
    }

    int M2() => 0;
}",
$@"class C
{{
    int M(int flag)
    {{
        switch(flag)
        {{
            case 0:
                int x;
                {fix} = M2();
                x = 1;
                return x;

            default:
                return flag;
        }}
    }}

    int M2() => 0;
}}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // For loop, assignment in body, read on back edge.
        [InlineData("for(i = 1; i < 10; i--)",
                        "M2(x); [|x|] = 1;")]
        // While loop, assignment in body, read on back edge.
        [InlineData("while(i++ < 10)",
                        "M2(x); [|x|] = 1;")]
        // Do loop, assignment in body, read on back edge.
        [InlineData("do",
                        "M2(x); [|x|] = 1;",
                    "while(i++ < 10);")]
        // Continue, read on back edge.
        [InlineData("while(i++ < 10)",
                        "M2(x); [|x|] = 1; if (flag) continue; x = 2;")]
        // Break.
        [InlineData(@"x = 0;
                      while(i++ < 10)",
                         "[|x|] = 1; if (flag) break; x = 2;")]
        // Assignment before loop, no overwrite on path where loop is never entered.
        [InlineData(@"[|x|] = 1;
                      while(i++ < 10)",
                         "x = 2;")]
        public async Task Loops_Overwritten_InSomeControlFlowPaths(
            string loopHeader, string loopBody, string loopFooter = null)
        {
            await TestMissingInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {loopHeader}
        {{
            {loopBody}
        }}
        {loopFooter ?? string.Empty}

        return x;
    }}

    void M2(int x) {{ }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        // For loop, assignment in body, re-assigned on back edge before read in loop and re-assigned at loop exit.
        [InlineData("for(i = 1; i < 10; i--)",
                        "x = 1; M2(x); [|x|] = 2;",
                    "x = 3;",
                    // Fixed code.
                    "for(i = 1; i < 10; i--)",
                        "x = 1; M2(x);",
                    "x = 3;")]
        // While loop, assignment in body, re-assigned on condition before read in loop and re-assigned at loop exit.
        [InlineData("while(i++ < (x = 10))",
                        "M2(x); [|x|] = 2;",
                    "x = 3;",
                    // Fixed code.
                    "while(i++ < (x = 10))",
                        "M2(x);",
                    "x = 3;")]
        // Assigned before loop, Re-assigned in continue, break paths and loop exit.
        [InlineData(@"[|x|] = 1;
                      i = 1;
                      while(i++ < 10)",
                        @"if(flag)
                            { x = 2; continue; }
                          else if(i < 5)
                            { break; }
                          else
                            { x = 3; }
                          M2(x);",
                      "x = 4;",
                    // Fixed code.
                    @"i = 1;
                      while(i++ < 10)",
                        @"if(flag)
                            { x = 2; continue; }
                          else if(i < 5)
                            { break; }
                          else
                            { x = 3; }
                          M2(x);",
                      "x = 4;")]
        public async Task Loops_Overwritten_InAllControlFlowPaths(
            string loopHeader, string loopBody, string loopFooter,
            string fixedLoopHeader, string fixedLoopBody, string fixedLoopFooter)
        {
            await TestInRegularAndScriptWithAllOptionsAsync(
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {loopHeader}
        {{
            {loopBody}
        }}
        {loopFooter}

        return x;
    }}

    void M2(int x) {{ }}
}}",
$@"class C
{{
    int M(int i, int x, bool flag)
    {{
        {fixedLoopHeader}
        {{
            {fixedLoopBody}
        }}
        {fixedLoopFooter}

        return x;
    }}

    void M2(int x) {{ }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task FixAll_NonConstantValue_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = M2();
    }

    int M(bool flag)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = M2()|};

        // Unused out assignment
        M2(out x);

        // Used Assignment
        x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = M2();
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
        _ = M2();
    }

    int M(bool flag)
    {
        // Trigger diagostic
        _ = M2();

        // Unused out assignment
        M2(out _);

        // Used Assignment
        int x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        _ = M2();
        if (flag)
        {
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task FixAll_NonConstantValue_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = M2();
        x = 0;
        System.Console.WriteLine(x);
    }

    int M(bool flag)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = M2()|};

        // Unused out assignment
        M2(out x);

        // Used Assignment, declaration for 'x' should move here
        x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = M2();
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
        int unused = M2();
        int x = 0;
        System.Console.WriteLine(x);
    }

    int M(bool flag)
    {
        // Trigger diagostic
        int unused = M2();
        int unused1;

        // Unused out assignment
        M2(out unused1);

        // Used Assignment, declaration for 'x' should move here
        int x = 0;
        System.Console.WriteLine(x);

        // Unused constant assignment.
        // Not fixed as we have a different code fix 'Remove redundant assignment'
        x = 1;

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int unused2 = M2();
        if (flag)
        {
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task FixAll_ConstantValue_RemoveRedundantAssignments(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        // Different code block
        int x = 1;
    }

    int M(bool flag, int p)
    {
        // Trigger diagostic
        {|FixAllInDocument:int x = 0|};

        // Unused assignment from parameter, should be removed.
        x = p;

        // Unused assignment from local, should be removed.
        int local = 3;
        x = local;

        // Used assignment, declaration for 'x' should move here
        x = 0;
        System.Console.WriteLine(x);

        // Unused non-constant 'out' assignment
        // Not fixed as we have a different code fix 'Use discard' for it.
        M2(out x);

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        int y = 1;
        if (flag)
        {
            y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}",
@"class C
{
    public C()
    {
        // Different code block
    }

    int M(bool flag, int p)
    {
        // Trigger diagostic

        // Unused assignment from parameter, should be removed.

        // Unused assignment from local, should be removed.
        int local = 3;

        // Used assignment, declaration for 'x' should move here
        int x = 0;
        System.Console.WriteLine(x);

        // Unused non-constant 'out' assignment
        // Not fixed as we have a different code fix 'Use discard' for it.
        M2(out x);

        // Unused initialization with only def/use in nested block.
        // Declaration for 'y' should be moved inside the if block.
        if (flag)
        {
            int y = 2;
            System.Console.WriteLine(y);
        }
        else
        {
        }

        x = M2();
        return x;
    }

    bool M2(out int x) { x = 0; return true; }
    int M2() => 0;
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task FixAll_MoveMultipleVariableDeclarations_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag, int p)
    {
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        {|FixAllInDocument:int x = 0|};
        int z1 = 1, _ = M2(), y = 0, z2 = 2;

        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}",
@"class C
{
    int M(bool flag, int p)
    {
        int z1 = 1;
        _ = M2();
        int x;
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        int y;
        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task FixAll_MoveMultipleVariableDeclarations_PreferUnusedLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M(bool flag, int p)
    {
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        {|FixAllInDocument:int x = 0|};
        int z1 = 1, _ = M2(), y = 0, z2 = 2;

        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}",
@"class C
{
    int M(bool flag, int p)
    {
        int z1 = 1, _ = M2();
        int x;
        // Multiple unused variable declarations (x and y) moved below to start of if-else block
        // Used declaration (z1) and evaluation (_ = M2()) retained.
        // Completely unused declaration (z2) removed.
        int y;
        if (flag)
        {
            x = 1;
            y = 1;
        }
        else
        {
            x = 2;
            y = 2;
        }

        return x + y + z1;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NonConstantValue_Trivia_PreferDiscard_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = M2()|], y = M2();   // C2
        // C3

        return y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        _ = M2();
        // C1
        int y = M2();   // C2
        // C3

        return y;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NonConstantValue_Trivia_PreferDiscard_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/M2()|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        return y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*//*C3*/ /*C4*/
        _/*C5*/ = /*C6*/M2()/*C7*/;
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        return y;
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NonConstantValue_Trivia_PreferUnusedLocal_01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = M2()|], y = M2();   // C2
        // C3

        // C4
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        int unused = M2(), y = M2();   // C2
        // C3

        // C4
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task NonConstantValue_Trivia_PreferUnusedLocal_02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/M2()|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/unused/*C5*/ = /*C6*/M2()/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", options: PreferUnusedLocal);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ConstantValue_Trivia_01(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        // C1
        [|int x = 0|], y = M2();   // C2
        // C3

        // C4
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        // C1
        int y = M2();   // C2
        // C3

        // C4
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ConstantValue_Trivia_02(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/[|int/*C3*/ /*C4*/x/*C5*/ = /*C6*/0|]/*C7*/, y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        x = 1;
        return x + y;
    }

    int M2() => 0;
}",
@"class C
{
    int M()
    {
        /*C1*/
        /*C2*/
        int/*C3*/ /*C4*/y/*C8*/ = M2()/*C9*/;   // C10
        /*C11*/

        // C12
        int x = 1;
        return x + y;
    }

    int M2() => 0;
}", optionName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExistingDiscardDeclarationInLambda_UseOutsideLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [|x|] = M2();
        Action a = () =>
        {
            var _ = M2();
        };

        a();
    }

    int M2() => 0;
}",
@"using System;

class C
{
    void M()
    {
        _ = M2();
        Action a = () =>
        {
            _ = M2();
        };

        a();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task ExistingDiscardDeclarationInLambda_UseInsideLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            int [|x|] = M2();
            var _ = M2();
        };

        a();
    }

    int M2() => 0;
}",
@"using System;

class C
{
    void M()
    {
        Action a = () =>
        {
            _ = M2();
            _ = M2();
        };

        a();
    }

    int M2() => 0;
}", options: PreferDiscard);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task ValueOverwrittenByOutVar_ConditionalAndExpression(string optionName)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int {|FixAllInDocument:x1|} = -1, x2 = -1;
        if (M2(x: out x1) &&
            M2(x: out x2))
        {
            x1 = 0;
            x2 = 0;
        }
        else
        {
            Console.WriteLine(x1);
        }

        Console.WriteLine(x1 + x2);
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}",
@"using System;

class C
{
    void M()
    {
        int x2 = -1;
        int x1;
        if (M2(x: out x1) &&
            M2(x: out x2))
        {
            x1 = 0;
            x2 = 0;
        }
        else
        {
            Console.WriteLine(x1);
        }

        Console.WriteLine(x1 + x2);
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}", optionName);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData("var")]
        [InlineData("int")]
        public async Task UnusedOutVariableDeclaration_PreferDiscard(string typeName)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        if (M2(out {typeName} [|x|]))
        {{
        }}
    }}

    bool M2(out int x)
    {{
        x = 0;
        return true;
    }}
}}",
@"class C
{
    void M()
    {
        if (M2(out _))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }
}", options: PreferDiscard);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        public async Task UnusedOutVariableDeclaration_MethodOverloads_PreferDiscard()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (M2(out int [|x|]))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }

    bool M2(out char x)
    {
        x = 'c';
        return true;
    }
}",
@"class C
{
    void M()
    {
        if (M2(out int _))
        {
        }
    }

    bool M2(out int x)
    {
        x = 0;
        return true;
    }

    bool M2(out char x)
    {
        x = 'c';
        return true;
    }
}", options: PreferDiscard);
        }

        [WorkItem(31583, "https://github.com/dotnet/roslyn/issues/31583")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task MissingImports(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        List<int> [|x|] = null;
    }
}", optionName);
        }

        [WorkItem(31583, "https://github.com/dotnet/roslyn/issues/31583")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [InlineData(nameof(PreferDiscard))]
        [InlineData(nameof(PreferUnusedLocal))]
        public async Task UsedAssignment_ConditionalPreprocessorDirective(string optionName)
        {
            await TestMissingInRegularAndScriptAsync(
@"#define DEBUG

class C
{
    int M()
    {
        int [|x|] = 0;
#if DEBUG
        x = 1;
#endif
        return x;
    }
}", optionName);
        }
    }
}
