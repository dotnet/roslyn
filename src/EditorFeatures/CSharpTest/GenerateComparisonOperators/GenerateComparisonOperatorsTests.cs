// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.GenerateComparisonOperators;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateComparisonOperators
{
    using static Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.AbstractCodeActionOrUserDiagnosticTest;
    using VerifyCS = CSharpCodeRefactoringVerifier<GenerateComparisonOperatorsCodeRefactoringProvider>;

    [UseExportProvider]
    public class GenerateComparisonOperatorsTests
    {
        private static Task TestInRegularAndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            TestParameters? parameters = null,
            List<DiagnosticResult> fixedExpectedDiagnostics = null)
        {
            return TestInRegularAndScript1Async(
                new[] { initialMarkup }, new[] { expectedMarkup }, index, parameters, fixedExpectedDiagnostics);
        }

        private static async Task TestInRegularAndScript1Async(
            string[] initialMarkup,
            string[] expectedMarkup,
            int index = 0,
            TestParameters? parameters = null,
            List<DiagnosticResult> fixedExpectedDiagnostics = null)
        {
            var test = new VerifyCS.Test
            {
                CodeActionIndex = index,
            };

            foreach (var source in initialMarkup)
                test.TestState.Sources.Add(source);

            foreach (var source in expectedMarkup)
                test.FixedState.Sources.Add(source);

            if (parameters?.parseOptions != null)
                test.LanguageVersion = ((CSharpParseOptions)parameters.Value.parseOptions).LanguageVersion;

            if (parameters?.options != null)
                test.EditorConfig = CodeFixVerifierHelper.GetEditorConfigText(parameters.Value.options);

            foreach (var diagnostic in fixedExpectedDiagnostics ?? new List<DiagnosticResult>())
                test.FixedState.ExpectedDiagnostics.Add(diagnostic);

            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestClass()
        {
            await TestInRegularAndScript1Async(
@"
using System;

[||]class C : IComparable<C>
{
    public int CompareTo(C c) => 0;
}",
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestPreferExpressionBodies()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
using System;

[||]class C : IComparable<C>
{
    public int CompareTo(C c) => 0;
}",
                FixedCode =
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right) => left.CompareTo(right) < 0;
    public static bool operator >(C left, C right) => left.CompareTo(right) > 0;
    public static bool operator <=(C left, C right) => left.CompareTo(right) <= 0;
    public static bool operator >=(C left, C right) => left.CompareTo(right) >= 0;
}",
                EditorConfig = CodeFixVerifierHelper.GetEditorConfigText(
                    new OptionsCollection(LanguageNames.CSharp)
                    {
                        { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
                    }),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestExplicitImpl()
        {
            await TestInRegularAndScript1Async(
@"
using System;

[||]class C : IComparable<C>
{
    int IComparable<C>.CompareTo(C c) => 0;
}",
@"
using System;

class C : IComparable<C>
{
    int IComparable<C>.CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return ((IComparable<C>)left).CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return ((IComparable<C>)left).CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return ((IComparable<C>)left).CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return ((IComparable<C>)left).CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestOnInterface()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C : [||]IComparable<C>
{
    public int CompareTo(C c) => 0;
}",
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestAtEndOfInterface()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C : IComparable<C>[||]
{
    public int CompareTo(C c) => 0;
}",
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestInBody()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

[||]
}",
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMissingWithoutCompareMethod()
        {
            var code = @"
using System;

class C : {|CS0535:IComparable<C>|}
{
[||]
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMissingWithUnknownType()
        {
            var code = @"
using System;

class C : IComparable<{|CS0246:Goo|}>
{
    public int CompareTo({|CS0246:Goo|} g) => 0;

[||]
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMissingWithAllExistingOperators()
        {
            var code =
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }

[||]
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestWithExistingOperator()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator {|CS0216:<|}(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

[||]
}",
@"
using System;

class C : IComparable<C>
{
    public int CompareTo(C c) => 0;

    public static bool operator <(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMultipleInterfaces()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C : IComparable<C>, IComparable<int>
{
    public int CompareTo(C c) => 0;
    public int CompareTo(int c) => 0;

[||]
}",
@"
using System;

class C : IComparable<C>, IComparable<int>
{
    public int CompareTo(C c) => 0;
    public int CompareTo(int c) => 0;

    public static bool operator <(C left, int right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(C left, int right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(C left, int right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(C left, int right)
    {
        return left.CompareTo(right) >= 0;
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestInInterfaceWithDefaultImpl()
        {
            await TestInRegularAndScript1Async(
@"
using System;

interface C : IComparable<C>
{
    int IComparable<C>.{|CS8701:CompareTo|}(C c) => 0;

[||]
}",
@"
using System;

interface C : IComparable<C>
{
    int IComparable<C>.{|CS8701:CompareTo|}(C c) => 0;

    public static bool operator {|CS8701:<|}(C left, C right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator {|CS8701:>|}(C left, C right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator {|CS8701:<=|}(C left, C right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator {|CS8701:>=|}(C left, C right)
    {
        return left.CompareTo(right) >= 0;
    }
}");
        }
    }
}
