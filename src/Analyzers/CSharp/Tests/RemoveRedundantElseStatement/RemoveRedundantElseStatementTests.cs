// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveRedundantElseStatement
{
    using VerifyCS = CSharpCodeFixVerifier<
        RemoveRedundantElseStatementDiagnosticAnalyzer,
        RemoveRedundantElseStatementCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveRedundantElseStatement)]
    public class RemoveRedundantElseStatementTests
    {
        [Fact]
        public async Task TestRedundantElseFix_SimpleIfElse()
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
        public async Task TestRedundantElseFix_SimpleIfElseWithSingleStatement()
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
        public async Task TestRedundantElseFix_IfElseIfElseWithThrow()
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
        public async Task TestRedundantElseFix_MultipleIfElseWithThrow()
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
        public async Task TestRedundantElseFix_IfElseIfElseWithoutThrow()
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
        public async Task TestRedundantElseFix_ElseIfWithoutJump()
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
        public async Task TestRedundantElseFix_IfElseWithBreak()
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
        public async Task TestRedundantElseFix_IfElseWithContinue()
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
        public async Task TestRedundantElseFix_IfElseWithoutContinue()
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
        public async Task TestRedundantElseFix_YieldBreak()
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
        public async Task TestRedundantElseFix_YieldReturn()
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
        public async Task TestRedundantElseFix_InSwitchCase()
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
        public async Task TestRedundantElseFix_InSwitchDefaultCase()
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
        public async Task TestRedundantElseFix_NestedIfStatements()
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
        public async Task TestRedundantElseFix_NestedIfStatementsInSwitch()
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
        public async Task TestRedundantElseFix_GlobalStatement()
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
        public async Task TestRedundantElseFix_VariableCollisionInIf()
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
        public async Task TestRedundantElseFix_VariableCollisionInSeparateBlock()
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
        public async Task TestRedundantElseFix_VariableCollisionInSwitch()
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
        public async Task TestRedundantElseFix_VariableCollisionGlobalStatement()
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
        public async Task TestRedundantElseFix_VariableCollisionGlobalStatementSwitch()
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
        public async Task TestRedundantElseFix_NoVariableCollision()
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
                                    continue;
                                }
                                [|else|]
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
    }
}
