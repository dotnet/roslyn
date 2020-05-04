// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateComparisonOperators;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateComparisonOperators
{
    public class GenerateComparisonOperatorsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateComparisonOperatorsCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

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
            await TestInRegularAndScriptAsync(
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

    public static bool operator <(C left, C right) => left.CompareTo(right) < 0;
    public static bool operator >(C left, C right) => left.CompareTo(right) > 0;
    public static bool operator <=(C left, C right) => left.CompareTo(right) <= 0;
    public static bool operator >=(C left, C right) => left.CompareTo(right) >= 0;
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));
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
            await TestMissingAsync(
@"
using System;

class C : IComparable<C>
{
[||]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMissingWithUnknownType()
        {
            await TestMissingAsync(
@"
using System;

class C : IComparable<Goo>
{
    public int CompareTo(Goo g) => 0;

[||]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateComparisonOperators)]
        public async Task TestMissingWithAllExistingOperators()
        {
            await TestMissingAsync(
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
}");
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

    public static bool operator <(C left, C right)
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
    int IComparable<C>.CompareTo(C c) => 0;

[||]
}",
@"
using System;

interface C : IComparable<C>
{
    int IComparable<C>.CompareTo(C c) => 0;

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
    }
}
