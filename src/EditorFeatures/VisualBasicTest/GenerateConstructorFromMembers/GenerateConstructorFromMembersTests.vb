' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateConstructorFromMembers
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateConstructorFromMembers
    Public Class GenerateConstructorFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateConstructorFromMembersCodeRefactoringProvider(DirectCast(parameters.fixProviderData, IPickMembersService))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSingleField() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer|]
End Class",
"Class Program
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    [|Private i As Integer
    Private b As String|]
End Class",
"Class Program
    Private i As Integer
    Private b As String

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSecondField() As Task
            Await TestInRegularAndScriptAsync(
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

    Public Sub New(b As String{|Navigation:)|}
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestFieldAssigningConstructor() As Task
            Await TestInRegularAndScriptAsync(
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

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMissingWithExistingConstructor() As Task
            Await TestMissingInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
"Structure S
    [|Private i As Integer|]
End Structure",
"Structure S
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestGenericType() As Task
            Await TestInRegularAndScriptAsync(
"Class Program(Of T)
    [|Private i As Integer|]
End Class",
"Class Program(Of T)
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class")
        End Function

        <WorkItem(541995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541995")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestSimpleDelegatingConstructor() As Task
            Await TestInRegularAndScriptAsync(
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

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.New(i)
        Me.b = b
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(542008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542008")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestGenerateFromNormalProperties() As Task
            Await TestInRegularAndScriptAsync(
"Class Z
    [|Public Property A As Integer
    Public Property B As String|]
End Class",
"Class Z
    Public Sub New(a As Integer, b As String{|Navigation:)|}
        Me.A = a
        Me.B = b
    End Sub

    Public Property A As Integer
    Public Property B As String
End Class")
        End Function

        <WorkItem(13944, "https://github.com/dotnet/roslyn/issues/13944")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestGetter_Only_Auto_Props() As Task
            Await TestInRegularAndScriptAsync(
"Class Contribution
  [|ReadOnly Property Title As String
    ReadOnly Property Number As Integer|]
End Class",
"Class Contribution
    Public Sub New(title As String, number As Integer{|Navigation:)|}
        Me.Title = title
        Me.Number = number
    End Sub

    ReadOnly Property Title As String
    ReadOnly Property Number As Integer
End Class")
        End Function

        <WorkItem(13944, "https://github.com/dotnet/roslyn/issues/13944")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestAbstract_Getter_Only_Auto_Props() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Contribution
  [|MustOverride ReadOnly Property Title As String
    ReadOnly Property Number As Integer|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialog1() As Task
            Await TestWithPickMembersDialogAsync(
"Class Program
    Private i As Integer
    [||]
End Class",
"Class Program
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class", chosenSymbols:={"i"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialog2() As Task
            Await TestWithPickMembersDialogAsync(
"Class Program
    Private i As Integer
    [||]
End Class",
"Class Program
    Private i As Integer

    Public Sub New({|Navigation:)|}
    End Sub
End Class", chosenSymbols:={})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialog3() As Task
            Await TestWithPickMembersDialogAsync(
"Class Program
    Private i As Integer
    Private j As String
    [||]
End Class",
"Class Program
    Private i As Integer
    Private j As String

    Public Sub New(j As String, i As Integer{|Navigation:)|}
        Me.j = j
        Me.i = i
    End Sub
End Class", chosenSymbols:={"j", "i"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialog4() As Task
            Await TestWithPickMembersDialogAsync(
"Class [||]Program
    Private i As Integer
End Class",
"Class Program
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class", chosenSymbols:={"i"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMissingOnMember1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    [||]Sub M()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMissingOnMember2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Program
    Private i As Integer
    Sub M()
    End Sub[||]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMissingOnAttributes() As Task
            Await TestMissingInRegularAndScriptAsync(
"<X>[||]
Class Program
    Private i As Integer
    Sub M()
    End Sub
End Class")
        End Function
    End Class
End Namespace
