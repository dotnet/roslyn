// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.EmbeddedStatementPlacement
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmbeddedStatementPlacementDiagnosticAnalyzer,
        EmbeddedStatementPlacementCodeFixProvider>;

    public class EmbeddedStatementPlacementTests
    {
        [Fact]
        public async Task NoErrorOnWrappedStatement()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true)
                            return;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnNonWrappedIfStatement()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true) [|return|];
                    }
                }
                """;
            var fixedCode = """
                class TestClass
                {
                    void M()
                    {
                        if (true)
                            return;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task NoErrorOnNonWrappedIfStatement_WhenOptionDisabled()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true) return;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, true, NotificationOption2.Suggestion } }
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnElseIf()
        {
            var source = """
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
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnElseWithNonIfStatementOnSameLine()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true)
                            return;
                        else [|return|];
                    }
                }
                """;
            var fixedCode = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnIfWithSingleLineBlock()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true) [|{|] return; }
                    }
                }
                """;
            var fixedCode = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task NoWrappingForMemberOrLambdaBlock()
        {
            var source = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task WrappingForLocalFunction()
        {
            var source = """
                class TestClass
                {
                    void N()
                    {
                        void Local() [|{|] return; }
                    }
                }
                """;
            var fixedCode = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task ErrorOnNonWrappedIfStatementWithEmptyBlock()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true) [|{|] }
                    }
                }
                """;
            var fixedCode = """
                class TestClass
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task WrapLambdaWithNestedStatement()
        {
            var source = """
                using System;

                class TestClass
                {
                    void N()
                    {
                        Action a1 = () => { [|if|] (true) return; };
                    }
                }
                """;
            var fixedCode = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task FixAll1()
        {
            var source = """
                class TestClass
                {
                    void M()
                    {
                        if (true) [|return|];
                        if (true) [|return|];
                    }
                }
                """;
            var fixedCode = """
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
                """;
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                Options = { { CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }
    }
}
