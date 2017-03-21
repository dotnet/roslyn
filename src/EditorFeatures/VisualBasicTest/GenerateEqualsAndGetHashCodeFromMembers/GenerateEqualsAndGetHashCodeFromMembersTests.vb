' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateConstructorFromMembers
    Public Class GenerateEqualsAndGetHashCodeFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider(
                DirectCast(parameters.fixProviderData, IPickMembersService))
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

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim z = TryCast(obj, Z)
        Return z IsNot Nothing AndAlso
               a = z.a
    End Function

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
index:=1, ignoreTrivia:=False)
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators1() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Class Program
    Public s As String
    [||]
End Class",
"
Imports System.Collections.Generic

Class Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               s = program.s
    End Function

    Public Shared Operator =(program1 As Program, program2 As Program) As Boolean
        Return EqualityComparer(Of Program).Default.Equals(program1, program2)
    End Operator

    Public Shared Operator <>(program1 As Program, program2 As Program) As Boolean
        Return Not program1 = program2
    End Operator
End Class",
chosenSymbols:=Nothing,
optionsCallback:=Sub(options) options(0).Value = True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators3() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Class Program
    Public s As String
    [||]

    Public Shared Operator =(program1 As Program, program2 As Program) As Boolean
        Return True
    End Operator
End Class",
"
Imports System.Collections.Generic

Class Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim program = TryCast(obj, Program)
        Return program IsNot Nothing AndAlso
               s = program.s
    End Function

    Public Shared Operator =(program1 As Program, program2 As Program) As Boolean
        Return True
    End Operator
End Class",
chosenSymbols:=Nothing,
optionsCallback:=Sub(Options) Assert.Empty(Options))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)>
        Public Async Function TestGenerateOperators4() As Task
            Await TestWithPickMembersDialogAsync(
"
Imports System.Collections.Generic

Structure Program
    Public s As String
    [||]
End Structure",
"
Imports System.Collections.Generic

Structure Program
    Public s As String

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is Program) Then
            Return False
        End If

        Dim program = DirectCast(obj, Program)
        Return s = program.s
    End Function

    Public Shared Operator =(program1 As Program, program2 As Program) As Boolean
        Return program1.Equals(program2)
    End Operator

    Public Shared Operator <>(program1 As Program, program2 As Program) As Boolean
        Return Not program1 = program2
    End Operator
End Structure",
chosenSymbols:=Nothing,
optionsCallback:=Sub(options) options(0).Value = True,
ignoreTrivia:=False)
        End Function
    End Class
End Namespace