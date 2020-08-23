' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InlineMethod

    <Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)>
    Public Class VisualBasicInlineMethodTests
        Private Class TestVerifier
            Inherits VisualBasicCodeRefactoringVerifier(Of VisualbasicInlineMethodRefactoringProvider).Test

            Public Shared Async Function TestInRegularAndScript1Async(initialMarkUp As String, expectedMarkUp As String, Optional keepInlineMethod As Boolean = True) As Task
                Dim test As New TestVerifier() With {.CodeActionIndex = If(keepInlineMethod, 0, 1)}
                test.TestState.Sources.Add(initialMarkUp)
                test.FixedState.Sources.Add(expectedMarkUp)
                Await test.RunAsync().ConfigureAwait(False)
            End Function
        End Class

        <Fact>
        Public Function TestInlineExpressionStatement() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        System.Console.WriteLine(i)
    End Sub

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class")
        End Function

        <Fact>
        Public Function TestInlineExpressionStatementAndRemoveInlinedMethod() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Sub Callee(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        System.Console.WriteLine(i)
    End Sub
End Class", keepInlineMethod:=False)
        End Function

        <Fact>
        Public Function TestInlineReturnExpression() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim x = Me.Ca[||]llee(i)
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim x = i + 10
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineReturnExpressionWithoutVariableDeclaration() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Me.Ca[||]llee(i)
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Dim temp As Integer = i + 10
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineAsArgument() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Integer)
        Callee(Callee(Ca[||]llee(i)))
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class", "
Public Class TestClass
    Public Sub Caller(i As Integer)
        Callee(Callee(i + 10))
    End Sub

    Private Function Callee(i As Integer) As Integer
        Return i + 10
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineWithLiteralArgument() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee(5, True)
    End Sub
    
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class",
"
Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 5 + If(True, 10, 100)
    End Sub
    
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineIdentifierArgument() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller()
        Dim i = 2222
        Dim x = True
        Ca[||]llee(i, x)
    End Sub
    
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class",
"
Public Class TestClass
    Public Sub Caller()
        Dim i = 2222
        Dim x = True
        Dim temp As Integer = i + If(x, 10, 100)
    End Sub
    
    Private Function Callee(i As Integer, b As Boolean) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineDefaltValue() As Task
            Return TestVerifier.TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller()
        Ca[||]llee()
    End Sub
    
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class",
"
Public Class TestClass
    Public Sub Caller()
        Dim temp As Integer = 20 + If(False, 10, 100)
    End Sub
    
    Private Function Callee(Optional i As Integer = 20, Optional b As Boolean = False) As Integer
        Return i + If(b, 10, 100)
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineLambda1() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(i, j)
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
End Class",
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Function()
                   Return i * j
               End Function
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineLambda2() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(i, j)()
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
End Class",
                "
Imports System
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Function()
                   Return i * j
               End Function()
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Func(Of Integer)
        return Function()
                   Return i * j
               End Function
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestIdentifierRename() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim x = Call[||]ee(Cal(i, j), Cal(i, j))
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Integer
        return i * j
    End Function

    Private Function Cal(i As Integer, j As Integer) As Integer
        Return i + j
    End Function
End Class",
                "
Public Class TestClass
    Public Sub Caller(i As Integer, j As Integer)
        Dim i1 As Integer = Cal(i, j)
        Dim j1 As Integer = Cal(i, j)
        Dim x = i1 * j1
    End Sub

    Private Function Callee(i As Integer, j As Integer) as Integer
        return i * j
    End Function

    Private Function Cal(i As Integer, j As Integer) As Integer
        Return i + j
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression1() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Task
        Return Ca[||]llee()
    End Function

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Function Caller() As Task
        Return Task.Delay(100)
    End Function

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression2() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Sub Caller()
        Dim x = Function(i As Integer) As Task
                    Return Cal[||]lee()
                End Function
    End Sub

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Sub Caller()
        Dim x = Function(i As Integer) As Task
                    Return Task.Delay(100)
                End Function
    End Sub

    Private Async Function Callee() As Task
        Await Task.Delay(100)
    End Function
End Class")
        End Function

        <Fact>
        Public Function TestInlineAwaitExpression3() As Task
            Return TestVerifier.TestInRegularAndScript1Async(
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Async Function Caller() As Task
        Await Ca[||]llee()
    End Function

    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
End Class",
                "
Imports System.Threading.Tasks
Public Class TestClass
    Public Async Function Caller() As Task
        Await Task.FromResult(1)
    End Function

    Private Async Function Callee() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
End Class")
        End Function
    End Class
End Namespace
