// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForOperatorsRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExpressionBodyCodeRefactoringProvider();

    private OptionsCollection UseExpressionBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

    private OptionsCollection UseExpressionBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.None));

    private OptionsCollection UseBlockBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

    private OptionsCollection UseBlockBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.None));

    [Fact]
    public Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    [||]Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                public static bool operator +(C c1, C c2) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                public static bool operator +(C c1, C c2) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedInLambda()
        => TestMissingAsync(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    return () => { [||] };
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        => TestMissingAsync(
            """
            class C
            {
                public static bool operator +(C c1, C c2) => [||]Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public static bool operator +(C c1, C c2) => [||]Bar();
            }
            """,
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    return Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public static bool operator +(C c1, C c2) => [||]Bar();
            }
            """,
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    return Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task TestOfferedWithSelectionInsideBlockBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    [|Bar()|];
                }
            }
            """,
            """
            class C
            {
                public static bool operator +(C c1, C c2) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedWithSelectionOutsideBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                public static bool operator +(C c1, C c2)
                {
                    [|Bar();
                }
            }|]
            """,
            parameters: new TestParameters(options: UseBlockBody));
}
