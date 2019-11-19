' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateConstructorFromMembers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PickMembers
Imports Microsoft.CodeAnalysis.VisualBasic.GenerateConstructorFromMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateConstructorFromMembers
    Public Class GenerateConstructorFromMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicGenerateConstructorFromMembersCodeRefactoringProvider(DirectCast(parameters.fixProviderData, IPickMembersService))
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
        Public Async Function TestMultipleFields_VerticalSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class Program[|
    Private i As Integer
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
        Public Async Function TestMultipleFields_VerticalSelectionUpToExcludedField() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private a As String[|
    Private i As Integer
    Private b As String|]
End Class",
"Class Program
    Private a As String
    Private i As Integer
    Private b As String

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_VerticalSelectionUpToMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Public Sub Foo
    End Sub[|

    Private i As Integer
    Private b As String|]
End Class",
"Class Program
    Public Sub Foo
    End Sub

    Private i As Integer
    Private b As String

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_VerticalSelectionUpToInherits() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Inherits Object[|

    Private i As Integer
    Private b As String|]
End Class",
"Class Program
    Inherits Object

    Private i As Integer
    Private b As String

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_VerticalSelectionUpToGeneric() As Task
            Await TestInRegularAndScriptAsync(
"Class Program(Of T)[|
    Private i As Integer
    Private b As String|]
End Class",
"Class Program(Of T)
    Private i As Integer
    Private b As String

    Public Sub New(i As Integer, b As String{|Navigation:)|}
        Me.i = i
        Me.b = b
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_SelectionIncludingClassName() As Task
            Await TestMissingAsync(
"Class Progra[|m
    Private i As Integer
    Private b As String|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_SelectionIncludingInherits() As Task

            Await TestMissingAsync(
"Class Program
    Inherits Objec[|t
    Private i As Integer
    Private b As String|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultipleFields_SelectionIncludingGeneric() As Task
            Await TestMissingAsync(
"Class Program(Of T[|)
    Private i As Integer
    Private b As String|]
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
        Public Async Function TestWithDialog1WithNullCheck() As Task
            Dim options = New Dictionary(Of OptionKey, Object)
            options(New OptionKey(GenerateConstructorFromMembersOptions.AddNullChecks, language:=LanguageNames.VisualBasic)) = True

            Dim parameters = New TestParameters()
            parameters = parameters.WithOptions(options)

            Await TestWithPickMembersDialogAsync(
"Class Program
    Private s As String
    [||]
End Class",
"Imports System

Class Program
    Private s As String

    Public Sub New(s As String{|Navigation:)|}
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If

        Me.s = s
    End Sub
End Class", chosenSymbols:={"s"}, parameters:=parameters)
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

        <WorkItem(17643, "https://github.com/dotnet/roslyn/issues/17643")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialogNoBackingField() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public Property F() As Integer
    [||]
End Class",
"
Class Program
    Public Property F() As Integer

    Public Sub New(f As Integer{|Navigation:)|}
        Me.F = f
    End Sub
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialogNoParameterizedProperty() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Public Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    [||]
End Class",
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Public Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Public Sub New(p As Integer{|Navigation:)|}
        Me.P = p
    End Sub
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialogNoIndexer() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Default Public Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    [||]
End Class",
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Default Public Property I(index As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Public Sub New(p As Integer{|Navigation:)|}
        Me.P = p
    End Sub
End Class",
chosenSymbols:=Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestWithDialogSetterOnlyProperty() As Task
            Await TestWithPickMembersDialogAsync(
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Public WriteOnly Property S() As Integer
        Set
        End Set
    End Property
    [||]
End Class",
"
Class Program
    Public Property P() As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Public WriteOnly Property S() As Integer
        Set
        End Set
    End Property

    Public Sub New(p As Integer, s As Integer{|Navigation:)|}
        Me.P = p
        Me.S = s
    End Sub
End Class",
chosenSymbols:=Nothing)
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|i|] As Integer
End Class",
"Class Program
    Private i As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelection2() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|i|]jklm As Integer
End Class",
"Class Program
    Private ijklm As Integer

    Public Sub New(ijklm As Integer{|Navigation:)|}
        Me.ijklm = ijklm
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelection3() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private gh[|i|] As Integer
End Class",
"Class Program
    Private ghi As Integer

    Public Sub New(ghi As Integer{|Navigation:)|}
        Me.ghi = ghi
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelectionBeforeIdentifier() As Task
            Await TestMissingAsync(
"Class Program
    Private[| |]i As Integer
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelectionAfterIdentifier() As Task
            Await TestMissingAsync(
"Class Program
    Private i[| |]As Integer
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelectionIdentifierNotSelected() As Task
            Await TestMissingAsync(
"Class Program
    Private i [|As Integer|]
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestPartialFieldSelectionIdentifierNotSelected2() As Task
            Await TestMissingAsync(
"Class Program
    Private i As Integer = [|3|]
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultiplePartialFieldSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|i As Integer
    Private j|] As Integer
End Class",
"Class Program
    Private i As Integer
    Private j As Integer

    Public Sub New(i As Integer, j As Integer{|Navigation:)|}
        Me.i = i
        Me.j = j
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultiplePartialFieldSelection2() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|i As Integer
    Private |]j As Integer
End Class",
"Class Program
    Private i As Integer
    Private j As Integer

    Public Sub New(i As Integer{|Navigation:)|}
        Me.i = i
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultiplePartialFieldSelection3() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Private [|i|] As Integer = 2, j As Integer = 3
End Class",
"Class Program
    Private i As Integer = 2, j As Integer = 3

    Public Sub New(i As Integer, j As Integer{|Navigation:)|}
        Me.i = i
        Me.j = j
    End Sub
End Class")
        End Function

        <WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)>
        Public Async Function TestMultiplePartialFieldSelection4() As Task
            Await TestMissingAsync(
"Class Program
    Private i As Integer = [|2|], j As Integer = 3
End Class")
        End Function
    End Class
End Namespace
