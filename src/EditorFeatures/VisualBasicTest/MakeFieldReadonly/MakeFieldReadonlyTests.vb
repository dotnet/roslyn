' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeFieldReadonly
    Public Class MakeFieldReadonlyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New MakeFieldReadonlyDiagnosticAnalyzer(),
                New VisualBasicMakeFieldReadonlyCodeFixProvider())
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function FieldIsPublic(accessibility As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    {accessibility} [|_goo|] As Integer
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
    Private ReadOnly [|_goo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsConst() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Const [|_goo|] As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer
End Class",
"Class C
    Private ReadOnly _goo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_PartialClass1() As Task
            Await TestInRegularAndScriptAsync(
"Partial Class C
    Private [|_goo|] As Integer
End Class",
"Partial Class C
    Private ReadOnly _goo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_PartialClass2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer
End Class

Partial Class C
End Class",
"Class C
    Private ReadOnly _goo As Integer
End Class

Partial Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_PartialClass3() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Private [|_goo|] As Integer
End Class
</Document>
                        <Document>
Partial Class C
End Class
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expectedMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Private ReadOnly _goo As Integer
End Class
</Document>
                        <Document>
Partial Class C
End Class
</Document>
                    </Project>
                </Workspace>.ToString()
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssigned_PartialClass1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Partial Class C
    Private [|_goo|] As Integer

    Sub M()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssigned_PartialClass2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer
End Class

Partial Class C
    Sub M()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssigned_PartialClass3() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Private [|_goo|] As Integer
End Class
</Document>
                        <Document>
Partial Class C
    Sub M()
        _goo = 0
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>.ToString()
            Await TestMissingInRegularAndScriptAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Struct() As Task
            Await TestInRegularAndScriptAsync(
"Structure C
    Private [|_goo|] As Integer
End Structure",
"Structure C
    Private ReadOnly _goo As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_Module() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Private [|_goo|] As Integer
End Module",
"Module C
    Private ReadOnly _goo As Integer
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_FieldDeclaredWithDim() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|_goo|] As Integer
End Class",
"Class C
    ReadOnly _goo As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInline() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_AllCanBeReadonly() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0, _bar As Integer = 0
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Private _bar As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_AllCanBeReadonly_MultipleNamesInDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|], _bar As Integer, _fizz As String = """"
End Class",
"Class C
    Private ReadOnly _goo As Integer
    Private _bar As Integer
    Private _fizz As String = """"
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function ThreeFieldsAssignedInline_AllCanBeReadonly_SeparatesAllAndKeepsThemInOrder() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _goo As Integer = 0, [|_bar|] As Integer = 0, _fizz As Integer = 0
End Class",
"Class C
    Private _goo As Integer = 0
    Private ReadOnly _bar As Integer = 0
    Private _fizz As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms01() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|x|] As Integer, y As String
End Class",
"Class C
    Private ReadOnly x As Integer
    Private y As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms02() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x As Integer, [|y|] As String
End Class",
"Class C
    Private x As Integer
    Private ReadOnly y As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms03() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|x|], y As Integer, z, w As String
End Class",
"Class C
    Private ReadOnly x As Integer
    Private y As Integer
    Private z As String
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms04() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, [|y|] As Integer, z, w As String
End Class",
"Class C
    Private x As Integer
    Private ReadOnly y As Integer
    Private z As String
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms05() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, y As Integer, [|z|], w As String
End Class",
"Class C
    Private x As Integer
    Private y As Integer
    Private ReadOnly z As String
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms06() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, y As Integer, z, [|w|] As String
End Class",
"Class C
    Private x As Integer
    Private y As Integer
    Private z As String
    Private ReadOnly w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms07() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|x|], y() As Integer, z(), w As String
End Class",
"Class C
    Private ReadOnly x As Integer
    Private y As Integer()
    Private z As String()
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms08() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, [|y|]() As Integer, z(), w As String
End Class",
"Class C
    Private x As Integer
    Private ReadOnly y As Integer()
    Private z As String()
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms09() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, y() As Integer, [|z|](), w As String
End Class",
"Class C
    Private x As Integer
    Private y As Integer()
    Private ReadOnly z As String()
    Private w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms10() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, y() As Integer, z(), [|w|] As String
End Class",
"Class C
    Private x As Integer
    Private y As Integer()
    Private z As String()
    Private ReadOnly w As String
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms11() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|x|] As New String("""".ToCharArray)
End Class",
"Class C
    ReadOnly x As New String("""".ToCharArray)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms12() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|x|], y As New String("""".ToCharArray)
End Class",
"Class C
    Private ReadOnly x As String = New String("""".ToCharArray)
    Private y As String = New String("""".ToCharArray)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms13() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x, [|y|] As New String("""".ToCharArray)
End Class",
"Class C
    Private x As String = New String("""".ToCharArray)
    Private ReadOnly y As String = New String("""".ToCharArray)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_VBSpecialForms14() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim {|FixAllInDocument:x|}, y As New String("""".ToCharArray)
End Class",
"Class C
    ReadOnly x, y As New String("""".ToCharArray)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_OneAssignedInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _goo As Integer = 0, [|_bar|] As Integer = 0
    Private Sub Goo()
        _goo = 0
    End Sub
End Class",
"Class C
    Private _goo As Integer = 0
    Private ReadOnly _bar As Integer = 0
    Private Sub Goo()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function MultipleFieldsAssignedInline_NoInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer, _bar As Integer = 0
End Class",
"Class C
    Private ReadOnly _goo As Integer
    Private _bar As Integer = 0
End Class")
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <InlineData("")>
        <InlineData("\r\n")>
        <InlineData("\r\n\r\n")>
        Public Async Function MultipleFieldsAssignedInline_LeadingCommentAndWhitespace(leadingTrivia As String) As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    'Comment{leadingTrivia}
    Private _goo As Integer = 0, [|_bar|] As Integer = 0
End Class",
$"Class C
    'Comment{leadingTrivia}
    Private _goo As Integer = 0
    Private ReadOnly _bar As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Public Sub New()
        _goo = 0
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Public Sub New()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMultilineLambdaInCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0

    Public Event SomeEvent()

    Public Sub New()
        AddHandler SomeEvent, Sub()
                                  Me._goo = 0
                              End Sub
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInLambdaInCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0

    Public Event SomeEvent()

    Public Sub New()
        AddHandler SomeEvent, Sub() Me._goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_DifferentInstance() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Public Sub New()
        Dim bar = New C()
        bar._goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_DifferentInstance_QualifiedWithObjectInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Public Sub New()
        Dim bar = New C() With {
            ._goo = 0
        }
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInCtor_QualifiedWithMe() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Public Sub New()
        Me._goo = 0
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Public Sub New()
        Me._goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldReturnedInProperty() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    ReadOnly Property Goo As Integer
        Get
            Return _goo
        End Get
    End Property
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    ReadOnly Property Goo As Integer
        Get
            Return _goo
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <WorkItem(29746, "https://github.com/dotnet/roslyn/issues/29746")>
        Public Async Function FieldReturnedInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_s|] As String
    Sub New(s As String)
        _s = s
    End Sub
    Public Function Method() As String
        Return _s
    End Function
End Class",
"Class C
    Private ReadOnly _s As String
    Sub New(s As String)
        _s = s
    End Sub
    Public Function Method() As String
        Return _s
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <WorkItem(29746, "https://github.com/dotnet/roslyn/issues/29746")>
        Public Async Function FieldReadInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_s|] As String
    Sub New(s As String)
        _s = s
    End Sub
    Public Function Method() As String
        Return _s.ToUpper()
    End Function
End Class",
"Class C
    Private ReadOnly _s As String
    Sub New(s As String)
        _s = s
    End Sub
    Public Function Method() As String
        Return _s.ToUpper()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInProperty() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    ReadOnly Property Goo As Integer
        Get
            Return _goo
        End Get
        Set(value As Integer)
            _goo = value
        End Set
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInNestedTypeConstructor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Class Derived
        Inherits C

        Sub New
            _goo = 0
        End Sub
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInNestedTypeMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Class Derived
        Inherits C

        Sub Method
            _goo = 0
        End Sub
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function VariableAssignedToFieldInMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo
        Dim i = _goo
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Sub Goo
        Dim i = _goo
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldAssignedInMethodWithCompoundOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo
        _goo += 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function AssignedInPartialClass() As Task
            Await TestMissingInRegularAndScriptAsync(
"Partial Class C
    Private [|_goo|] As Integer = 0
End Class

Partial Class C
    Sub Goo()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByRefParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo()
        Bar(_goo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <WorkItem(26262, "https://github.com/dotnet/roslyn/issues/26262")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function CopyPassedAsByRefParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo()
        ' Note: the parens cause a copy, so this is not an actual write into _goo
        Bar((_goo))
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Sub Goo()
        ' Note: the parens cause a copy, so this is not an actual write into _goo
        Bar((_goo))
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByRefParameterInCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub New()
        Bar(_goo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Sub New()
        Bar(_goo)
    End Sub
    Sub Bar(ByRef value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function PassedAsByValParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo|] As Integer = 0
    Sub Goo()
        Bar(_goo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0
    Sub Goo()
        Bar(_goo)
    End Sub
    Sub Bar(ByVal value As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInSharedCtor() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Shared [|_goo|] As Integer = 0
    Shared Sub New()
        _goo = 0
    End Sub
End Class",
"Class C
    Private Shared ReadOnly _goo As Integer = 0
    Shared Sub New()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function SharedFieldAssignedInNonSharedCtor() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private Shared [|_goo|] As Integer = 0
    Sub New()
        _goo = 0
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsMutableStructure() As Task
            Await TestMissingInRegularAndScriptAsync(
"Structure S
    Private _goo As Integer
End Structure
Class C
    Private [|_goo|] As S
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldIsCustomImmutableStructure() As Task
            Await TestInRegularAndScriptAsync(
"Structure S
    Private readonly _goo As Integer
    Private Const _bar As Integer = 0
    Private Shared _fizz As Integer
End Structure
Class C
    Private [|_goo|] As S
End Class",
"Structure S
    Private readonly _goo As Integer
    Private Const _bar As Integer = 0
    Private Shared _fizz As Integer
End Structure
Class C
    Private ReadOnly _goo As S
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FixAll() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private {|FixAllInDocument:_goo|} As Integer = 0, _bar As Integer = 0
    Dim a, b(), c As Integer, x, y As String
    Private _fizz As Integer = 0
End Class",
"Class C
    Private ReadOnly _goo As Integer = 0, _bar As Integer = 0
    ReadOnly a, b(), c As Integer, x, y As String
    Private ReadOnly _fizz As Integer = 0
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FixAll_MultipleFieldsAssignedInline_TwoCanBeReadonly_MultipleNamesInDeclarator() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private _goo, {|FixAllInDocument:_bar|} As Integer, _fizz As String = """"
    Sub Goo()
        _goo = 0
    End Sub
End Class",
"Class C
    Private _goo As Integer
    Private ReadOnly _bar As Integer
    Private ReadOnly _fizz As String = """"
    Sub Goo()
        _goo = 0
    End Sub
End Class")
        End Function

        <WorkItem(26850, "https://github.com/dotnet/roslyn/issues/26850")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        Public Async Function FieldNotAssigned_FieldPartiallyDeclaredWithDim() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim [|_goo|]
End Class",
"Class C
    ReadOnly _goo
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <WorkItem(29373, "https://github.com/dotnet/roslyn/issues/29373")>
        Public Async Function FieldIsReDimOperand() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo()|] As Integer
    Private Sub M()
        Redim _goo(5)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <WorkItem(29373, "https://github.com/dotnet/roslyn/issues/29373")>
        Public Async Function FieldIsReDimPreserveOperand() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Private [|_goo()|] As Integer
    Private Sub M()
        Redim Preserve _goo(5)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)>
        <WorkItem(29373, "https://github.com/dotnet/roslyn/issues/29373")>
        Public Async Function FieldIsRedimIndex() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private [|_goo()|] As Integer
    Private Sub M(a() As Integer)
        Redim a(_goo)
    End Sub
End Class",
"Class C
    Private ReadOnly _goo() As Integer
    Private Sub M(a() As Integer)
        Redim a(_goo)
    End Sub
End Class")
        End Function
    End Class
End Namespace
