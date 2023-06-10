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
        public async Task TestRedundantElseFix_IfElseWithBreak()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                using System;

                class C
                {
                    int Count(int n) 
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
                    int Count(int n) 
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
                    int Count(int n) 
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
                    int Count(int n) 
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
                        int Count(int n) 
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
    }
}
