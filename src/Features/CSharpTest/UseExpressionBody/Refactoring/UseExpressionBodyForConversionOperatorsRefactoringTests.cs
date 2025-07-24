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
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForConversionOperatorsRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
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
                public static implicit operator bool(C c1)
                {
                    [||]Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedInLambda()
        => TestMissingAsync(
            """
            class C
            {
                public static implicit operator bool(C c1)
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
                public static implicit operator bool(C c1) => [||]Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1) => [||]Bar();
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    return Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1) => [||]Bar();
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    return Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task TestOfferedWithSelectionInsideBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    [|Bar()|];
                }
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1) => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedWithSelectionOutsideBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                public static implicit operator bool(C c1)
                {
                    [|Bar();
                }
            }|]
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38057")]
    public Task TestCommentAfterConstructorName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static implicit operator bool(C c1) // comment
                {
                    [||]return Bar();
                }
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1) => Bar(); // comment
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));
}
