' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.ReplaceConditionalWithStatements
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Xunit

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ReplaceConditionalWithStatements.VisualBasicReplaceConditionalWithStatementsCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ReplaceConditionalWithStatements

    Public Class ReplaceConditionalWithStatementsTests
        <Fact>
        Public Async Function TestAssignment_ObjectType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    a = $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    if (b)
                    {
                        a = (long)0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestAssignment_ObjectType_OnAssigment() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    $$a = b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    if (b)
                    {
                        a = (long)0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestAssignment_SameType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    long a
                    a = $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    long a
                    if (b)
                    {
                        a = 0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestAssignment_Discard() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    _ = $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        _ = (long)0
                    }
                    else
                    {
                        _ = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestCompoundAssignment() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    int a = 0
                    a += $$b ? 1 : 2
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    int a = 0
                    if (b)
                    {
                        a += 1
                    }
                    else
                    {
                        a += 2
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    object a = $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    if (b)
                    {
                        a = (long)0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement1_OnDeclaration() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    $$object a = b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    object a
                    if (b)
                    {
                        a = (long)0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement_WithVar() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                void M(bool b)
                {
                    var a = $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                void M(bool b)
                {
                    long a
                    if (b)
                    {
                        a = 0
                    }
                    else
                    {
                        a = 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ObjectReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                object M(bool b)
                {
                    return $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                object M(bool b)
                {
                    if (b)
                    {
                        return (long)0
                    }
                    else
                    {
                        return 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ObjectReturn_OnReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                object M(bool b)
                {
                    $$return b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                object M(bool b)
                {
                    if (b)
                    {
                        return (long)0
                    }
                    else
                    {
                        return 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ActualTypeReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            class C
            {
                long M(bool b)
                {
                    return $$b ? 0 : 1L
                }
            }
            ",
                "
            class C
            {
                long M(bool b)
                {
                    if (b)
                    {
                        return 0
                    }
                    else
                    {
                        return 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SimpleInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    Console.WriteLine($$b ? 0 : 1L)
                }
            }
            ",
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        Console.WriteLine((long)0)
                    }
                    else
                    {
                        Console.WriteLine(1L)
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SimpleInvocationArgument_OnStatement() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    $$Console.WriteLine(b ? 0 : 1L)
                }
            }
            ",
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        Console.WriteLine((long)0)
                    }
                    else
                    {
                        Console.WriteLine(1L)
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SecondInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    Console.WriteLine(b ? "" : "", $$b ? 0 : 1L)
                }
            }
            ",
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        Console.WriteLine(b ? "" : "", (long)0)
                    }
                    else
                    {
                        Console.WriteLine(b ? "" : "", 1L)
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_NestedInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                bool M(bool b)
                {
                    M(M(M($$b ? true : false)))
                    return default
                }
            }
            ",
                "
            imports System
            class C
            {
                bool M(bool b)
                {
                    if (b)
                    {
                        M(M(M(true)))
                    }
                    else
                    {
                        M(M(M(false)))
                    }
                    return default
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestAwaitExpression1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            imports System.Threading.Tasks
            class C
            {
                async void M(bool b, Task x, Task y)
                {
                    await ($$b ? x : y)
                }
            }
            ",
                "
            imports System
            imports System.Threading.Tasks
            class C
            {
                async void M(bool b, Task x, Task y)
                {
                    if (b)
                    {
                        await (x)
                    }
                    else
                    {
                        await (y)
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestAwaitExpression_OnAwait() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            imports System.Threading.Tasks
            class C
            {
                async void M(bool b, Task x, Task y)
                {
                    $$await (b ? x : y)
                }
            }
            ",
                "
            imports System
            imports System.Threading.Tasks
            class C
            {
                async void M(bool b, Task x, Task y)
                {
                    if (b)
                    {
                        await (x)
                    }
                    else
                    {
                        await (y)
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestThrowStatement1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    throw $$b ? new Exception(""x"") : new Exception(""y"")
                }
            }
            ",
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        throw new Exception(""x"")
                    }
                    else
                    {
                        throw new Exception(""y"")
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestThrowStatement_OnThrow1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    $$throw b ? new Exception(""x"") : new Exception(""y"")
                }
            }
            ",
                "
            imports System
            class C
            {
                void M(bool b)
                {
                    if (b)
                    {
                        throw new Exception(""x"")
                    }
                    else
                    {
                        throw new Exception(""y"")
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestYieldReturn1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
            imports System
            imports System.Collections.Generic
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    yield return $$b ? 0 : 1L
                }
            }
            ",
                "
            imports System
            imports System.Collections.Generic
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    if (b)
                    {
                        yield return (long)0
                    }
                    else
                    {
                        yield return 1L
                    }
                }
            }
            ")
        End Function

        <Fact>
        Public Async Function TestYieldReturn_OnYield1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
            "
            imports System
            imports System.Collections.Generic
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    $$yield return b ? 0 : 1L
                }
            }
            ",
            "
            imports System
            imports System.Collections.Generic
            class C
            {
                IEnumerable<object> M(bool b)
                {
                    if (b)
                    {
                        yield return (long)0
                    }
                    else
                    {
                        yield return 1L
                    }
                }
            }
            ")
        End Function
    End Class
End Namespace
