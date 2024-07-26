' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateMethod
    Partial Public Class GenerateMethodTests
        <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public NotInheritable Class GenerateConversionTests
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
                Return (Nothing, New GenerateConversionCodeFixProvider())
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateExplicitConversionGenericClass() As Task
                Await TestInRegularAndScriptAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = CType([|1|], C(Of Integer))
    End Sub
End Class

Class C(Of T)
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = CType(1, C(Of Integer))
    End Sub
End Class

Class C(Of T)
    Public Shared Narrowing Operator CType(v As Integer) As C(Of T)
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateExplicitConversionClass() As Task
                Await TestInRegularAndScriptAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = CType([|1|], C)
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = CType(1, C)
    End Sub
End Class

Class C
    Public Shared Narrowing Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateExplicitConversionAwaitExpression() As Task
                Await TestInRegularAndScriptAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = CType([|Await a|], C)
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = CType(Await a, C)
    End Sub
End Class

Class C
    Public Shared Narrowing Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateImplicitConversionTargetTypeNotInSource() As Task
                Await TestInRegularAndScriptAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = [|dig|]
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = dig
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub

    Public Shared Widening Operator CType(v As Digit) As Double
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateImplicitConversionGenericClass() As Task
                Await TestInRegularAndScriptAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = [|1|]
    End Sub
End Class

Class C(Of T)
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = 1
    End Sub
End Class

Class C(Of T)
    Public Shared Widening Operator CType(v As Integer) As C(Of T)
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateImplicitConversionClass() As Task
                Await TestInRegularAndScriptAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = [|1|]
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = 1
    End Sub
End Class

Class C
    Public Shared Widening Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateImplicitConversionAwaitExpression() As Task
                Await TestInRegularAndScriptAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = [|Await a|]
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = Await a
    End Sub
End Class

Class C
    Public Shared Widening Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            Public Async Function TestGenerateExplicitConversionTargetTypeNotInSource() As Task
                Await TestInRegularAndScriptAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = CType([|dig|], Double)
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = CType(dig, Double)
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub

    Public Shared Narrowing Operator CType(v As Digit) As Double
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf))
            End Function
        End Class
    End Class
End Namespace
