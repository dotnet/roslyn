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
    public async Task TestAssignment_ObjectType()
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
    public async Task TestAssignment_SameType()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(bool b)
                {
                    long a;
                    a = $$b ? 0 : 1L;
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
    public async Task TestCompoundAssignment()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(bool b)
                {
                    int a = 0;
                    a += $$b ? 1 : 2;
                }
            }
            """,
            """
            class C
            {
                void M(bool b)
                {
                    int a = 0;
                    if (b)
                    {
                        a += 1;
                    }
                    else
                    {
                        a += 2;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithExpression()
    {
        await new VerifyCS.Test
        {
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
            TestCode =
            """
            record X(int A);
            class C
            {
                void M(bool b, X x)
                {
                    x = x with { A = $$b ? 1 : 2 };
                }
            }
            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit
                {
                }
            }
            """,
            FixedCode =
            """
            record X(int A);
            class C
            {
                void M(bool b, X x)
                {
                    if (b)
                    {
                        x = x with { A = 1 };
                    }
                    else
                    {
                        x = x with { A = 2 };
                    }
                }
            }
            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit
                {
                }
            }
            """
        }.RunAsync();
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

    [Fact]
    public async Task ExpressionStatement_SecondInvocationArgument()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    Console.WriteLine(b ? "" : "", $$b ? 0 : 1L);
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
                        Console.WriteLine(b ? "" : "", (long)0);
                    }
                    else
                    {
                        Console.WriteLine(b ? "" : "", 1L);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestThrowStatement1()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            class C
            {
                void M(bool b)
                {
                    throw $$b ? new Exception("x") : new Exception("y");
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
                        throw new Exception("x");
                    }
                    else
                    {
                        throw new Exception("y");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestYieldReturn1()
    {
        await VerifyCS.VerifyRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    yield return $$b ? 0 : 1L;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    if (b)
                    {
                        yield return (long)0;
                    }
                    else
                    {
                        yield return 1L;
                    }
                }
            }
            """);
    }
}
