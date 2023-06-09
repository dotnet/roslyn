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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement
{
    using VerifyCS = CSharpCodeFixVerifier<
        RemoveRedundantElseStatementDiagnosticAnalyzer,
        RemoveRedundantElseStatementCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
    public class RemoveRedundantElseStatementTests
    {
        [Fact]
        public async Task TestRedundantElseFix_1()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                using System;

                class C
                {
                    int M(int value) {
                        if (value == 0)
                        {
                            return 1;
                        }
                        [|else|]
                        {
                            return 2;
                        }
                    }
                }
                """, """
                using System;
                
                class C
                {
                    int M(int value) {
                        if (value == 0)
                        {
                            return 1;
                        }

                        return 2;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRedundantElseFix_2()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                using System;

                class C
                {
                    int M(int value) {
                        if (value == 0)
                        {
                            return 1;
                        }
                        else if (value == 1)
                        {
                            return 2;
                        }
                        [|else|]
                        {
                            return 3;
                        }
                    }
                }
                """, """
                using System;
                
                class C
                {
                    int M(int value) {
                        if (value == 0)
                        {
                            return 1;
                        }
                        else if (value == 1)
                        {
                            return 2;
                        }

                        return 3;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRedundantElseFix_3()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                using System;

                class C
                {
                    int M(int value) {
                        int x = 0;

                        if (value == 0)
                        {
                            return 1;
                        }
                        [|else|]
                        {
                            x = 2;
                        }

                        return x;
                    }
                }
                """, """
                using System;
                
                class C
                {
                    int M(int value) {
                        int x = 0;
                
                        if (value == 0)
                        {
                            return 1;
                        }

                        x = 2;

                        return x;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRedundantElse_1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    using System;
                    
                    class C
                    {
                        int M(int value) {
                            if (value == 0)
                            {
                                return 1;
                            }
                            [|else|]
                            {
                                return 2;
                            }
                        }
                    }
                    """
            }.RunAsync();
        }
    }
}
