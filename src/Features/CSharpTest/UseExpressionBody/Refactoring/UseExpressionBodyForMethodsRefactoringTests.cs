﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
public sealed class UseExpressionBodyForMethodsRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExpressionBodyCodeRefactoringProvider();

    private OptionsCollection UseExpressionBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

    private OptionsCollection UseExpressionBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.None));

    private OptionsCollection UseBlockBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

    private OptionsCollection UseBlockBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.None));

    [Fact]
    public Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                void Goo()
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
                void Goo()
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                void Goo()
                {
                    [||]Bar();
                }
            }
            """,
            """
            class C
            {
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedInLambda()
        => TestMissingAsync(
            """
            class C
            {
                Action Goo()
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
                void Goo() => [||]Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                void Goo() => [||]Bar();
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    Bar();
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
                void Goo() => [||]Bar();
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25501")]
    public Task TestOfferedAtStartOfMethod()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [||]void Goo()
                {
                    Bar();
                }
            }
            """,
            """
            class C
            {
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25501")]
    public Task TestOfferedBeforeMethodOnSameLine()
        => TestInRegularAndScript1Async(
            """
            class C
            {
            [||]    void Goo()
                {
                    Bar();
                }
            }
            """,
            """
            class C
            {
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25501")]
    public Task TestOfferedBeforeAttributes()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [||][A]
                void Goo()
                {
                    Bar();
                }
            }
            """,
            """
            class C
            {
                [A]
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25501")]
    public Task TestNotOfferedBeforeComments()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [||]/// <summary/>
                void Goo()
                {
                    Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25501")]
    public Task TestNotOfferedInComments()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                /// [||]<summary/>
                void Goo()
                {
                    Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53532")]
    public Task TestTriviaOnArrow1()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                    // Test
                     [||]=> Console.WriteLine();
            }
            """,
            """
            class C
            {
                void M()
                {
                    // Test
                    Console.WriteLine();
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
                void Goo()
                {
                    [|Bar()|];
                }
            }
            """,
            """
            class C
            {
                void Goo() => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestNotOfferedWithSelectionOutsideBlockBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    [|Bar();
                }
            }|]
            """,
            parameters: new TestParameters(options: UseBlockBody));

    [Fact]
    public Task TestOfferedWithSelectionInsideExpressionBody()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                void Goo() => [|Bar()|];
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task TestNotOfferedWithSelectionOutsideExpressionBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo() => [|Bar();
            }|]
            """,
            parameters: new TestParameters(options: UseExpressionBody));
}
