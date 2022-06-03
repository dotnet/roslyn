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
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForLocalFunctionsRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExpressionBodyCodeRefactoringProvider();

        private OptionsCollection UseExpressionBody =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private OptionsCollection UseExpressionBodyDisabledDiagnostic =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.None));

        private OptionsCollection UseBlockBody =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        private OptionsCollection UseBlockBodyDisabledDiagnostic =>
            Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.None));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
@"class C
{
    void Goo()
    {
        void Bar() 
        {
            [||]Test();
        }
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
        void Bar() 
        {
            [||]Test();
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() => Test();
    }
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
        void Bar() 
        {
            [||]Test();
        }
    }
}",
@"class C
{
    void Goo()
    {
        void Bar() => Test();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"class C
{
    void Goo()
    {
        void Bar() => [||]Test();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo()
    {
        void Bar() => [||]Test();
    }
}",
@"class C
{
    void Goo()
    {
        void Bar()
        {
            Test();
        }
    }
}", parameters: new TestParameters(options: UseBlockBodyDisabledDiagnostic));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Goo()
    {
        void Bar() => [||]Test();
    }
}",
@"class C
{
    void Goo()
    {
        void Bar()
        {
            Test();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }
    }
}
