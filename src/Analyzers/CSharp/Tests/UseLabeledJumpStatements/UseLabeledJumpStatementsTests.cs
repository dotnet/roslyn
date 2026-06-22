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
    public Task TestGotoBreak_LabelReferencedElsewhere()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        if (b)
                            goto found;

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
                    void M(bool b)
                    {
                        if (b)
                            goto found;

                    loop_x: for (int x = 0; x < 10; x++)
                        {
                            for (int y = 0; y < 10; y++)
                            {
                                if (x * y > 20)
                                    break loop_x;
                            }
                        }

                        found:
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
    public Task TestBreakAndContinueTargetingSameLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int n)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (i == j)
                                    {|IDE0410:goto|} brk;
                                if (i < j)
                                    {|IDE0410:goto|} cont;
                            }

                            cont: ;
                        }

                        brk:
                        System.Console.WriteLine();
                    }
                }
                """,
            // The incremental fix relabels the loop with the first jump it encounters in source order ('goto brk'),
            // whereas the fix-all pass processes them in the opposite order and lands on 'cont'.  Both rewrites are
            // equivalent; they only differ in the (arbitrary) reused label name.
            FixedCode = """
                class C
                {
                    void M(int n)
                    {
                    brk: for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (i == j)
                                    break brk;
                                if (i < j)
                                    continue brk;
                            }
                        }

                        System.Console.WriteLine();
                    }
                }
                """,
            BatchFixedCode = """
                class C
                {
                    void M(int n)
                    {
                    cont: for (int i = 0; i < n; i++)
                        {
                            for (int j = 0; j < n; j++)
                            {
                                if (i == j)
                                    break cont;
                                if (i < j)
                                    continue cont;
                            }
                        }

                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_NestedLoopsDifferentTargets()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        {|IDE0410:goto|} outer;
                                    if (y)
                                        {|IDE0410:goto|} middle;
                                }
                            }

                            middle: ;
                        }

                        outer: ;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                    outer: for (int i = 0; i < 2; i++)
                        {
                        middle: for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        break outer;
                                    if (y)
                                        break middle;
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_NestedLoopsSameTarget()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        {|IDE0410:goto|} done;
                                }

                                if (y)
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
                    void M(bool x, bool y)
                    {
                    done: for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        break done;
                                }

                                if (y)
                                    break done;
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoContinue_NestedLoopsDifferentTargets()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        {|IDE0410:goto|} continueOuter;
                                    if (y)
                                        {|IDE0410:goto|} continueMiddle;
                                }

                                System.Console.WriteLine(j);
                                continueMiddle: ;
                            }

                            System.Console.WriteLine(i);
                            continueOuter: ;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                    continueOuter: for (int i = 0; i < 2; i++)
                        {
                        continueMiddle: for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (x)
                                        continue continueOuter;
                                    if (y)
                                        continue continueMiddle;
                                }

                                System.Console.WriteLine(j);
                            }

                            System.Console.WriteLine(i);
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_NestedDifferentTargets()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                        bool a = false;
                        for (int i = 0; i < 2; i++)
                        {
                            bool b = false;
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (y)
                                    {
                                        b = true;
                                        {|IDE0410:break|};
                                    }
                                }

                                if (b)
                                    break;

                                if (x)
                                {
                                    a = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (a)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool x, bool y)
                    {
                    loop_i: for (int i = 0; i < 2; i++)
                        {
                        loop_j: for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (y)
                                    {
                                        break loop_j;
                                    }
                                }

                                if (x)
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
    public Task TestFlag_MultiLevelChainBreak()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (c)
                                    {
                                        flag = true;
                                        {|IDE0410:break|};
                                    }
                                }

                                if (flag)
                                    break;
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                    loop_i: for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (c)
                                    {
                                        break loop_i;
                                    }
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_MultiLevelChainContinue()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (c)
                                    {
                                        flag = true;
                                        {|IDE0410:break|};
                                    }
                                }

                                if (flag)
                                    break;
                            }

                            if (flag)
                                continue;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                    loop_i: for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (c)
                                    {
                                        continue loop_i;
                                    }
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagChainIntermediateContinue()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (c)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }

                                if (flag)
                                    continue;
                            }

                            if (flag)
                                break;
                        }
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
                                    {|IDE0410:break|};
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
                                    {|IDE0410:break|};
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
                                    {|IDE0410:break|};
                                }

                                if (i > j)
                                {
                                    found = true;
                                    {|IDE0410:break|};
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
    public Task TestGotoBreak_LabelInsideSwitchSection()
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
                                    for (int j = 0; j < 10; j++)
                                    {
                                        if (i == j)
                                            {|IDE0410:goto|} done;
                                    }
                                }

                                done: ;
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int x)
                    {
                        switch (x)
                        {
                            case 1:
                            done: for (int i = 0; i < 10; i++)
                                {
                                    for (int j = 0; j < 10; j++)
                                    {
                                        if (i == j)
                                            break done;
                                    }
                                }
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_TopLevelStatements()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            TestCode = """
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (i == j)
                            {|IDE0410:goto|} done;
                    }
                }

                done:
                System.Console.WriteLine();
                """,
            FixedCode = """
                done: for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (i == j)
                            break done;
                    }
                }

                System.Console.WriteLine();
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

    [Fact]
    public Task TestNotOffered_GotoCase()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int x)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            switch (x)
                            {
                                case 0:
                                    goto case 1;
                                case 1:
                                    System.Console.WriteLine();
                                    break;
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_LabelHasNoPrecedingStatement()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        first: ;
                        for (int i = 0; i < 10; i++)
                        {
                            if (b)
                                goto first;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_ContinueLabelNotLastStatement()
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

                            next: ;
                            System.Console.WriteLine();
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_ContinueLabelInNonLoopBlock()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (b)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (j == 5)
                                        goto next;
                                }

                                next: ;
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_SingleLevelContinue()
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
                            if (i == 5)
                                goto next;
                            System.Console.WriteLine(i);
                            next: ;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagInitializedToTrue()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = true;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagAssignmentNotFollowedByBreak()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }

                                if (i == j)
                                    flag = true;
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagGuardIsReturn()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                return;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagGuardNotPrecededByLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            if (c)
                            {
                                flag = true;
                                break;
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagInnerBreakTargetsSwitch()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(int x)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                switch (x)
                                {
                                    default:
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagIsNullableBool()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool? flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag == true)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagIsField()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    bool flag;

                    void M(bool c)
                    {
                        flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagWithTwoGuards()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                break;
                            if (flag)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagContinue_ForLoopWithTrailingStatement()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        int count = 0;
                        bool skip = false;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 1; j++)
                            {
                                if (i == 0)
                                {
                                    skip = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (skip)
                                continue;

                            count++;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        int count = 0;
                    loop_i: for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 1; j++)
                            {
                                if (i == 0)
                                {
                                    continue loop_i;
                                }
                            }

                            count++;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_ExistingLoopLabelWithExternalReference()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool b)
                    {
                        if (b)
                            goto found;

                    existing: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                                {|IDE0410:goto|} found;
                        }

                        found:
                        System.Console.WriteLine();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool b)
                    {
                        if (b)
                            goto found;

                    existing: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                                break existing;
                        }

                        found:
                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_NestedWhileLoopsSynthesizeDistinctLabels()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool p, bool q, bool r, bool x, bool y)
                    {
                        bool a = false;
                        while (p)
                        {
                            bool b = false;
                            while (q)
                            {
                                while (r)
                                {
                                    if (y)
                                    {
                                        b = true;
                                        {|IDE0410:break|};
                                    }
                                }

                                if (b)
                                    break;

                                if (x)
                                {
                                    a = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (a)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool p, bool q, bool r, bool x, bool y)
                    {
                    outer1: while (p)
                        {
                        outer: while (q)
                            {
                                while (r)
                                {
                                    if (y)
                                    {
                                        break outer;
                                    }
                                }

                                if (x)
                                {
                                    break outer1;
                                }
                            }
                        }
                    }
                }
                """,
            BatchFixedCode = """
                class C
                {
                    void M(bool p, bool q, bool r, bool x, bool y)
                    {
                    outer: while (p)
                        {
                        outer1: while (q)
                            {
                                while (r)
                                {
                                    if (y)
                                    {
                                        break outer1;
                                    }
                                }

                                if (x)
                                {
                                    break outer;
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_RemovedDeclarationKeepsUnbalancedDirectives()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                #if true
                        bool found = false;
                #endif
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (i * j > 20)
                                {
                                    found = true;
                                    {|IDE0410:break|};
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

                #if true
                #endif
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
    public Task TestGotoContinue_EmbeddedLoopWrappedInBlock()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool cond)
                    {
                        if (cond)
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
                    void M(bool cond)
                    {
                        if (cond)
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
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_EmbeddedLoopWrappedInBlock()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool cond, bool c)
                    {
                        bool flag = false;
                        if (cond)
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                    if (c)
                                    {
                                        flag = true;
                                        {|IDE0410:break|};
                                    }

                                if (flag)
                                    break;
                            }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool cond, bool c)
                    {
                        if (cond)
                        {
                        loop_i: for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                    if (c)
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
    public Task TestFlagBreak_ForEachSynthesizesLoopVariableName()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> items)
                    {
                        bool found = false;
                        foreach (var item in items)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (item == j)
                                {
                                    found = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (found)
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> items)
                    {
                    loop_item: foreach (var item in items)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (item == j)
                                {
                                    break loop_item;
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagBreak_DoWhileSynthesizesOuter()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool found = false;
                        do
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (found)
                                break;
                        } while (c);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                    outer: do
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    break outer;
                                }
                            }
                        } while (c);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlag_BraceWrappedGuard()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool flag = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    flag = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (flag)
                            {
                                break;
                            }
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                    loop_i: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
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
    public Task TestFlag_DeclaredInsideTargetLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            bool found = false;
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    {|IDE0410:break|};
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
                    void M(bool c)
                    {
                    loop_i: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
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
    public Task TestNotOffered_FlagGuardHasElse()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                break;
                            else
                                System.Console.WriteLine();
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagBreak_SynthesizedLabelMatchesExistingLocalName()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        int loop_i = 0;
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    {|IDE0410:break|};
                                }
                            }

                            if (found)
                                break;
                        }

                        System.Console.WriteLine(loop_i);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                        int loop_i = 0;
                    loop_i: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    break loop_i;
                                }
                            }
                        }

                        System.Console.WriteLine(loop_i);
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_BracelessLoopBodies()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        while (c)
                            while (c)
                                if (c)
                                    {|IDE0410:goto|} found;

                        found:
                        System.Console.WriteLine();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                    found: while (c)
                            while (c)
                                if (c)
                                    break found;

                        System.Console.WriteLine();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_InsideTryBlock()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        try
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                        {|IDE0410:goto|} found;
                                }
                            }

                            found:
                            System.Console.WriteLine();
                        }
                        finally
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool c)
                    {
                        try
                        {
                        found: for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                        break found;
                                }
                            }

                            System.Console.WriteLine();
                        }
                        finally
                        {
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestGotoBreak_InsideLocalFunction()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        void Local(bool c)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                        {|IDE0410:goto|} found;
                                }
                            }

                            found:
                            System.Console.WriteLine();
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        void Local(bool c)
                        {
                        found: for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                        break found;
                                }
                            }

                            System.Console.WriteLine();
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagBreak_InsideLambda()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M()
                    {
                        System.Action<bool> a = c =>
                        {
                            bool found = false;
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                    {
                                        found = true;
                                        {|IDE0410:break|};
                                    }
                                }

                                if (found)
                                    break;
                            }
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        System.Action<bool> a = c =>
                        {
                        loop_i: for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (c)
                                    {
                                        break loop_i;
                                    }
                                }
                            }
                        };
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagBreak_DeeplyNested()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                for (int k = 0; k < 10; k++)
                                {
                                    for (int l = 0; l < 10; l++)
                                    {
                                        if (c)
                                        {
                                            found = true;
                                            {|IDE0410:break|};
                                        }
                                    }

                                    if (found)
                                        break;
                                }

                                if (found)
                                    break;
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
                    void M(bool c)
                    {
                    loop_i: for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                for (int k = 0; k < 10; k++)
                                {
                                    for (int l = 0; l < 10; l++)
                                    {
                                        if (c)
                                        {
                                            break loop_i;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_GotoDefault()
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
                                    for (int j = 0; j < 10; j++)
                                        goto default;
                                break;
                            default:
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagGuardCompoundCondition()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c, bool d)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found && d)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagGuardNegated()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNotOffered_FlagSitesDifferentInnerLoops()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool c, bool d)
                    {
                        bool found = false;
                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (c)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            for (int k = 0; k < 10; k++)
                            {
                                if (d)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                break;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFlagContinue_DeclaredInsideTargetLoop()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    void M(bool a, bool b)
                    {
                        while (a)
                        {
                            bool skip = false;
                            while (b)
                            {
                                if (a && b)
                                {
                                    skip = true;
                                    {|IDE0410:break|};
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
}
