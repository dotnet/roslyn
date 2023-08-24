// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveRedundantElseStatement;

using VerifyCS = CSharpCodeFixVerifier<
    RemoveRedundantElseStatementDiagnosticAnalyzer,
    RemoveRedundantElseStatementCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveRedundantElseStatement)]
public sealed class RemoveRedundantElseStatementTests
{
    [Fact]
    public async Task TestSimpleIfElse()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        return Fib(n - 1) + Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimpleIfElseWithSingleStatement()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                        return 1;
                    [|else|]
                        return Fib(n - 1) + Fib(n - 2);
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                        return 1;
                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestIfElseIfElseWithThrow()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n < 0)
                    {
                        throw new ArgumentException("n can't be negative");
                    }
                    else if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        return Fib(n - 1) + Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n < 0)
                    {
                        throw new ArgumentException("n can't be negative");
                    }
                    else if (n <= 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleIfElseWithThrow()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n < 0)
                    {
                        throw new ArgumentException("n can't be negative");
                    }
                    else if (n == 1)
                    {
                        return 1;
                    }
                    else if (n == 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        return Fib(n - 1) + Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n < 0)
                    {
                        throw new ArgumentException("n can't be negative");
                    }
                    else if (n == 1)
                    {
                        return 1;
                    }
                    else if (n == 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestIfElseIfElseWithoutThrow()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    int Fib(int n) 
                    {
                        if (n < 0)
                        {
                            Console.WriteLine("Error");
                        }
                        else if (n <= 2)
                        {
                            return 1;
                        }

                        return Fib(n - 1) + Fib(n - 2);
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestElseIfWithoutJump()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                    
                class C
                {
                    int Fib(int n) 
                    {
                        if (n < 0)
                        {
                            throw new ArgumentException();
                        }
                        else if (n <= 2)
                        {
                        }

                        return Fib(n - 1) + Fib(n - 2);
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestIfElseWithBreak()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i == n)
                        {
                            break;
                        }
                        [|else|]
                        {
                            i++;
                        }
                    }

                    return i;
                }
            }
            """, """
            using System;
            
            class C
            {
                int M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i == n)
                        {
                            break;
                        }

                        i++;
                    }
            
                    return i;
                }
            }
            """);
    }

    [Fact]
    public async Task TestIfElseWithContinue()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i < n)
                        {
                            i+=1;
                            continue;
                        }
                        [|else|]
                        {
                            return i;
                        }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i < n)
                        {
                            i+=1;
                            continue;
                        }

                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestIfElseWithoutContinue()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    int M(int n) 
                    {
                        int i = 0;
                        while (true)
                        {
                            if (i < n)
                            {
                                i+=1;
                            }
                            else
                            {
                                return i;
                            }
                        }
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestYieldBreak()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i == n)
                        {
                            yield break;
                        }
                        [|else|]
                        {
                            yield return i++;
                        }
                    }
                }
            }
            """, """
            using System;
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> M(int n) 
                {
                    int i = 0;
                    while (true)
                    {
                        if (i == n)
                        {
                            yield break;
                        }

                        yield return i++;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestYieldReturn()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M(int n) 
                    {
                        int i = 0;
                        while (true)
                        {
                            if (i < n)
                            {
                                yield return i++;
                            }
                            else
                            {
                                yield break;
                            }
                        }
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestInSwitchCase()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    switch (n)
                    {
                        case 0:
                            if (true)
                            {
                                return 0;
                            }
                            [|else|]
                            {
                                return 1;
                            }

                        default:
                            return 2;
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    switch (n)
                    {
                        case 0:
                            if (true)
                            {
                                return 0;
                            }

                            return 1;
                        default:
                            return 2;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestInSwitchDefaultCase()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    switch (n)
                    {
                        case 0:
                            return 2;

                        default:
                            if (true)
                            {
                                return 0;
                            }
                            [|else|]
                            {
                                return 1;
                            }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    switch (n)
                    {
                        case 0:
                            return 2;

                        default:
                            if (true)
                            {
                                return 0;
                            }

                            return 1;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedIfStatements()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        if (false)
                        {
                            return 0;
                        }
                        [|else|]
                        {
                            return 0;
                        }
                    }
                    [|else|]
                    {
                        return 0;
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        if (false)
                        {
                            return 0;
                        }

                        return 0;
                    }

                    return 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedIfStatementsInElse()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        if (false)
                        {
                            return 0;
                        }
                        [|else|]
                        {
                            return 1;
                        }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        return 1;
                    }

                    if (false)
                    {
                        return 0;
                    }

                    return 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNestedIfStatementsInSwitch()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        switch (n)
                        {
                            default:
                                if (true)
                                {
                                    break;
                                }
                                [|else|]
                                {
                                    break;
                                }
                        }

                        return 0;
                    }
                    [|else|]
                    {
                        return 0;
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int M(int n) 
                {
                    if (true)
                    {
                        switch (n)
                        {
                            default:
                                if (true)
                                {
                                    break;
                                }

                                break;
                        }

                        return 0;
                    }

                    return 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestGlobalStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                if (false)
                {
                    return;
                }
                [|else|]
                {
                    Console.WriteLine("Success");
                }
                """,
            FixedCode = """
                using System;

                if (false)
                {
                    return;
                }

                Console.WriteLine("Success");

                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }

    [Fact]
    public async Task TestVariableCollisionInIf()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    int M(int n) 
                    {
                        int i = 0;
                        while (true)
                        {
                            if (i == n)
                            {
                                i+=1;
                                int j = 0;
                                return j;
                            }
                            else
                            {
                                int j = 0;
                                return j;
                            }
                        }
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestVariableCollisionInSeparateBlock()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int M(int n) 
                    {
                        int i = 0;
                        while (true)
                        {
                            if (i < n)
                            {
                                i+=1;
                                continue;
                            }
                            else
                            {
                                int j = 0;
                            }

                            {
                                int j = 0;
                            }
                        }
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestVariableCollisionInSwitch()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int M(int n) 
                    {
                        switch (n)
                        {
                            case 0:
                                if (false)
                                {
                                    return 0;
                                }
                                else
                                {
                                    int m = 0;
                                    break;
                                }

                            default:
                                {
                                    int m = 0;
                                }
                                return 0;
                        }

                        return 0;
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestVariableCollisionGlobalStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                if (false) 
                {
                    throw new Exception("");
                }
                else
                {
                    int i = 0;
                }

                {
                    int i = 1;
                }
                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }

    [Fact]
    public async Task TestVariableCollisionGlobalStatementSwitch()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                int n = 0;
                switch (n)
                {
                    case 0:
                        if (false)
                        {
                            break;
                        }
                        else
                        {
                            int m = 0;
                            break;
                        }
                    
                    default:
                        {
                            int m = 0;
                        }
                        break;
                }
                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoVariableCollisionLocalFunction()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    int M(int n) 
                    {
                        int i = 0;
                        if (i < n)
                        {
                            return 1;
                        }
                        [|else|]
                        {
                            int j = 0;
                            return j;
                        }

                        void L()
                        {
                            int j = 1;
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    int M(int n) 
                    {
                        int i = 0;
                        if (i < n)
                        {
                            return 1;
                        }

                        int j = 0;
                        return j;
                        void L()
                        {
                            int j = 1;
                        }
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoVariableCollisionLambda()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    int M(int n) 
                    {
                        Func<int, int> lamb = x => 
                        {
                            int j = 3;
                            return j + x; 
                        };

                        int i = 0;
                        if (i < n)
                        {
                            return 1;
                        }
                        [|else|]
                        {
                            int j = 0;
                            return j;
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    int M(int n) 
                    {
                        Func<int, int> lamb = x => 
                        {
                            int j = 3;
                            return j + x; 
                        };

                        int i = 0;
                        if (i < n)
                        {
                            return 1;
                        }

                        int j = 0;
                        return j;
                    }
                }
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestFormatting1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|] { return Fib(n - 1) + Fib(n - 2); }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|] return Fib(n - 1) + Fib(n - 2);
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        return Fib(n - 1) +
                            Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    return Fib(n - 1) +
                        Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting4()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        // leading comment 1
                        /* leading comment 2 */
                        return Fib(n - 1) +
                            Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
            
                    // leading comment 1
                    /* leading comment 2 */
                    return Fib(n - 1) +
                        Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting5()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|]
                    {
                        int i = 0;

                        // leading comment 1
                        /* leading comment 2 */
                        return Fib(n - 1) +
                            Fib(n - 2);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    int i = 0;
            
                    // leading comment 1
                    /* leading comment 2 */
                    return Fib(n - 1) +
                        Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting6()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }
                    [|else|] { int i = 1; return Fib(n - 1) + Fib(n - 2); }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2)
                    {
                        return 1;
                    }

                    int i = 1; return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting7()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) { return 1; } [|else|] { return Fib(n - 1) + Fib(n - 2); }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) { return 1; }

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting8()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) { return 1; } [|else|] { int i = 1; return Fib(n - 1) + Fib(n - 2); }
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) { return 1; }

                    int i = 1; return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }

    [Fact]
    public async Task TestFormatting9()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) return 1; [|else|] return Fib(n - 1) + Fib(n - 2);
                }
            }
            """, """
            using System;

            class C
            {
                int Fib(int n) 
                {
                    if (n <= 2) return 1;

                    return Fib(n - 1) + Fib(n - 2);
                }
            }
            """);
    }
}
