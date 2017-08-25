// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForAccessorsRefactoringTests : AbstractCSharpCodeActionTest
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
        public async Task TestUpdatePropertyIfPropertyWantsBlockAndAccessorWantsExpression()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo
    {
        get
        {
            [||]return Bar();
        }
    }
}",
@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody2()
        {
            await TestMissingAsync(
@"class C
{
    int Goo
    {
        get
        {
            [||]return Bar();
        }
    }
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo
    {
        get
        {
            return [||]Bar();
        }
    }
}",
@"class C
{
    int Goo
    {
        get => Bar();
    }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferExpressionBodyForPropertyIfPropertyAndAccessorBothPreferExpressions()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo
    {
        get
        {
            return [||]Bar();
        }
    }
}",
@"class C
{
    int Goo => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"class C
{
    int Goo { get => [||]Bar(); }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferedForPropertyIfUserPrefersBlockPropertiesAndHasBlockProperty()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo { get => [||]Bar(); }
}",

@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestOfferForPropertyIfPropertyPrefersBlockButCouldBecomeExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo { get => [||]Bar(); }
}",
@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));
        }

        [WorkItem(20350, "https://github.com/dotnet/roslyn/issues/20350")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestAccessorListFormatting()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo { get => [||]Bar(); }
}",
@"class C
{
    int Goo
    {
        get
        {
            return Bar();
        }
    }
}", ignoreTrivia: false, parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));
        }
    }
}
