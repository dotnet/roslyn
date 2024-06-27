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
public class UseExpressionBodyForIndexersRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExpressionBodyCodeRefactoringProvider();

    private OptionsCollection UseExpressionBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

    private OptionsCollection UseExpressionBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.None));

    private OptionsCollection UseBlockBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

    private OptionsCollection UseBlockBodyDisabledDiagnostic
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.None));

    [Fact]
    public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
    {
        await TestMissingAsync(
@"class C
{
    int this[int i]
    {
        get 
        {
            [||]return Bar();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBody));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int this[int i]
    {
        get
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int this[int i] => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int this[int i]
    {
        get
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int this[int i] => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
    }

    [Fact]
    public async Task TestNotOfferedInLambda()
    {
        await TestMissingAsync(
@"class C
{
    Action Goo[int i]
    {
        get 
        {
            return () => { [||] };
        }
    }
}", parameters: new TestParameters(options: UseBlockBody));
    }

    [Fact]
    public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
    {
        await TestMissingAsync(
@"class C
{
    int this[int i] => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBody));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int this[int i] => [||]Bar();
}",
@"class C
{
    int this[int i]
    {
        get
        {
            return Bar();
        }
    }
}", parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20363")]
    public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int this[int i] => [||]Bar();
}",
@"class C
{
    int this[int i]
    {
        get
        {
            return Bar();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBody));
    }
}
