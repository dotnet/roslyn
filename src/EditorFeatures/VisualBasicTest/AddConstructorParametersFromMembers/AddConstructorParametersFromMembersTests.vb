' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddConstructorParametersFromMembers
    Public Class AddConstructorParameterFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New AddConstructorParametersFromMembersCodeRefactoringProvider()
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAdd1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class")
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddOptional1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, Optional s As String = Nothing)
        Me.i = i
        Me.s = s
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddToConstructorWithMostMatchingParameters1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, b As Boolean)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class")
        End Function

        <WorkItem(530592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddOptionalToConstructorWithMostMatchingParameters1() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, Optional b As Boolean = Nothing)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestAddParamtersToConstructorBySelectOneMember() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    [|Private k As Integer|]
    Private j As Integer
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class",
"Class Program
    Private i As Integer
    Private k As Integer
    Private j As Integer
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class")
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestParametersAreStillRightIfMembersAreOutOfOrder() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class",
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class")
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestNormalProperty() As Task
            Await TestInRegularAndScriptAsync(
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer)
    End Sub
End Class",
"
Class Program
    Private i As Integer
    Property Hello As Integer = 1
    Public Sub New(i As Integer, hello As Integer)
        Me.Hello = hello
    End Sub
End Class"
            )
        End Function

        <WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
        Public Async Function TestMissingIfFieldsAndPropertyAlreadyExists() As Task
            Await TestMissingAsync(
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer, hello As Integer)
    End Sub
End Class")
        End Function
    End Class
End Namespace
