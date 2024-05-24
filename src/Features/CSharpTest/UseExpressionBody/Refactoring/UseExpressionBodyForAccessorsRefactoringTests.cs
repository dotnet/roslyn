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
public class UseExpressionBodyForAccessorsRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExpressionBodyCodeRefactoringProvider();

    private OptionsCollection UseExpressionBodyForAccessors_BlockBodyForProperties
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseExpressionBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.None },
        };

    private OptionsCollection UseExpressionBodyForAccessors_ExpressionBodyForProperties
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private OptionsCollection UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
        };

    private OptionsCollection UseBlockBodyForAccessors_ExpressionBodyForProperties
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private OptionsCollection UseBlockBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
        };

    private OptionsCollection UseBlockBodyForAccessors_BlockBodyForProperties
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseBlockBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic
        => new OptionsCollection(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.None },
        };

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
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
    int Goo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody2()
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
    int Goo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic));
    }

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
    {
        await TestMissingAsync(
@"class C
{
    int Goo { get => [||]Bar(); }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int Goo { get => [||]Bar(); }
}",

@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));
    }

    [Fact]
    public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody2()
    {
        await TestInRegularAndScript1Async(
@"class C
{
    int Goo { get => [||]Bar(); }
}",

@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic));
    }

    [Fact]
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

    [Fact]
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
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
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));
    }
}
