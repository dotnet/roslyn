' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.GenerateFromMembers.GenerateConstructorFromMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateConstructorFromMembers
    Public Class GenerateConstructorFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New GenerateConstructorFromMembersCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSingleField() As Task
            Await TestAsync(
"Class Program
    [|Private i As Integer|]
End Class",
"Class Program
    Private i As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields() As Task
            Await TestAsync(
"Class Program
    [|Private i As Integer
    Private b As String|]
End Class",
"Class Program
    Private i As Integer
    Private b As String
    Public Sub New(i As Integer, b As String)
        Me.i = i
        Me.b = b
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSecondField() As Task
            Await TestAsync(
"Class Program
    Private i As Integer
    [|Private b As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private b As String
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(b As String)
        Me.b = b
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestFieldAssigningConstructor() As Task
            Await TestAsync(
"Class Program
    [|Private i As Integer
    Private b As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private b As String
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, b As String)
        Me.i = i
        Me.b = b
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMissingWithExistingConstructor() As Task
            Await TestMissingAsync(
"Class Program
    [|Private i As Integer
    Private b As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, b As String)
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestStruct() As Task
            Await TestAsync(
"Structure S
    [|Private i As Integer|]
End Structure",
"Structure S
    Private i As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Structure",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestGenericType() As Task
            Await TestAsync(
"Class Program(Of T)
    [|Private i As Integer|]
End Class",
"Class Program(Of T)
    Private i As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
index:=0)
        End Function

        <WorkItem(541995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541995")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSimpleDelegatingConstructor() As Task
            Await TestAsync(
"Class Program
    [|Private i As Integer
    Private b As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private b As String
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, b As String)
        Me.New(i)
        Me.b = b
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(542008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542008")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestGenerateFromNormalProperties() As Task
            Await TestAsync(
"Class Z
    [|Public Property A As Integer
    Public Property B As String|]
End Class",
"Class Z
    Public Sub New(a As Integer, b As String)
        Me.A = a
        Me.B = b
    End Sub
    Public Property A As Integer
    Public Property B As String
End Class",
index:=0)
        End Function
    End Class
End Namespace
