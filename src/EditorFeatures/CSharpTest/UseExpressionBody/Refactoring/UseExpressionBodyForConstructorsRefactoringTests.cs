﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForConstructorsRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExpressionBodyCodeRefactoringProvider();

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithNoneEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
@"class C
{
    public C()
    {
        [||]Bar();
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    public C()
    {
        [||]Bar();
    }
}",
@"class C
{
    public C() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedInLambda()
        {
            await TestMissingAsync(
@"class C
{
    public C()
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
    public C() => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    public C() => [||]Bar();
}",
@"class C
{
    public C()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseExpressionBody));
        }
    }
}
