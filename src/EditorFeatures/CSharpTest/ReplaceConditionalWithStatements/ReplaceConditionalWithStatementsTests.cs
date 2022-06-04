// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ReplaceConditionalWithStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReplaceConditionalWithStatements;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpReplaceConditionalWithStatementsCodeRefactoringProvider>;

public class ReplaceConditionalWithStatementsTests
{
    [Fact]
    public async Task TestAssignment1()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(bool b)
                {
                    object a;
                    a = $$b ? 0 : 1L;
                }
            }
            """,
            """
            class C
            {
                void M(bool b)
                {
                    object a;
                    if (b)
                    {
                        a = (long)0;
                    }
                    else
                    {
                        a = 1L;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestLocalDeclarationStatement1()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(bool b)
                {
                    object a = $$b ? 0 : 1L;
                }
            }
            """,
            """
            class C
            {
                void M(bool b)
                {
                    object a;
                    if (b)
                    {
                        a = (long)0;
                    }
                    else
                    {
                        a = 1L;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestLocalDeclarationStatement_WithVar()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(bool b)
                {
                    var a = $$b ? 0 : 1L;
                }
            }
            """,
            """
            class C
            {
                void M(bool b)
                {
                    long a;
                    if (b)
                    {
                        a = 0;
                    }
                    else
                    {
                        a = 1L;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestReturnStatement_ObjectReturn()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                object M(bool b)
                {
                    return $$b ? 0 : 1L;
                }
            }
            """,
            """
            class C
            {
                object M(bool b)
                {
                    if (b)
                    {
                        return (long)0;
                    }
                    else
                    {
                        return 1L;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestReturnStatement_AcualTypeReturn()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                long M(bool b)
                {
                    return $$b ? 0 : 1L;
                }
            }
            """,
            """
            class C
            {
                long M(bool b)
                {
                    if (b)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1L;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task ExpressionStatement_SimpleInvocationArgument()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    Console.WriteLine($$b ? 0 : 1L);
                }
            }
            """,
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        Console.WriteLine((long)0);
                    }
                    else
                    {
                        Console.WriteLine(1L);
                    }
                }
            }
            """);
    }
}
