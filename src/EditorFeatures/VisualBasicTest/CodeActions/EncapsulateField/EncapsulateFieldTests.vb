' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.EncapsulateField
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.EncapsulateField
    <Trait(Traits.Feature, Traits.Features.EncapsulateField)>
    Public Class EncapsulateFieldTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New EncapsulateFieldRefactoringProvider()
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulatePrivateFieldAndUpdateReferences(host As TestHost) As Task
            Dim text = <File>
Class C
    Private ReadOnly x[||] As Integer

    Public Sub New()
        x = 3
    End Sub

    Sub goo()
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

    Sub goo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateDimField(host As TestHost) As Task
            Dim text = <File>
Class C
    Dim x[||] As Integer

    Sub goo()
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

    Sub goo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)

        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateGenericField(host As TestHost) As Task
            Dim text = <File>
Class C(Of T)
    Dim x[||] As T

    Sub goo()
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

    Sub goo()
        Dim z = X1
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)

        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulatePublicFieldIgnoringReferences(host As TestHost) As Task
            Dim text = <File>
Class C
    Public [|x|] As Integer

    Sub goo()
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

    Sub goo()
        x = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, index:=1, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulatePublicFieldUpdatingReferences(host As TestHost) As Task
            Dim text = <File>
Class C
    Public [|x|] As Integer

    Sub goo()
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

    Sub goo()
        X = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateMultiplePrivateFieldsWithReferences(host As TestHost) As Task
            Dim text = <File>
Class C
    Private [|x, y|] As Integer

    Sub goo()
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

    Sub goo()
        X1 = 3
        Y1 = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateMultiplePublicFieldsWithReferences(host As TestHost) As Task
            Dim text = <File>
Class C
    [|Public x As String
    Public y As Integer|]

    Sub goo()
        x = "goo"
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

    Sub goo()
        x = "goo"
        y = 4
    End Sub
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, index:=1, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestNoSetterForConstField(host As TestHost) As Task
            Dim text = <File>
Class Program
    Private Const [|goo|] As Integer = 3
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class Program
    Private Const goo As Integer = 3

    Public Shared ReadOnly Property Goo1 As Integer
        Get
            Return goo
        End Get
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)

        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateEscapedIdentifier(host As TestHost) As Task
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
            [Class] = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)

        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateEscapedIdentifierWithQualifiedAccess(host As TestHost) As Task
            Dim text = <File>
Class C
    Private [|[Class]|] As String
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private [Class] As String

    Public Property Class1 As String
        Get
            Return Me.Class
        End Get
        Set(value As String)
            Me.Class = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(
                text, expected,
                options:=New OptionsCollection(GetLanguage()) From {
                    {CodeStyleOptions2.QualifyFieldAccess, True, NotificationOption2.Error}
                }, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateFieldNamedValue(host As TestHost) As Task
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

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)

        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateFieldName__(host As TestHost) As Task
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

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694262")>
        Public Async Function TestPreserveTrivia(host As TestHost) As Task
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

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/694241")>
        Public Async Function TestNewPropertyNameIsUnique(host As TestHost) As Task
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

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/695046")>
        Public Async Function TestAvailableNotJustOnVariableName(host As TestHost) As Task
            Dim text = <File>
Class C
    Private [||] ReadOnly x As Integer
End Class</File>.ConvertTestSourceTag()

            Await TestActionCountAsync(text, 2, New TestParameters(testHost:=host))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/705898")>
        Public Async Function TestCopyAccessibility(host As TestHost) As Task
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

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/707080")>
        Public Async Function TestBackingFieldStartsWithUnderscore(host As TestHost) As Task
            Dim text = <File>
Public Class Class1
    Public [|Name|] As String
    Sub goo()
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

    Sub goo()
        Name = ""
    End Sub
End Class
</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEncapsulateShadowingField(host As TestHost) As Task
            Dim text = <File>
Class C
    Protected _goo As Integer

    Public Property Goo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Class D
    Inherits C

    Protected Shadows [|_goo|] As Integer
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Protected _goo As Integer

    Public Property Goo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class

Class D
    Inherits C

    Private Shadows _goo As Integer

    Protected Property Goo1 As Integer
        Get
            Return _goo
        End Get
        Set(value As Integer)
            _goo = value
        End Set
    End Property
End Class</File>.ConvertTestSourceTag()

            Await TestInRegularAndScriptAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem(1096007, "https://github.com/dotnet/roslyn/issues/282")>
        Public Async Function TestDoNotEncapsulateOutsideTypeDeclaration(host As TestHost) As Task
            Dim globalField = <File>
Dim [|x|] = 1
</File>.ConvertTestSourceTag()
            Await TestMissingInRegularAndScriptAsync(globalField, New TestParameters(testHost:=host))

            Dim namespaceField = <File>
Namespace N
    Dim [|x|] = 1
End Namespace            
</File>.ConvertTestSourceTag()
            Await TestMissingInRegularAndScriptAsync(namespaceField, New TestParameters(testHost:=host))

            Dim enumField = <File>
Enum E
     [|x|] = 1
End Enum
</File>.ConvertTestSourceTag()
            Await TestMissingInRegularAndScriptAsync(enumField, New TestParameters(testHost:=host))

        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/7090")>
        <Theory, CombinatorialData>
        Public Async Function ApplyCurrentMePrefixStyle(host As TestHost) As Task
            Await TestInRegularAndScriptAsync("
Class C
    Dim [|i|] As Integer
End Class
", "
Class C
    Dim i As Integer

    Public Property I1 As Integer
        Get
            Return Me.i
        End Get
        Set(value As Integer)
            Me.i = value
        End Set
    End Property
End Class
",
options:=New OptionsCollection(GetLanguage()) From {
    {CodeStyleOptions2.QualifyFieldAccess, True, NotificationOption2.Error}
}, testHost:=host)
        End Function
    End Class
End Namespace
