// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseLabeledJumpStatements;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseLabeledJumpStatementsDiagnosticAnalyzer,
    CSharpUseLabeledJumpStatementsCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseLabeledJumpStatements)]
public sealed class UseLabeledJumpStatementsTests
{
    [Fact]
    public Task TestNotOfferedWhenFeatureUnavailable()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp13,
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (i * j > 20)
                                    goto found;
                            }
                        }

                        found:
                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_TwoLevels()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            for (int y = 0; y < 10; y++)
                            {
                                if (x * y > 20)
                                    {|IDE0400:goto|} found;
                            }
                        }

                        found:
                        System.Console.WriteLine();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                    found: for (int x = 0; x < 10; x++)
                        {
                            for (int y = 0; y < 10; y++)
                            {
                                if (x * y > 20)
                                    break found;
                            }
                        }

                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_EmptyLabelPad()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        while (true)
                        {
                            while (true)
                            {
                                {|IDE0400:goto|} done;
                            }
                        }

                        done: ;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                    done: while (true)
                        {
                            while (true)
                            {
                                break done;
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_MultipleJumpsToSameLabel()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (j == 1)
                                    {|IDE0400:goto|} done;
                                if (j == 2)
                                    {|IDE0400:goto|} done;
                            }
                        }

                        done:
                        System.Console.WriteLine();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                    done: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (j == 1)
                                    break done;
                                if (j == 2)
                                    break done;
                            }
                        }

                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoContinue_TwoLevels()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (j == 5)
                                    {|IDE0400:goto|} next;
                            }

                            next: ;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                    next: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (j == 5)
                                    continue next;
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_ContinueLabelOnNonEmptyStatement()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (j == 5)
                                    goto next;
                            }

                            next:
                            System.Console.WriteLine(i);
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_SingleLevelBreak()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        while (true)
                        {
                            if (b)
                                goto done;
                        }

                        done:
                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_JumpNotInsideLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        while (b)
                        {
                            break;
                        }

                        done:
                        System.Console.WriteLine();

                        if (b)
                            goto done;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_LabelAfterNonLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        if (b)
                        {
                            goto done;
                        }

                        done:
                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_LabelNotImmediatelyAfterLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        while (true)
                        {
                            goto done;
                        }

                        System.Console.WriteLine();
                        done:
                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();
}
