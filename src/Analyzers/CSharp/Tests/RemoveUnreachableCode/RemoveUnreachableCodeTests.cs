// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnreachableCode
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpRemoveUnreachableCodeDiagnosticAnalyzer, CSharpRemoveUnreachableCodeCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
    public class RemoveUnreachableCodeTests
    {
        [Fact]
        public async Task TestSingleUnreachableStatement()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInUnreachableIfBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        if (false)
                        {
                [|            var v = 0;
                |]        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (false)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInIfWithNoBlock()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        if (false)
                [|            {|CS1023:var v = 0;|}
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (false)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatements()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |][|        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFromSubsequentStatement()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |][|        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatementsExcludingLocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local() {}
                [|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local() {}
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatementsExcludingMultipleLocalFunctions()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local() {}
                        void Local2() {}
                [|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local() {}
                        void Local2() {}
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local() {}
                [|
                        var z = 2;
                |]
                        void Local2() {}
                [|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local() {}

                        void Local2() {}
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local() {}
                [|
                        var z = 2;
                        var z2 = 2;
                |]
                        void Local2() {}
                [|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local() {}

                        void Local2() {}
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveSubsequentStatementsUpToNextLabel()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |][|
                        label:
                            System.Console.WriteLine();
                |][|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnUnreachableLabel()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |][|
                        label:
                            System.Console.WriteLine();
                |][|
                        var y = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingOnReachableLabel()
        {
            var code = """
                class C
                {
                    void M(object o)
                    {
                        if (o != null)
                        {
                            goto label;
                        }

                        throw new System.Exception();
                [|        var v = 0;
                |]
                        label:
                            System.Console.WriteLine();

                        var y = 1;
                    }
                }
                """;
            var fixedCode = """
                class C
                {
                    void M(object o)
                    {
                        if (o != null)
                        {
                            goto label;
                        }

                        throw new System.Exception();

                        label:
                            System.Console.WriteLine();

                        var y = 1;
                    }
                }
                """;
            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact]
        public async Task TestInLambda()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                using System;

                class C
                {
                    void M()
                    {
                        Action a = () => {
                            if (true)
                                return;
                [|
                            Console.WriteLine();
                |]        };
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void M()
                    {
                        Action a = () => {
                            if (true)
                                return;
                        };
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInLambdaInExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                using System;

                class C
                {
                    Action M()
                        => () => {
                            if (true)
                                return;
                [|
                            Console.WriteLine();
                |]        };
                }
                """,
                """
                using System;

                class C
                {
                    Action M()
                        => () => {
                            if (true)
                                return;
                        };
                }
                """);
        }

        [Fact]
        public async Task TestSingleRemovalDoesNotTouchCodeInUnrelatedLocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local()
                        {
                            throw new System.Exception();
                [|            var x = 0;
                |]        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local()
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|        var v = 0;
                |]
                        void Local()
                        {
                            throw new System.Exception();
                [|            var x = 0;
                |]        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                        void Local()
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        if (o == null)
                        {
                            goto ReachableLabel;
                        }

                        throw new System.Exception();
                [|        var v = 0;
                |][|
                        UnreachableLabel:
                            System.Console.WriteLine(o);
                |]
                        ReachableLabel:
                            System.Console.WriteLine(o.ToString());
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        if (o == null)
                        {
                            goto ReachableLabel;
                        }

                        throw new System.Exception();

                        ReachableLabel:
                            System.Console.WriteLine(o.ToString());
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        if (o == null)
                        {
                            goto ReachableLabel2;
                        }

                        throw new System.Exception();
                [|        var v = 0;
                |]
                        ReachableLabel1:
                            System.Console.WriteLine(o);

                        ReachableLabel2:
                        {
                            System.Console.WriteLine(o.ToString());
                            goto ReachableLabel1;
                        }
                [|
                        var x = 1;
                |]    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        if (o == null)
                        {
                            goto ReachableLabel2;
                        }

                        throw new System.Exception();

                        ReachableLabel1:
                            System.Console.WriteLine(o);

                        ReachableLabel2:
                        {
                            System.Console.WriteLine(o.ToString());
                            goto ReachableLabel1;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll4()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        for (int i = 0; i < 10; i = i + 1)
                        {
                            for (int j = 0; j < 10; j = j + 1)
                            {
                                goto stop;
                [|                goto outerLoop;
                |]            }
                        outerLoop:
                            return;
                        }
                    stop:
                        return;
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        for (int i = 0; i < 10; i = i + 1)
                        {
                            for (int j = 0; j < 10; j = j + 1)
                            {
                                goto stop;
                            }
                        outerLoop:
                            return;
                        }
                    stop:
                        return;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll5()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        if (false)
                            throw new System.Exception();

                        throw new System.Exception();
                [|        return;
                |]    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        if (false)
                            throw new System.Exception();

                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInUnreachableInSwitchSection1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        switch (i)
                        {
                            case 0:
                                throw new System.Exception();
                [|                var v = 0;
                |][|                break;
                |]        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        switch (i)
                        {
                            case 0:
                                throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDirectives1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();
                [|
                #if true
                        var v = 0;
                |]#endif
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        throw new System.Exception();

                #if true
                #endif
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDirectives2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                #if true
                        throw new System.Exception();
                [|        var v = 0;
                |]#endif
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                #if true
                        throw new System.Exception();
                #endif
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDirectives3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                #if true
                        throw new System.Exception();
                [|#endif
                        var v = 0;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                #if true
                        throw new System.Exception();

                #endif
                    }
                }
                """);
        }

        [Fact]
        public async Task TestForLoop1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 5;)
                        {
                            i = 2;
                            goto Lab2;
                [|            i = 1;
                |][|            break;
                |]        Lab2:
                            return ;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 5;)
                        {
                            i = 2;
                            goto Lab2;
                        Lab2:
                            return ;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInfiniteForLoop()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                class C
                {
                    void M()
                    {
                        for (;;) { }
                [|        return;
                |]    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        for (;;) { }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_EndingWithNewLine()
        {
            var code = """
                throw new System.Exception();
                [|System.Console.ReadLine();
                |]
                """;
            var fixedCode = """
                throw new System.Exception();

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_NotEndingWithNewLine()
        {
            var code = """
                throw new System.Exception();
                [|System.Console.ReadLine();|]
                """;
            var fixedCode = """
                throw new System.Exception();

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_MultipleUnreachableStatements()
        {
            var code = """
                throw new System.Exception();
                [|System.Console.ReadLine();
                |][|System.Console.ReadLine();
                |]
                """;
            var fixedCode = """
                throw new System.Exception();

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_MultipleUnreachableStatements_HasClassDeclarationInBetween()
        {
            var code = """
                throw new System.Exception();
                [|System.Console.ReadLine();
                |]

                public class C { }
                [|
                {|CS8803:System.Console.ReadLine();|}|]
                """;
            var fixedCode = """
                throw new System.Exception();


                public class C { }

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_MultipleUnreachableStatements_AfterClassDeclaration1()
        {
            var code = """
                throw new System.Exception();

                public class C { }
                [|
                {|CS8803:System.Console.ReadLine();|}|]
                """;
            var fixedCode = """
                throw new System.Exception();

                public class C { }

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
        public async Task TestTopLevel_MultipleUnreachableStatements_AfterClassDeclaration2()
        {
            var code = """
                public class C { }

                {|CS8803:throw new System.Exception();|}
                [|System.Console.ReadLine();|]
                """;
            var fixedCode = """
                public class C { }

                {|CS8803:throw new System.Exception();|}

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }
    }
}
