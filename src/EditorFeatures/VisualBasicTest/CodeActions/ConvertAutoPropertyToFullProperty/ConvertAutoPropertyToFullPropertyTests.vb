' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertAutoPropertyToFullProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ValidateFormatString
    <Trait(Traits.Feature, Traits.Features.ConvertAutoPropertyToFullProperty)>
    Public Class ConvertAutoPropertyToFullPropertyTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertAutoPropertyToFullPropertyCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function SimpleTest() As Task
            Dim initial = "
Class C
    Public Property T[||]est1 As Integer
End Class"
            Dim expected = "
Class C
    Private _Test1 As Integer

    Public Property Test1 As Integer
        Get
            Return _Test1
        End Get
        Set
            _Test1 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithInitializer() As Task
            Dim initial = "
Class C
    Public Property T[||]est2 As Integer = 4
End Class"
            Dim expected = "
Class C
    Private _Test2 As Integer = 4

    Public Property Test2 As Integer
        Get
            Return _Test2
        End Get
        Set
            _Test2 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithReadonly() As Task
            Dim initial = "
Class C
    Public ReadOnly Property T[||]est5 As String
End Class"
            Dim expected = "
Class C
    Private ReadOnly _Test5 As String

    Public ReadOnly Property Test5 As String
        Get
            Return _Test5
        End Get
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithReadonlyAndInitializer() As Task
            Dim initial = "
Class C
    Public ReadOnly Property Tes[||]t4 As String = ""Initial Value""
End Class"
            Dim expected = "
Class C
    Private ReadOnly _Test4 As String = ""Initial Value""

    Public ReadOnly Property Test4 As String
        Get
            Return _Test4
        End Get
    End Property
End Class"

            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function PrivateProperty() As Task
            Dim initial = "
Class C
    Private Property Tes[||]t4 As String
End Class"
            Dim expected = "
Class C
    Private _Test4 As String

    Private Property Test4 As String
        Get
            Return _Test4
        End Get
        Set
            _Test4 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithComments() As Task
            Dim initial = "
Class C
    '' Comment before
    Public Property Test1 As In[||]teger ''Comment during
    '' Comment after

End Class"
            Dim expected = "
Class C
    Private _Test1 As Integer
    '' Comment before
    Public Property Test1 As Integer ''Comment during
        Get
            Return _Test1
        End Get
        Set
            _Test1 = Value
        End Set
    End Property
    '' Comment after

End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function SharedProperty() As Task
            Dim initial = "
Class C
    Public Sha[||]red Property Test1 As Double
End Class"

            Dim expected = "
Class C
    Private Shared _Test1 As Double

    Public Shared Property Test1 As Double
        Get
            Return _Test1
        End Get
        Set
            _Test1 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithOverridable() As Task
            Dim initial = "
Class C
    Public Overridable Proper[||]ty Test4 As Decimal
End Class"

            Dim expected = "
Class C
    Private _Test4 As Decimal

    Public Overridable Property Test4 As Decimal
        Get
            Return _Test4
        End Get
        Set
            _Test4 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WithMustOverride() As Task
            Await TestMissingAsync("
Class C
    Public MustOverride Property Tes[||]t4 As String
End Class")
        End Function

        <Fact>
        Public Async Function CursorBeforeInitializer() As Task
            Dim initial = "
Class C
    Public Property Test1 As Integer [||]= 4
End Class"
            Dim expected = "
Class C
    Private _Test1 As Integer = 4

    Public Property Test1 As Integer
        Get
            Return _Test1
        End Get
        Set
            _Test1 = Value
        End Set
    End Property
End Class"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function CursorOnInitializer() As Task
            Await TestMissingAsync("
Class C
    Public Property Test2 As Integer = [||]4
End Class")
        End Function

        <Fact>
        Public Async Function InInterface() As Task
            Await TestMissingAsync("
Interface I
    Public Property Tes[||]t2 As Integer
End Interface")
        End Function

        <Fact>
        Public Async Function InvalidLocation() As Task
            Await TestMissingAsync("
Namespace NS
    Public Property Tes[||]t2 As Integer
End Namespace")

            Await TestMissingAsync("Public Property Tes[||]t2 As Integer")
        End Function
    End Class
End Namespace
