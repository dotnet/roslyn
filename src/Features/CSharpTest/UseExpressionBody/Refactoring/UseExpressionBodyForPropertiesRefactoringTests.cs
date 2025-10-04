// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForPropertiesRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExpressionBodyCodeRefactoringProvider();

    private OptionsCollection UseExpressionBodyForAccessors_BlockBodyForProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseExpressionBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.None },
        };

    private OptionsCollection UseExpressionBodyForAccessors_ExpressionBodyForProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private OptionsCollection UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible, NotificationOption2.None },
        };

    private OptionsCollection UseBlockBodyForAccessors_ExpressionBodyForProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private OptionsCollection UseBlockBodyForAccessors_BlockBodyForProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseBlockBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never, NotificationOption2.None },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never, NotificationOption2.None },
        };

    [Fact]
    public Task TestNotOfferedIfUserPrefersExpressionBodiesAndInBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                int Goo
                {
                    get 
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesWithoutDiagnosticAndInBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo
                {
                    get 
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic));

    [Fact]
    public Task TestUpdateAccessorIfAccessWantsBlockAndPropertyWantsExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo
                {
                    get 
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get => Bar();
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesAndInBlockBody2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo => Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestNotOfferedInLambda()
        => TestMissingAsync(
            """
            class C
            {
                Action Goo
                {
                    get 
                    {
                        return () => { [||] };
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody()
        => TestMissingAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestNotOfferedIfUserPrefersBlockBodiesAndInExpressionBody2()
        => TestMissingAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));

    [Fact]
    public Task TestOfferedIfUserPrefersBlockBodiesWithoutDiagnosticAndInExpressionBody2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_BlockBodyForProperties_DisabledDiagnostic));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20363")]
    public Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));

    [Fact]
    public Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """,
            parameters: new TestParameters(options: UseBlockBodyForAccessors_ExpressionBodyForProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20360")]
    public Task TestOfferedIfUserPrefersExpressionBodiesAndInExpressionBody_CSharp6()
        => TestAsync(
            """
            class C
            {
                int Goo => [||]Bar();
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """,
            new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6), options: UseExpressionBodyForAccessors_ExpressionBodyForProperties));

    [Fact]
    public Task TestOfferedWithSelectionInsideBlockBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        [|return Bar()|];
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo => Bar();
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));

    [Fact]
    public Task TestNotOfferedWithSelectionOutsideBlockBody()
        => TestMissingAsync(
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        [|return Bar();
                    }
                }
            }|]
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_BlockBodyForProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38057")]
    public Task TestCommentAfterPropertyName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo // comment
                {
                    get 
                    {
                        [||]return Bar();
                    }
                }
            }
            """,
            """
            class C
            {
                int Goo => Bar(); // comment
            }
            """,
            parameters: new TestParameters(options: UseExpressionBodyForAccessors_ExpressionBodyForProperties_DisabledDiagnostic));
}
