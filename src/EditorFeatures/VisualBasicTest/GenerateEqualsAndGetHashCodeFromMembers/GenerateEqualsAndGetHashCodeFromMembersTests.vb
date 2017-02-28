' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateConstructorFromMembers
    Public Class GenerateEqualsAndGetHashCodeFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider()
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestEqualsOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function
End Class",
ignoreTrivia:=False)
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGetHashCodeOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function GetHashCode() As Integer
        Return -1757793268 + a.GetHashCode()
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(541991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541991")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestBothOnSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Private a As Integer|]
End Class",
"Class Z
    Private a As Integer

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return -1757793268 + a.GetHashCode()
    End Function
End Class",
index:=2, ignoreTrivia:=False)
        End Function

        <WorkItem(545205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545205")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestTypeWithNumberInName() As Task
            Await TestInRegularAndScriptAsync(
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