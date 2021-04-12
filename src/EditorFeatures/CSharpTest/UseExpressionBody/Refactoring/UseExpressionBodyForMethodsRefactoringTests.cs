﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForMethodsRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExpressionBodyCodeRefactoringProvider();

        private OptionsCollection UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private OptionsCollection UseExpressionBodyDisabledDiagnostic =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.None));

        private OptionsCollection UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        private OptionsCollection UseBlockBodyDisabledDiagnostic =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.None));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
@"class C
{
    void Goo()
    {
        [||]Bar();
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo()
    {
        [||]Bar();
    }
}",
@"class C
{
    void Goo() => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyDisabledDiagnostic));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo()
    {
        [||]Bar();
    }
}",
@"class C
{
    void Goo() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedInLambda()
        {
            await TestMissingAsync(
@"class C
{
    Action Goo()
    {
        return () => { [||] };
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"class C
{
    void Goo() => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo() => [||]Bar();
}",
@"class C
{
    void Goo()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo() => [||]Bar();
}",
@"class C
{
    void Goo()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }

        [WorkItem(25501, "https://github.com/dotnet/roslyn/issues/25501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedAtStartOfMethod()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    [||]void Goo()
    {
        Bar();
    }
}",
@"class C
{
    void Goo() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(25501, "https://github.com/dotnet/roslyn/issues/25501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedBeforeMethodOnSameLine()
        {
            await TestInRegularAndScript1Async(
@"class C
{
[||]    void Goo()
    {
        Bar();
    }
}",
@"class C
{
    void Goo() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(25501, "https://github.com/dotnet/roslyn/issues/25501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedBeforeAttributes()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    [||][A]
    void Goo()
    {
        Bar();
    }
}",
@"class C
{
    [A]
    void Goo() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(25501, "https://github.com/dotnet/roslyn/issues/25501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedBeforeComments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [||]/// <summary/>
    void Goo()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [WorkItem(25501, "https://github.com/dotnet/roslyn/issues/25501")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedInComments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    /// [||]<summary/>
    void Goo()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }
    }
}
