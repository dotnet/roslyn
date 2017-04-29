' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeFieldReadonly
    Public Class MakeFieldReadonlyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicMakeFieldReadonlyDiagnosticAnalyzer(),
                New VisualBasicMakeFieldReadonlyCodeFixProvider())
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly),
        InlineData("Public"),
        InlineData("Friend"),
        InlineData("Protected"),
        InlineData("Protected Friend")>
        Public Async Function FieldIsPublic(accessibility As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    {accessibility} [|_foo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsEvent() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Event [|SomeEvent|]()
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsReadonly() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private ReadOnly [|_foo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsConst() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Const [|_foo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer
End Class",
"Class C
    Private ReadOnly _foo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Struct() As Task
            Await TestInRegularAndScriptAsync(
"Structure C
    Private [|_foo|] As Integer
End Structure",
"Structure C
    Private ReadOnly _foo As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Module() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Private [|_foo|] As Integer
End Module",
"Module C
    Private ReadOnly _foo As Integer
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_FieldDeclaredWithDim() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|_foo|] As Integer
End Class",
"Class C
    ReadOnly _foo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInline() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_AllCanBeReadonly() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0, _bar As Integer = 0
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Private _bar As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_AllCanBeReadonly_MultipleNamesInDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|], _bar As Integer, _fizz As String = """"
End Class",
"Class C
    Private ReadOnly _foo As Integer
    Private _bar As Integer
    Private _fizz As String = """"
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function ThreeFieldsAssignedInline_AllCanBeReadonly_SeparatesAllAndKeepsThemInOrder() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _foo As Integer = 0, [|_bar|] As Integer = 0, _fizz As Integer = 0
End Class",
"Class C
    Private _foo As Integer = 0
    Private ReadOnly _bar As Integer = 0
    Private _fizz As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_OneAssignedInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _foo As Integer = 0, [|_bar|] As Integer = 0
    Private Sub Foo()
        _foo = 0
    End Sub
End Class",
"Class C
    Private _foo As Integer = 0
    Private ReadOnly _bar As Integer = 0
    Private Sub Foo()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_NoInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer, _bar As Integer = 0
End Class",
"Class C
    Private ReadOnly _foo As Integer
    Private _bar As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Public Sub New()
        _foo = 0
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Public Sub New()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMultilineLambdaInCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0

    Public Event SomeEvent()

    Public Sub New()
        AddHandler SomeEvent, Sub()
                                  Me._foo = 0
                              End Sub
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInLambdaInCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0

    Public Event SomeEvent()

    Public Sub New()
        AddHandler SomeEvent, Sub() Me._foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_DifferentInstance() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Public Sub New()
        Dim bar = New C()
        bar._foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_DifferentInstance_QualifiedWithObjectInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Public Sub New()
        Dim bar = New C() With {
            ._foo = 0
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_QualifiedWithMe() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Public Sub New()
        Me._foo = 0
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Public Sub New()
        Me._foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldReturnedInProperty() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
    End Property
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInProperty() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    ReadOnly Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(value As Integer)
            _foo = value
        End Set
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function VariableAssignedToFieldInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo
        Dim i = _foo
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Sub Foo
        Dim i = _foo
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethodWithCompoundOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo
        _foo += 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function AssignedInPartialClass() As Task
            Await TestMissingInRegularAndScriptAsync(
"Partial Class C
    Private [|_foo|] As Integer = 0
End Class

Partial Class C
    Sub Foo()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByRefParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByRefParameterInCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub New()
        Bar(_foo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Sub New()
        Bar(_foo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByValParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_foo|] As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0
    Sub Foo()
        Bar(_foo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInSharedCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Shared [|_foo|] As Integer = 0
    Shared Sub New()
        _foo = 0
    End Sub
End Class",
"Class C
    Private Shared ReadOnly _foo As Integer = 0
    Shared Sub New()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInNonSharedCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Shared [|_foo|] As Integer = 0
    Sub New()
        _foo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsMutableStructure() As Task
            Await TestMissingInRegularAndScriptAsync(
"Structure S
    Private _foo As Integer
End Structure
Class C
    Private [|_foo|] As S
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsCustomImmutableStructure() As Task
            Await TestInRegularAndScriptAsync(
"Structure S
    Private readonly _foo As Integer
    Private Const _bar As Integer = 0
    Private Shared _fizz As Integer
End Structure
Class C
    Private [|_foo|] As S
End Class",
"Structure S
    Private readonly _foo As Integer
    Private Const _bar As Integer = 0
    Private Shared _fizz As Integer
End Structure
Class C
    Private ReadOnly _foo As S
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FixAll() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private {|FixAllInDocument:_foo|} As Integer = 0, _bar As Integer = 0
    Private _fizz As Integer = 0
End Class",
"Class C
    Private ReadOnly _foo As Integer = 0, _bar As Integer = 0
    Private ReadOnly _fizz As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FixAll_MultipleFieldsAssignedInline_TwoCanBeReadonly_MultipleNamesInDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _foo, {|FixAllInDocument:_bar|} As Integer, _fizz As String = """"
    Sub Foo()
        _foo = 0
    End Sub
End Class",
"Class C
    Private _foo As Integer
    Private ReadOnly _bar As Integer
    Private ReadOnly _fizz As String = """"
    Sub Foo()
        _foo = 0
    End Sub
End Class")
        End Function
    End Class
End Namespace