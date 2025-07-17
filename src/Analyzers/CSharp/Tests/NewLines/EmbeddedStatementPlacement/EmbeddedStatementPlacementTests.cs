// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.EmbeddedStatementPlacement;

using VerifyCS = CSharpCodeFixVerifier<
    EmbeddedStatementPlacementDiagnosticAnalyzer,
    EmbeddedStatementPlacementCodeFixProvider>;

public sealed class EmbeddedStatementPlacementTests
{
    [Fact]
    public Task NoErrorOnWrappedStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ErrorOnNonWrappedIfStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true) [|return|];
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NoErrorOnNonWrappedIfStatement_WhenOptionDisabled()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true) return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true, NotificationOption2.Suggestion } }
        }.RunAsync();

    [Fact]
    public Task NotOnElseIf()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                    else if (true)
                        return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ErrorOnElseWithNonIfStatementOnSameLine()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                    else [|return|];
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                    else
                        return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ErrorOnIfWithSingleLineBlock()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true) [|{|] return; }
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task NoWrappingForMemberOrLambdaBlock()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            class TestClass
            {
                void M() { return; }
                void N()
                {
                    Action a1 = () => { return; };
                    Action a2 = delegate () { return; };
                }

                int Prop1 { get { return 1; } }
                int Prop2
                {
                    get { return 1; }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task WrappingForLocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void N()
                {
                    void Local() [|{|] return; }
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void N()
                {
                    void Local()
                    {
                        return;
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task ErrorOnNonWrappedIfStatementWithEmptyBlock()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true) [|{|] }
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                    {
                    }
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task WrapLambdaWithNestedStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            class TestClass
            {
                void N()
                {
                    Action a1 = () => { [|if|] (true) return; };
                }
            }
            """,
            FixedCode = """
            using System;

            class TestClass
            {
                void N()
                {
                    Action a1 = () =>
                    {
                        if (true)
                            return;
                    };
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact]
    public Task FixAll1()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    if (true) [|return|];
                    if (true) [|return|];
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    if (true)
                        return;
                    if (true)
                        return;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66017")]
    public Task SwitchFollowedByEmptyStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            class TestClass
            {
                void M()
                {
                    switch (0)
                    {
                    }[|;|]
                }
            }
            """,
            FixedCode = """
            class TestClass
            {
                void M()
                {
                    switch (0)
                    {
                    }

                    ;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
}
