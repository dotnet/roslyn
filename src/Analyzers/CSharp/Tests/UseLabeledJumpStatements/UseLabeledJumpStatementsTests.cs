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
                                    {|IDE0410:goto|} found;
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
                                {|IDE0410:goto|} done;
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
                                    {|IDE0410:goto|} done;
                                if (j == 2)
                                    {|IDE0410:goto|} done;
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
                                    {|IDE0410:goto|} next;
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
    public Task TestFlagBreak_ForLoopSynthesizesLoopVariableName()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (i * j > 20)
                                {
                                    found = true;
                                    {|IDE0410:break;|}
                                }
                            }

                            if (found)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                    loop_i: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (i * j > 20)
                                {
                                    break loop_i;
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagContinue_WhileLoopSynthesizesOuter()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool a, bool b)
                    {
                        bool skip = false;
                        while (a)
                        {
                            while (b)
                            {
                                if (a && b)
                                {
                                    skip = true;
                                    {|IDE0410:break;|}
                                }
                            }

                            if (skip)
                                continue;

                            System.Console.WriteLine();
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool a, bool b)
                    {
                    outer: while (a)
                        {
                            while (b)
                            {
                                if (a && b)
                                {
                                    continue outer;
                                }
                            }

                            System.Console.WriteLine();
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagBreak_MultipleSites()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int n)
                    {
                        bool found = false;
                        for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (i == j)
                                {
                                    found = true;
                                    {|IDE0410:break;|}
                                }

                                if (i > j)
                                {
                                    found = true;
                                    {|IDE0410:break;|}
                                }
                            }

                            if (found)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int n)
                    {
                    loop_i: for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (i == j)
                                {
                                    break loop_i;
                                }

                                if (i > j)
                                {
                                    break loop_i;
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagReadElsewhere()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool cond)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (cond)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                break;
                        }

                        System.Console.WriteLine(found);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_SwitchTarget()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int x)
                    {
                        switch (x)
                        {
                            case 1:
                                for (int i = 0; i < 10; i++)
                                {
                                    if (i == 5)
                                        {|IDE0410:goto|} done;
                                }

                                break;
                        }

                        done:
                        System.Console.WriteLine();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int x)
                    {
                    done: switch (x)
                        {
                            case 1:
                                for (int i = 0; i < 10; i++)
                                {
                                    if (i == 5)
                                        break done;
                                }

                                break;
                        }

                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_SingleLevelSwitchBreak()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int x)
                    {
                        switch (x)
                        {
                            case 1:
                                if (x > 0)
                                    goto done;
                                break;
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
