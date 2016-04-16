' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.EncapsulateField

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.EncapsulateField
    Public Class EncapsulateFieldTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New EncapsulateFieldRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulatePrivateFieldAndUpdateReferences() As Task
            Dim text = <File>
Class C
    Private ReadOnly x[||] As Integer

    Public Sub New()
        x = 3
    End Sub

    Sub foo()
        Dim z = x
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private ReadOnly x As Integer

    Public Sub New()
        x = 3
    End Sub

    Public ReadOnly Property X1 As Integer
        Get
            Return x
        End Get
    End Property

    Sub foo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateDimField() As Task
            Dim text = <File>
Class C
    Dim x[||] As Integer

    Sub foo()
        Dim z = x
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Dim x As Integer

    Public Property X1 As Integer
        Get
            Return x
        End Get
        Set(value As Integer)
            x = value
        End Set
    End Property

    Sub foo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateGenericField() As Task
            Dim text = <File>
Class C(Of T)
    Dim x[||] As T

    Sub foo()
        Dim z = x
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C(Of T)
    Dim x As T

    Public Property X1 As T
        Get
            Return x
        End Get
        Set(value As T)
            x = value
        End Set
    End Property

    Sub foo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulatePublicFieldIgnoringReferences() As Task
            Dim text = <File>
Class C
    Public [|x|] As Integer

    Sub foo()
        x = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _x As Integer

    Public Property X As Integer
        Get
            Return _x
        End Get
        Set(value As Integer)
            _x = value
        End Set
    End Property

    Sub foo()
        x = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulatePublicFieldUpdatingReferences() As Task
            Dim text = <File>
Class C
    Public [|x|] As Integer

    Sub foo()
        x = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _x As Integer

    Public Property X As Integer
        Get
            Return _x
        End Get
        Set(value As Integer)
            _x = value
        End Set
    End Property

    Sub foo()
        X = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateMultiplePrivateFieldsWithReferences() As Task
            Dim text = <File>
Class C
    Private [|x, y|] As Integer

    Sub foo()
        x = 3
        y = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private x, y As Integer

    Public Property X1 As Integer
        Get
            Return x
        End Get
        Set(value As Integer)
            x = value
        End Set
    End Property

    Public Property Y1 As Integer
        Get
            Return y
        End Get
        Set(value As Integer)
            y = value
        End Set
    End Property

    Sub foo()
        X1 = 3
        Y1 = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateMultiplePublicFieldsWithReferences() As Task
            Dim text = <File>
Class C
    [|Public x As String
    Public y As Integer|]

    Sub foo()
        x = "foo"
        y = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _x As String
    Private _y As Integer

    Public Property X As String
        Get
            Return _x
        End Get
        Set(value As String)
            _x = value
        End Set
    End Property

    Public Property Y As Integer
        Get
            Return _y
        End Get
        Set(value As Integer)
            _y = value
        End Set
    End Property

    Sub foo()
        x = "foo"
        y = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestNoSetterForConstField() As Task
            Dim text = <File>
Class Program
    Private Const [|foo|] As Integer = 3
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class Program
    Private Const foo As Integer = 3

    Public Shared ReadOnly Property Foo1 As Integer
        Get
            Return foo
        End Get
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateEscapedIdentifier() As Task
            Dim text = <File>
Class C
    Private [|[Class]|] As String
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private [Class] As String

    Public Property Class1 As String
        Get
            Return [Class]
        End Get
        Set(value As String)
            Me.Class = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateFieldNamedValue() As Task
            Dim text = <File>
Class C
    Private [|value|] As Integer = 3
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private value As Integer = 3

    Public Property Value1 As Integer
        Get
            Return value
        End Get
        Set(value As Integer)
            Me.value = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False, index:=0)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateFieldName__() As Task
            Dim text = <File>
Class D
    Public [|__|] As Integer
End Class
</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class D
    Private ___ As Integer

    Public Property __ As Integer
        Get
            Return ___
        End Get
        Set(value As Integer)
            ___ = value
        End Set
    End Property
End Class
</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <WorkItem(694262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694262")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestPreserveTrivia() As Task
            Dim text = <File>
Class AA
    Private name As String : Public [|dsds|] As Integer
End Class
</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class AA
    Private name As String : Private _dsds As Integer

    Public Property Dsds As Integer
        Get
            Return _dsds
        End Get
        Set(value As Integer)
            _dsds = value
        End Set
    End Property
End Class
</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <WorkItem(694241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694241")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestNewPropertyNameIsUnique() As Task
            Dim text = <File>
Class AA
    Private [|name|] As String
    Property Name1 As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class AA
    Private name As String
    Property Name1 As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property

    Public Property Name2 As String
        Get
            Return name
        End Get
        Set(value As String)
            name = value
        End Set
    End Property
End Class
</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <WorkItem(695046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/695046")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestAvailableNotJustOnVariableName() As Task
            Dim text = <File>
Class C
    Private [||] ReadOnly x As Integer
End Class</File>.ConvertTestSourceTag()

            Await TestActionCountAsync(text, 2)
        End Function

        <WorkItem(705898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705898")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestCopyAccessibility() As Task
            Dim text = <File>
Class C
    Protected [|x|] As Integer
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _x As Integer

    Protected Property X As Integer
        Get
            Return _x
        End Get
        Set(value As Integer)
            _x = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <WorkItem(707080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/707080")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestBackingFieldStartsWithUnderscore() As Task
            Dim text = <File>
Public Class Class1
    Public [|Name|] As String
    Sub foo()
        name = ""
    End Sub
End Class
</File>.ConvertTestSourceTag()

            Dim expected = <File>
Public Class Class1
    Private _name As String

    Public Property Name As String
        Get
            Return _name
        End Get
        Set(value As String)
            _name = value
        End Set
    End Property

    Sub foo()
        Name = ""
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestEncapsulateShadowingField() As Task
            Dim text = <File>
Class C
    Protected _foo As Integer

    Public Property Foo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Class D
    Inherits C

    Protected Shadows [|_foo|] As Integer
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Protected _foo As Integer

    Public Property Foo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Class D
    Inherits C

    Private Shadows _foo As Integer

    Protected Property Foo1 As Integer
        Get
            Return _foo
        End Get
        Set(value As Integer)
            _foo = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestAsync(text, expected, compareTokens:=False)
        End Function

        <WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282")>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function TestDoNotEncapsulateOutsideTypeDeclaration() As Task
            Dim globalField = <File>
Dim [|x|] = 1
</File>.ConvertTestSourceTag()
            Await TestMissingAsync(globalField)


            Dim namespaceField = <File>
Namespace N
    Dim [|x|] = 1
End Namespace            
</File>.ConvertTestSourceTag()
            Await TestMissingAsync(namespaceField)

            Dim enumField = <File>
Enum E
     [|x|] = 1
End Enum
</File>.ConvertTestSourceTag()
            Await TestMissingAsync(enumField)

        End Function
    End Class
End Namespace