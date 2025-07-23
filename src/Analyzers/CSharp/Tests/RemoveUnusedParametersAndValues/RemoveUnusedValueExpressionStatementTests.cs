// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
public sealed partial class RemoveUnusedValueExpressionStatementTests : RemoveUnusedValuesTestsBase
{
    public RemoveUnusedValueExpressionStatementTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    private protected override OptionsCollection PreferNone
        => Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
               new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.None));

    private protected override OptionsCollection PreferDiscard
        => Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
               new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.DiscardVariable, NotificationOption2.Silent));

    private protected override OptionsCollection PreferUnusedLocal
        => Option(CSharpCodeStyleOptions.UnusedValueExpressionStatement,
               new CodeStyleOption2<UnusedValuePreference>(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent));

    [Fact]
    public Task ExpressionStatement_Suppressed()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                int M2() => 0;
            }
            """, options: PreferNone);

    [Fact]
    public Task ExpressionStatement_PreferDiscard_CSharp6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {
                    var unused = M2();
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferDiscard,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Theory]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_VariableInitialization(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int x = [|M2()|];
                }

                int M2() => 0;
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard), "_")]
    [InlineData(nameof(PreferUnusedLocal), "var unused")]
    public Task ExpressionStatement_NonConstantPrimitiveTypeValue(string optionName, string fix)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                int M2() => 0;
            }
            """,
            $$"""
            class C
            {
                void M()
                {
                    {{fix}} = M2();
                }

                int M2() => 0;
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard), "_")]
    [InlineData(nameof(PreferUnusedLocal), "var unused")]
    public Task ExpressionStatement_UserDefinedType(string optionName, string fix)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                C M2() => new C();
            }
            """,
            $$"""
            class C
            {
                void M()
                {
                    {{fix}} = M2();
                }

                C M2() => new C();
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_ConstantValue(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|1|];
                }
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_SyntaxError(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2(,)|];
                }

                int M2() => 0;
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_SemanticError(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33073")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_SemanticError_02(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                UndefinedType M2() => null;
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33073")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_SemanticError_03(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                private async Task M()
                {
                    // error CS0103: The name 'CancellationToken' does not exist in the current context
                    [|await Task.Delay(0, CancellationToken.None).ConfigureAwait(false)|];
                }
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33073")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_SemanticError_04(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                private async Task M()
                {
                    // error CS0103: The name 'Task' does not exist in the current context
                    // error CS0103: The name 'CancellationToken' does not exist in the current context
                    // error CS1983: The return type of an async method must be void, Task or Task<T>
                    [|await Task.Delay(0, CancellationToken.None).ConfigureAwait(false)|];
                }
            }
            """, optionName);

    [Theory]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_VoidReturningMethodCall(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                }

                void M2() { }
            }
            """, optionName);

    [Theory]
    [InlineData("=")]
    [InlineData("+=")]
    public Task ExpressionStatement_AssignmentExpression(string op)
        => TestMissingInRegularAndScriptWithAllOptionsAsync(
            $$"""
            class C
            {
                void M(int x)
                {
                    x {{op}} [|M2()|];
                }

                int M2() => 0;
            }
            """);

    [Theory]
    [InlineData("x++")]
    [InlineData("x--")]
    [InlineData("++x")]
    [InlineData("--x")]
    public Task ExpressionStatement_IncrementOrDecrement(string incrementOrDecrement)
        => TestMissingInRegularAndScriptWithAllOptionsAsync(
            $$"""
            class C
            {
                int M(int x)
                {
                    [|{{incrementOrDecrement}}|];
                    return x;
                }
            }
            """);

    [Fact]
    public Task ExpressionStatement_UnusedLocal_NameAlreadyUsed()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var unused = M2();
                    [|M2()|];
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {
                    var unused = M2();
                    var unused1 = M2();
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Fact]
    public Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_02()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|M2()|];
                    var unused = M2();
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {
                    var unused1 = M2();
                    var unused = M2();
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Fact]
    public Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_03()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int p)
                {
                    [|M2()|];
                    if (p > 0)
                    {
                        var unused = M2();
                    }
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M(int p)
                {
                    var unused1 = M2();
                    if (p > 0)
                    {
                        var unused = M2();
                    }
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Fact]
    public Task ExpressionStatement_UnusedLocal_NameAlreadyUsed_04()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int p)
                {
                    if (p > 0)
                    {
                        [|M2()|];
                    }
                    else
                    {
                        var unused = M2();
                    }
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M(int p)
                {
                    if (p > 0)
                    {
                        var unused1 = M2();
                    }
                    else
                    {
                        var unused = M2();
                    }
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Theory]
    [InlineData(nameof(PreferDiscard), "_", "_", "_")]
    [InlineData(nameof(PreferUnusedLocal), "var unused", "var unused", "var unused3")]
    public Task ExpressionStatement_FixAll(string optionName, string fix1, string fix2, string fix3)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C()
                {
                    M2();           // Separate code block
                }

                void M(int unused1, int unused2)
                {
                    {|FixAllInDocument:M2()|};
                    M2();           // Another instance in same code block
                    _ = M2();       // Already fixed
                    var x = M2();   // Different unused value diagnostic
                }

                int M2() => 0;
            }
            """,
            $$"""
            class C
            {
                public C()
                {
                    {{fix1}} = M2();           // Separate code block
                }

                void M(int unused1, int unused2)
                {
                    {{fix3}} = M2();
                    {{fix2}} = M2();           // Another instance in same code block
                    _ = M2();       // Already fixed
                    var x = M2();   // Different unused value diagnostic
                }

                int M2() => 0;
            }
            """, optionName);

    [Fact]
    public Task ExpressionStatement_Trivia_PreferDiscard_01()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // C1
                    [|M2()|];   // C2
                    // C3
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {
                    // C1
                    _ = M2();   // C2
                    // C3
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferDiscard));

    [Fact]
    public Task ExpressionStatement_Trivia_PreferDiscard_02()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {/*C0*/
                    /*C1*/[|M2()|]/*C2*/;/*C3*/
                 /*C4*/
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {/*C0*/
                    /*C1*/
                    _ = M2()/*C2*/;/*C3*/
                 /*C4*/
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferDiscard));

    [Fact]
    public Task ExpressionStatement_Trivia_PreferUnusedLocal_01()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // C1
                    [|M2()|];   // C2
                    // C3
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {
                    // C1
                    var unused = M2();   // C2
                    // C3
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Fact]
    public Task ExpressionStatement_Trivia_PreferUnusedLocal_02()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {/*C0*/
                    /*C1*/[|M2()|]/*C2*/;/*C3*/
                 /*C4*/
                }

                int M2() => 0;
            }
            """,
            """
            class C
            {
                void M()
                {/*C0*/
                    /*C1*/
                    var unused = M2()/*C2*/;/*C3*/
                    /*C4*/
                }

                int M2() => 0;
            }
            """, new TestParameters(options: PreferUnusedLocal));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32942")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionBodiedMember_01(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M() => [|M2()|];
                int M2() => 0;
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32942")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionBodiedMember_02(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    System.Action a = () => [|M2()|];
                }

                int M2() => 0;
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/32942")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionBodiedMember_03(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    LocalFunction();
                    return;

                    void LocalFunction() => [|M2()|];
                }

                int M2() => 0;
            }
            """, optionName);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43648")]
    [InlineData(nameof(PreferDiscard))]
    [InlineData(nameof(PreferUnusedLocal))]
    public Task ExpressionStatement_Dynamic(string optionName)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                        List<dynamic> returnValue = new List<dynamic>();

                        dynamic dynamicValue = new object();

                        [|returnValue.Add(dynamicValue)|];
                }
            }
            """, optionName);
}
