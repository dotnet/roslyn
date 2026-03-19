' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ReassignedVariable
    Public Class VisualBasicReassignedVariableTests
        Inherits AbstractReassignedVariableTests

        Protected Overrides Function CreateWorkspace(markup As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(markup)
        End Function

        <Fact>
        Public Async Function TestNoParameterReassignment() As Task
            Await TestAsync(
"Class C
    Sub M(p As Integer)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParameterReassignment() As Task
            Await TestAsync(
"Class C
    Sub M([|p|] As Integer)
        [|p|] = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParameterReassignmentWhenReadAfter() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M([|p|] As Integer)
        [|p|] = 1
        Console.WriteLine([|p|])
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParameterReassignmentWhenReadBefore() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M([|p|] As Integer)
        Console.WriteLine([|p|])
        [|p|] = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParameterReassignmentWhenReadWithDefaultValue() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M([|p|] As Integer = 1)
        Console.WriteLine([|p|])
        [|p|] = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIndexerWithWriteInGetter() As Task
            Await TestAsync(
"
Imports System
Class C
    Default Property Item([|p|] As Integer) As Integer
        Get
            [|p|] += 1
            Return [|p|]
        End Get
    End Property
End Class")
        End Function

        <Fact>
        Public Async Function TestIndexerWithWriteInSetter() As Task
            Await TestAsync(
"
Imports System
Class C
    Default Property Item([|p|] As Integer) As Integer
        Set(ByVal value As Integer)
            [|p|] += 1
        End Set
    End Property
End Class")
        End Function

        <Fact>
        Public Async Function TestPropertyWithAssignmentToValue() As Task
            Await TestAsync(
"
Imports System
Class C
    Property Goo As Integer
        Set(ByVal [|value|] As Integer)
            [|value|] = [|value|] + 1
        End Set
    End Property
End Class")
        End Function

        <Fact>
        Public Async Function TestEventAddWithAssignmentToValue() As Task
            Await TestAsync(
"
Imports System
Class C
    Custom Event Goo As Action
        AddHandler([|value|] As Action)
            [|value|] = Nothing
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class")
        End Function

        <Fact>
        Public Async Function TestEventRemoveWithAssignmentToValue() As Task
            Await TestAsync(
"
Imports System
Class C
    Custom Event Goo As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler([|value|] As Action)
            [|value|] = Nothing
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class")
        End Function

        <Fact>
        Public Async Function TestEventRaiseWithAssignmentToValue() As Task
            Await TestAsync(
"
Imports System
Class C
    Custom Event Goo As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent([|sender|] As Object, e As EventArgs)
            [|sender|] = Nothing
        End RaiseEvent
    End Event
End Class")
        End Function

        <Fact>
        Public Async Function TestLambdaParameterWithoutReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim a As Action(Of Integer) = Sub(x) Console.WriteLine(x)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLambdaParameterWithReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim a As Action(Of Integer) =
            Sub([|x|])
                [|x|] += 1
                Console.WriteLine([|x|])
            End Sub
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLambdaParameterWithReassignment2() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim a As Action(Of Integer) =
            Sub([|x|]) [|x|] += 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLambdaParameterWithReassignment3() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim a As Action(Of Integer) =
            Sub([|x|] As Integer)
                [|x|] += 1
                Console.WriteLine([|x|])
            End Sub
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLocalWithoutInitializerWithoutReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M(b As Boolean)
        Dim p As Integer
        If b Then p = 1 Else p = 2

        Console.WriteLine(p)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLocalWithoutInitializerWithReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M(b As Boolean)
        Dim [|p|] As Integer
        If b Then [|p|] = 1 Else [|p|] = 2

        [|p|] = 0
        Console.WriteLine([|p|])
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOutParameterCausingReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim [|p|] As Integer = 0
        M2([|p|])
        Console.WriteLine([|p|])
    End Sub

    Sub M2(<System.Runtime.InteropServices.Out> ByRef [|p|] As Integer)
        [|p|] = 0
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestOutParameterWithoutReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim p As Integer
        M2(p)
        Console.WriteLine(p)
    End Sub

    Sub M2(<System.Runtime.InteropServices.Out> ByRef [|p|] As Integer)
        [|p|] = 0
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function AssignmentThroughOutParameterIsNotAssignmentOfTheVariableItself() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M(<System.Runtime.InteropServices.Out> ByRef [|p|] As Integer)
        [|p|] = 0
        [|p|] = 1
        Console.WriteLine([|p|])
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function AssignmentThroughRefParameter() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M(ByRef [|p|] As Integer)
        [|p|] = 0
        [|p|] = 1
        Console.WriteLine([|p|])
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRefParameterCausingPossibleReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim [|p|] As Integer = 0
        M2([|p|])
        Console.WriteLine([|p|])
    End Sub

    Sub M2(ByRef p As Integer)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRefParameterWithoutReassignment() As Task
            Await TestAsync(
"
Imports System
Class C
    Sub M()
        Dim p As Integer
        M2(p)
        Console.WriteLine(p)
    End Sub

    Sub M2(ByRef p As Integer)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRefExtensionMethodCausingPossibleReassignment() As Task
            Await TestAsync(
"
Imports System
Module C
    Sub M()
        Dim [|p|] As Integer = 0
        [|p|].M2()
        Console.WriteLine([|p|])
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Sub M2(ByRef p As Integer)
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestMutatingStructMethod() As Task
            Await TestAsync(
"
Imports System
Structure S
    f As Integer

    Sub M(p As S)
        p.MutatingMethod()
        Console.WriteLine(p)
    End Sub

    Sub MutatingMethod()
        Me = Nothing
    End Sub
End Structure")
        End Function

        <Fact>
        Public Async Function TestDuplicateMethod() As Task
            Await TestAsync(
"Class C
    Sub M([|p|] As Integer)
        [|p|] = 1
    End Sub

    Sub M([|p|] As Integer)
        [|p|] = 1
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestDuplicateParameter() As Task
            Await TestAsync(
"Class C
    Sub M([|p|] As Integer, p As Integer)
        [|p|] = 1
    End Sub
End Class")
        End Function
    End Class
End Namespace
