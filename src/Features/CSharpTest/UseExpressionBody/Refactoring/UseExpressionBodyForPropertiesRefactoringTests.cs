// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyForPropertiesRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
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
        public async Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
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
            [||]return Bar();
        }
    }
}",
@"class C
{
    int Goo => Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic));
        }

        [Fact]
        public async Task TestUpdateAccessorIfAccessWantsBlockAndPropertyWantsExpression()
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
    int Goo
    {
        get => Bar();
    }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
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
        public async Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody2()
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
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact]
        public async Task TestNotOfferedInLambda()
        {
            await TestMissingAsync(
@"class C
{
    Action Goo
    {
        get 
        {
            return () => { [||] };
        }
    }
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        {
            await TestMissingAsync(
@"class C
{
    int Goo => [||]Bar();
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));
        }

        [Fact]
        public async Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody2()
        {
            await TestMissingAsync(
@"class C
{
    int Goo => [||]Bar();
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo => [||]Bar();
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
}", parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));
        }

        [Fact]
        public async Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo => [||]Bar();
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
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20363")]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo => [||]Bar();
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

        [Fact]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int Goo => [||]Bar();
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
}", parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20360")]
        public async Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody_CSharp6()
        {
            await TestAsync(
@"class C
{
    int Goo => [||]Bar();
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
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6),
    options: UseExpressionBodyForAccessors_ExpressionBodyForProperties);
        }
    }
}
