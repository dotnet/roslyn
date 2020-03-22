// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
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
    }
}
