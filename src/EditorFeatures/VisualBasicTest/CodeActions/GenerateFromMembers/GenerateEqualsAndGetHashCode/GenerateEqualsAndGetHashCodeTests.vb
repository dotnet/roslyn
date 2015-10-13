' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateFromMembers.GenerateEqualsAndGetHashCode
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateFromMembers
    Public Class GenerateEqualsAndGetHashCodeTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New GenerateEqualsAndGetHashCodeCodeRefactoringProvider()
        End Function

        <WorkItem(541991)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Sub TestEqualsOnSingleField()
            Test(
NewLines("Class Z \n [|Private a As Integer|] \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Z \n Private a As Integer \n Public Overrides Function Equals(obj As Object) As Boolean \n Dim z = TryCast(obj, Z) \n Return z IsNot Nothing AndAlso EqualityComparer(Of Integer).Default.Equals(a, z.a) \n End Function \n End Class"),
index:=0)
        End Sub

        <WorkItem(541991)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Sub TestGetHashCodeOnSingleField()
            Test(
NewLines("Class Z \n [|Private a As Integer|] \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Z \n Private a As Integer \n Public Overrides Function GetHashCode() As Integer \n Return EqualityComparer(Of Integer).Default.GetHashCode(a) \n End Function \n End Class"),
index:=1)
        End Sub

        <WorkItem(541991)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Sub TestBothOnSingleField()
            Test(
NewLines("Class Z \n [|Private a As Integer|] \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Z \n Private a As Integer \n Public Overrides Function Equals(obj As Object) As Boolean \n Dim z = TryCast(obj, Z) \n Return z IsNot Nothing AndAlso EqualityComparer(Of Integer).Default.Equals(a, z.a) \n End Function \n Public Overrides Function GetHashCode() As Integer \n Return EqualityComparer(Of Integer).Default.GetHashCode(a) \n End Function \n End Class"),
index:=2)
        End Sub

        <WorkItem(545205)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Sub TestTypeWithNumberInName()
            Test(
NewLines("Partial Class c1(Of V As {New}, U) \n [|Dim x As New V|] \n End Class"),
NewLines("Imports System.Collections.Generic \n Partial Class c1(Of V As {New}, U) \n Dim x As New V \n Public Overrides Function Equals(obj As Object) As Boolean \n Dim c = TryCast(obj, c1(Of V, U)) \n Return c IsNot Nothing AndAlso EqualityComparer(Of V).Default.Equals(x, c.x) \n End Function \n End Class"))
        End Sub
    End Class
End Namespace

