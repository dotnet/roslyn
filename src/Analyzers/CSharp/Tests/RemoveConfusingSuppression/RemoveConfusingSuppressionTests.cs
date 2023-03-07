// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Confusing
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpRemoveConfusingSuppressionDiagnosticAnalyzer, CSharpRemoveConfusingSuppressionCodeFixProvider>;

    public class RemoveConfusingSuppressionTests
    {
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsExpression1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        if (o is string)
                        {
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsPattern1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string s)
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        if (o is string s)
                        {
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsExpression_CSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (!(o is string))
                        {
                        }
                    }
                }
                """,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsPattern_CSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string s)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (!(o is string s))
                        {
                        }
                    }
                }
                """,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsExpression_CSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o is not string)
                        {
                        }
                    }
                }
                """,
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsPattern_CSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string s)
                        {
                        }
                    }
                }
                """,
                FixedState =
                {
                    Sources =
                    {
                        """
                        class C
                        {
                            void M(object o)
                            {
                                if (o is not string s)
                                {
                                }
                            }
                        }
                        """
                    },
                },
                CodeActionIndex = 1,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsExpression_FixAll1()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string)
                        {
                        }
                        if (o [|!|]is string)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o is string)
                        {
                        }
                        if (o is string)
                        {
                        }
                    }
                }
                """,
                NumberOfFixAllIterations = 1,
                CodeActionEquivalenceKey = CSharpRemoveConfusingSuppressionCodeFixProvider.RemoveOperator,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsExpression_FixAll1()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string)
                        {
                        }
                        if (o [|!|]is string)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (!(o is string))
                        {
                        }
                        if (!(o is string))
                        {
                        }
                    }
                }
                """,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = CSharpRemoveConfusingSuppressionCodeFixProvider.NegateExpression,
                NumberOfFixAllIterations = 1,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestRemoveWithIsPatternExpression_FixAll1()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string s)
                        {
                        }
                        if (o [|!|]is string t)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o is string s)
                        {
                        }
                        if (o is string t)
                        {
                        }
                    }
                }
                """,
                NumberOfFixAllIterations = 1,
                CodeActionEquivalenceKey = CSharpRemoveConfusingSuppressionCodeFixProvider.RemoveOperator,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44872")]
        public async Task TestNegateWithIsPatternExpression_FixAll1()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (o [|!|]is string s)
                        {
                        }
                        if (o [|!|]is string t)
                        {
                        }
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void M(object o)
                    {
                        if (!(o is string s))
                        {
                        }
                        if (!(o is string t))
                        {
                        }
                    }
                }
                """,
                NumberOfFixAllIterations = 1,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = CSharpRemoveConfusingSuppressionCodeFixProvider.NegateExpression,
            }.RunAsync();
        }
    }
}
