﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForPropertiesRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new UseExpressionBodyCodeRefactoringProvider();

        private IDictionary<OptionKey, object> UseExpressionBodyForAccessors_BlockBodyForProperties =>
            OptionsSet(
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement),
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseExpressionBodyForAccessors_ExpressionBodyForProperties =>
            OptionsSet(
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement),
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseBlockBodyForAccessors_ExpressionBodyForProperties =>
            OptionsSet(
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseBlockBodyForAccessors_BlockBodyForProperties =>
            OptionsSet(
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                this.SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        {
            await TestMissingAsync(
@"class C
{
    int Foo
    {
        get 
        {
            [||]return Bar();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestUpdateAccessorIfAccessWantsBlockAndPropertyWantsExpression()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get 
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int Foo
    {
        get => Bar();
    }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int Foo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo
    {
        get
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int Foo => Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedInLambda()
        {
            await TestMissingAsync(
@"class C
{
    Action Foo
    {
        get 
        {
            return () => { [||] };
        }
    }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"class C
{
    int Foo => [||]Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody2()
        {
            await TestMissingAsync(
@"class C
{
    int Foo => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [WorkItem(20363, "https://github.com/dotnet/roslyn/issues/20363")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo => [||]Bar();
}",
@"class C
{
    int Foo { get { return Bar(); } }
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Foo => [||]Bar();
}",
@"class C
{
    int Foo { get { return Bar(); } }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        [WorkItem(20360, "https://github.com/dotnet/roslyn/issues/20360")]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody_CSharp6()
        {
            await TestAsync(
@"class C
{
    int Foo => [||]Bar();
}",
@"class C
{
    int Foo { get { return Bar(); } }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6),
    options: UseExpressionBodyForAccessors_ExpressionBodyForProperties);
        }
    }
}
