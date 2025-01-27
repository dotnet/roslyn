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
    <UseExportProvider>
    Public Class ReplaceConditionalWithStatementsTests
        <Fact>
        Public Async Function TestAssignment_ObjectType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a as object
        a = $$If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a as object
        If b Then
            a = CType(0, Long)
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestAssignment_ObjectType_OnAssigment() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a as object 
        $$a = If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a as object
        If b Then
            a = CType(0, Long)
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestAssignment_SameType() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a as long
        a = $$If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a as long
        If b Then
            a = 0
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestCompoundAssignment() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a = 0
        a += $$If(b, 1, 2)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a = 0
        If b Then
            a += 1
        Else
            a += 2
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a as object = $$If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a as object

        If b Then
            a = CType(0, Long)
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement1_OnDeclaration() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        $$dim a as object = If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a as object

        If b Then
            a = CType(0, Long)
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestLocalDeclarationStatement_WithNoAsClause() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    sub M(b as boolean)
        dim a = $$If(b, 0, 1L)
    end sub
end class
            ",
                "
class C
    sub M(b as boolean)
        dim a As Long

        If b Then
            a = 0
        Else
            a = 1L
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ObjectReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    function M(b as boolean) as object
        return $$If(b, 0, 1L)
    end function
end class
            ",
                "
class C
    function M(b as boolean) as object
        If b Then
            return CType(0, Long)
        Else
            return 1L
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ObjectReturn_OnReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    function M(b as boolean) as object
        $$return If(b, 0, 1L)
    end function
end class
            ",
                "
class C
    function M(b as boolean) as object
        If b Then
            return CType(0, Long)
        Else
            return 1L
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestReturnStatement_ActualTypeReturn() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
class C
    function M(b as boolean) as long
        return $$If(b, 0, 1L)
    end function
end class
            ",
                "
class C
    function M(b as boolean) as long
        If b Then
            return 0
        Else
            return 1L
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SimpleInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    sub M(b as boolean)
        Console.WriteLine($$If(b, 0, 1L))
    end sub
end class
            ",
                "
imports System
class C
    sub M(b as boolean)
        If b Then
            Console.WriteLine(CType(0, Long))
        Else
            Console.WriteLine(1L)
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SimpleInvocationArgument_OnStatement() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    sub M(b as boolean)
        $$Console.WriteLine(If(b, 0, 1L))
    end sub
end class
            ",
                "
imports System
class C
    sub M(b as boolean)
        If b Then
            Console.WriteLine(CType(0, Long))
        Else
            Console.WriteLine(1L)
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_SecondInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    sub M(b as boolean)
        Console.WriteLine(If(b, """", """"), $$If(b, 0, 1L))
    end sub
end class
            ",
                "
imports System
class C
    sub M(b as boolean)
        If b Then
            Console.WriteLine(If(b, """", """"), CType(0, Long))
        Else
            Console.WriteLine(If(b, """", """"), 1L)
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestExpressionStatement_NestedInvocationArgument() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    function M(b as boolean) as boolean
        M(M(M($$If(b, true, false))))
        return nothing
    end function
end class
            ",
                "
imports System
class C
    function M(b as boolean) as boolean
        If b Then
            M(M(M(true)))
        Else
            M(M(M(false)))
        End If
        return nothing
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestAwaitExpression1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
imports System.Threading.Tasks
class C
    async function M(b as boolean, x as Task, y as Task) as Task
        await ($$If(b, x, y))
    end function
end class
            ",
                "
imports System
imports System.Threading.Tasks
class C
    async function M(b as boolean, x as Task, y as Task) as Task
        If b Then
            await (x)
        Else
            await (y)
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestAwaitExpression_OnAwait() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
imports System.Threading.Tasks
class C
    async function M(b as boolean, x as Task, y as Task) as Task
        $$await (If(b, x, y))
    end function
end class
            ",
                "
imports System
imports System.Threading.Tasks
class C
    async function M(b as boolean, x as Task, y as Task) as Task
        If b Then
            await (x)
        Else
            await (y)
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestThrowStatement1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    sub M(b as boolean)
        throw $$If(b, new Exception(""x""), new Exception(""y""))
    end sub
end class
            ",
                "
imports System
class C
    sub M(b as boolean)
        If b Then
            throw new Exception(""x"")
        Else
            throw new Exception(""y"")
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestThrowStatement_OnThrow1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
class C
    sub M(b as boolean)
        $$throw If(b, new Exception(""x""), new Exception(""y""))
    end sub
end class
            ",
                "
imports System
class C
    sub M(b as boolean)
        If b Then
            throw new Exception(""x"")
        Else
            throw new Exception(""y"")
        End If
    end sub
end class
            ")
        End Function

        <Fact>
        Public Async Function TestYieldReturn1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
                "
imports System
imports System.Collections.Generic
class C
    iterator function M(b as boolean) as IEnumerable(of object)
        yield $$If(b, 0, 1L)
    end function
end class
            ",
                "
imports System
imports System.Collections.Generic
class C
    iterator function M(b as boolean) as IEnumerable(of object)
        If b Then
            yield CType(0, Long)
        Else
            yield 1L
        End If
    end function
end class
            ")
        End Function

        <Fact>
        Public Async Function TestYieldReturn_OnYield1() As Task
            Await VerifyVB.VerifyRefactoringAsync(
            "
imports System
imports System.Collections.Generic
class C
    iterator function M(b as boolean) as IEnumerable(of object)
        $$yield If(b, 0, 1L)
    end function
end class
            ",
            "
imports System
imports System.Collections.Generic
class C
    iterator function M(b as boolean) as IEnumerable(of object)
        If b Then
            yield CType(0, Long)
        Else
            yield 1L
        End If
    end function
end class
            ")
        End Function
    End Class
End Namespace
