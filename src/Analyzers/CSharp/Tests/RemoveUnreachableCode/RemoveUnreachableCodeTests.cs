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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnreachableCode;

using VerifyCS = CSharpCodeFixVerifier<CSharpRemoveUnreachableCodeDiagnosticAnalyzer, CSharpRemoveUnreachableCodeCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
public sealed class RemoveUnreachableCodeTests
{
    [Fact]
    public Task TestSingleUnreachableStatement()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestInUnreachableIfBody()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestInIfWithNoBlock()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatements()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFromSubsequentStatement()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatementsExcludingLocalFunction()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatementsExcludingMultipleLocalFunctions()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions2()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRemoveSubsequentStatementsUpToNextLabel()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestOnUnreachableLabel()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestMissingOnReachableLabel()
        => VerifyCS.VerifyCodeFixAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestInLambda()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestInLambdaInExpressionBody()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestSingleRemovalDoesNotTouchCodeInUnrelatedLocalFunction()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFixAll1()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFixAll2()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFixAll3()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFixAll4()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestFixAll5()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestInUnreachableInSwitchSection1()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestDirectives1()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestDirectives2()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestDirectives3()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestForLoop1()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestInfiniteForLoop()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_EndingWithNewLine()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            throw new System.Exception();
            [|System.Console.ReadLine();
            |]
            """,
            FixedCode = """
            throw new System.Exception();

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_NotEndingWithNewLine()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            throw new System.Exception();
            [|System.Console.ReadLine();|]
            """,
            FixedCode = """
            throw new System.Exception();

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_MultipleUnreachableStatements()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            throw new System.Exception();
            [|System.Console.ReadLine();
            |][|System.Console.ReadLine();
            |]
            """,
            FixedCode = """
            throw new System.Exception();

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_MultipleUnreachableStatements_HasClassDeclarationInBetween()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            throw new System.Exception();
            [|System.Console.ReadLine();
            |]

            public class C { }
            [|
            {|CS8803:System.Console.ReadLine();|}|]
            """,
            FixedCode = """
            throw new System.Exception();


            public class C { }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_MultipleUnreachableStatements_AfterClassDeclaration1()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            throw new System.Exception();

            public class C { }
            [|
            {|CS8803:System.Console.ReadLine();|}|]
            """,
            FixedCode = """
            throw new System.Exception();

            public class C { }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61810")]
    public Task TestTopLevel_MultipleUnreachableStatements_AfterClassDeclaration2()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            TestCode = """
            public class C { }

            {|CS8803:throw new System.Exception();|}
            [|System.Console.ReadLine();|]
            """,
            FixedCode = """
            public class C { }

            {|CS8803:throw new System.Exception();|}

            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72024")]
    public Task TestIncompleteBinaryExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                public class C { }
            
                {|CS8803:throw new System.Exception();|}
                [|1+1|]{|CS1002:|}
                """,
            FixedCode = """
                public class C { }
            
                {|CS8803:throw new System.Exception();|}

                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
}
