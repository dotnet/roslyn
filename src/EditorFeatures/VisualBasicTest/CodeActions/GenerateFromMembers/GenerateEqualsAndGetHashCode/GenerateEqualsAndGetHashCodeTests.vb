' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateConstructorFromMembers
    Public Class GenerateEqualsAndGetHashCodeTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New GenerateEqualsAndGetHashCodeCodeRefactoringProvider()
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestEqualsOnSingleField() As Task
            Await TestAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Imports System.Collections.Generic
Class Z
    Private a As Integer
    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso EqualityComparer(Of Integer).Default.Equals(a, z.a)
    End Function
End Class",
index:=0)
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGetHashCodeOnSingleField() As Task
            Await TestAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Imports System.Collections.Generic
Class Z
    Private a As Integer
    Public Overrides Function GetHashCode() As Integer
        Return EqualityComparer(Of Integer).Default.GetHashCode(a)
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestBothOnSingleField() As Task
            Await TestAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Imports System.Collections.Generic
Class Z
    Private a As Integer
    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso EqualityComparer(Of Integer).Default.Equals(a, z.a)
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return EqualityComparer(Of Integer).Default.GetHashCode(a)
    End Function
End Class",
index:=2)
        End Function

        <WorkItem(545205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545205")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestTypeWithNumberInName() As Task
            Await TestAsync(
"Partial Class c1(Of V As {New}, U)
    [|Dim x As New V|]
End Class",
"Imports System.Collections.Generic
Partial Class c1(Of V As {New}, U)
    Dim x As New V
    Public Overrides Function Equals(obj As Object) As Boolean
        Dim c = TryCast(obj, c1(Of V, U))
        Return c IsNot Nothing AndAlso EqualityComparer(Of V).Default.Equals(x, c.x)
    End Function
End Class")
        End Function
    End Class
End Namespace

