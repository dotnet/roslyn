' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.MoveStaticMembers
Imports Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers.VisualBasicMoveStaticMembersRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.MoveStaticMembers
    <UseExportProvider>
    Public Class VisualBasicMoveStaticMembersTests

        Private Shared ReadOnly s_testServices As TestComposition = FeaturesTestCompositions.Features.AddParts(GetType(TestMoveStaticMembersService))

#Region "Perform Actions From Options"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveField() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveProperty() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared ReadOnly Property Test[||]Property As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestProperty")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared ReadOnly Property TestProperty As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveEvent() As Task
            Dim initialMarkup = "
Imports System

Namespace TestNs
    Public Class Class1
        Public Shared Event Test[||]Event As EventHandler
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestEvent")
            Dim expectedText1 = "
Imports System

Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Imports System

Namespace TestNs
    Class Class1Helpers
        Public Shared Event TestEvent As EventHandler
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveComplexEvent() As Task
            Dim initialMarkup = "
Imports System

Namespace TestNs
    Public Class Class1
        Public Shared Custom Event Cl[||]ick As EventHandler
            AddHandler(ByVal value As EventHandler)
                Console.WriteLine(value.ToString())
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                Console.WriteLine(value.ToString())
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                Console.WriteLine(sender.ToString())
            End RaiseEvent
        End Event
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("Click")
            Dim expectedText1 = "
Imports System

Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Imports System

Namespace TestNs
    Class Class1Helpers
        Public Shared Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
                Console.WriteLine(value.ToString())
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                Console.WriteLine(value.ToString())
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                Console.WriteLine(sender.ToString())
            End RaiseEvent
        End Event
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveSub() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Sub Test[||]Sub()
        End Sub
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestSub")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Sub TestSub()
        End Sub
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveConst() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Const Test[||]Const As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestConst")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Const TestConst As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveNothing() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray(Of String).Empty
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithTrivia() As Task
            Dim initialMarkup = "
Namespace TestNs
    ' Comment we don't want to move
    Public Class Class1
        'Comment we want to move
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    ' Comment we don't want to move
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        'Comment we want to move
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestInNestedClass() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Class InnerClass
            Public Shared Function Test[||]Func() As Integer
                Return 0
            End Function
        End Class
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
        Public Class InnerClass
        End Class
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestInNestedNamespace() As Task
            Dim initialMarkup = "
Namespace TestNs
    Namespace InnerNs
        Public Class Class1
            Public Shared Function Tes[||]tFunc() As Integer
                Return 0
            End Function
        End Class
    End Namespace
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Namespace InnerNs
        Public Class Class1
        End Class
    End Namespace
End Namespace"
            Dim expectedText2 = "Namespace TestNs.InnerNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldNoNamespace() As Task
            Dim initialMarkup = "
Public Class Class1
    Public Shared Test[||]Field As Integer = 0
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Public Class Class1
End Class"
            Dim expectedText2 = "Class Class1Helpers
    Public Shared TestField As Integer = 0
End Class
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldNewNamespace() As Task
            Dim initialMarkup = "
Public Class Class1
    Public Shared Test[||]Field As Integer = 0
End Class"
            Dim newTypeName = "TestNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Public Class Class1
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldAddNamespace() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "InnerNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs.InnerNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveGenericFunction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func(Of T)(item As T) As T
            Return item
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc(Of T)(item As T) As T
            Return item
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithGenericClass() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1(Of T)
        Public Shared Function Test[||]Func(item As T) As T
            Return item
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1(Of T)
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers(Of T)
        Public Shared Function TestFunc(item As T) As T
            Return item
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithFolders() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveMultipleFunctions() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc1", "TestFunc2")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveOneOfMultipleFuncs() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc2")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc1() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveOneOfEach() As Task
            Dim initialMarkup = "
Imports System

Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0

        Public Shared ReadOnly Property TestProperty As Integer
            Get
                Return 0
            End Get
        End Property

        Public Shared Event TestEvent As EventHandler

        Public Shared Function TestFunc() As Integer
            Return 0
        End Function

        Public Shared Sub TestSub()
        End Sub
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create(
                "TestField",
                "TestProperty",
                "TestFunc",
                "TestEvent",
                "TestSub")
            Dim expectedText1 = "
Imports System

Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Imports System

Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0

        Public Shared ReadOnly Property TestProperty As Integer
            Get
                Return 0
            End Get
        End Property

        Public Shared Event TestEvent As EventHandler

        Public Shared Sub TestSub()
        End Sub

        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageWithTrivia() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            ' Keep this comment and these random spaces
            Return Class1. TestFunc( ) +  1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            ' Keep this comment and these random spaces
            Return Class1Helpers. TestFunc( ) +  1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorSourceUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Integer
            Return TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldAndRefactorSourceUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0

        Public Shared Function TestFunc2() As Integer
            Return TestField + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestField + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMovePropertyAndRefactorSourceUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Private Shared _testProperty As Integer

        Public Shared Property Test[||]Property As Integer
            Get
                Return _testProperty
            End Get
            Set
                _testProperty = value
            End Set
        End Property

        Public Shared Function TestFunc2() As Integer
            Return TestProperty + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("_testProperty", "TestProperty")
            Dim expectedText1 = "
Imports TestNs.ExtraNs

Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestProperty + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs.ExtraNs
    Class Class1Helpers
        Private Shared _testProperty As Integer

        Public Shared Property TestProperty As Integer
            Get
                Return _testProperty
            End Get
            Set
                _testProperty = value
            End Set
        End Property
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveGenericFunctionAndRefactorImpliedUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func(Of T)(item As T) As T
            Return item
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1.TestFunc(5)
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1Helpers.TestFunc(5)
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc(Of T)(item As T) As T
            Return item
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveGenericFunctionAndRefactorUsage() As Task
            Dim initialMarkup = "Imports System

Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func(Of T)() As Type
            Return GetType(T)
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Type
            Return Class1.TestFunc(Of Integer)()
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "Imports System

Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Type
            Return Class1Helpers.TestFunc(Of Integer)()
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Imports System

Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc(Of T)() As Type
            Return GetType(T)
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionFromGenericClassAndRefactorUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1(Of T As New)
        Public Shared Function Test[||]Func() As T
            Return New T()
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1(Of Integer).TestFunc()
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1(Of T As New)
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1Helpers(Of Integer).TestFunc()
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers(Of T As New)
        Public Shared Function TestFunc() As T
            Return New T()
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionFromGenericClassAndRefactorPartialTypeArgUsage() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1(Of T1 As New, T2, T3)
        Public Shared Function Test[||]Func() As T1
            Return New T1()
        End Function

        Public Shared Function Foo(item As T2) As T2
            Return item
        End Function

        Public Shared Function Bar(item As T3) As T3
            Return item
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1(Of Integer, String, Double).TestFunc()
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1(Of T1 As New, T2, T3)
        Public Shared Function Foo(item As T2) As T2
            Return item
        End Function

        Public Shared Function Bar(item As T3) As T3
            Return item
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2 As Integer
            Return Class1Helpers(Of Integer).TestFunc()
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers(Of T1 As New)
        Public Shared Function TestFunc() As T1
            Return New T1()
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageDifferentNamespace() As Task
            Dim initialMarkup = "
Imports TestNs

Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace

Namespace TestNs2
    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Imports TestNs

Namespace TestNs
    Public Class Class1
    End Class
End Namespace

Namespace TestNs2
    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageNewNamespace() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Imports TestNs.ExtraNs

Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs.ExtraNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageSeparateFile() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1.TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorClassAlias() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports C1 = TestNs.Class1

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return C1.TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs
Imports C1 = TestNs.Class1

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorConflictingName() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports TestNs

Public Class Class2
    Public Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 1
        End Function
    End Class

    Public Shared Function TestFunc2() As Integer
        Return Class1.TestFunc() + Class1Helpers.TestFunc()
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs

Public Class Class2
    Public Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 1
        End Function
    End Class

    Public Shared Function TestFunc2() As Integer
        Return TestNs.Class1Helpers.TestFunc() + Class1Helpers.TestFunc()
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))
            ' In this case, the parse gives a MemberAccessExpression for 
            ' "TestNs.Class1Helpers.TestFunc", but we give a QualifiedName
            ' We can just ensure that the text output is the same here
            test.CodeActionValidationMode = Testing.CodeActionValidationMode.None

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorQualifiedName() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return TestNs.Class1.TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorClassImports() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports TestNs.Class1

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs
Imports TestNs.Class1

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorNamespaceAlias() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports C1 = TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return C1.Class1.TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs
Imports C1 = TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorNamespaceAliasNewNamespace() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports C1 = TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return C1.Class1.TestFunc() + 1
    End Function
End Class"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports TestNs.ExtraNs
Imports C1 = TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs.ExtraNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))

            Await test.RunAsync().ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveExtensionFunction() As Task
            Dim initialMarkup = "
Imports System.Runtime.CompilerServices

Namespace TestNs
    Public Module Class1
        <Extension>
        Public Function Test[||]Func(other As Other) As Integer
            Return other.OtherInt + 2
        End Function
    End Module

    Public Class Class2
        Public Function GetOtherInt() As Integer
            Dim other = New Other()
            Return other.TestFunc()
        End Function
    End Class

    Public Class Other
        Public OtherInt As Integer

        Public Sub New()
            OtherInt = 5
        End Sub
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Imports System.Runtime.CompilerServices

Namespace TestNs
    Public Module Class1
    End Module

    Public Class Class2
        Public Function GetOtherInt() As Integer
            Dim other = New Other()
            Return other.TestFunc()
        End Function
    End Class

    Public Class Other
        Public OtherInt As Integer

        Public Sub New()
            OtherInt = 5
        End Sub
    End Class
End Namespace"
            Dim expectedText2 = "Imports System.Runtime.CompilerServices

Namespace TestNs
    Module Class1Helpers
        <Extension>
        Public Function TestFunc(other As Other) As Integer
            Return other.OtherInt + 2
        End Function
    End Module
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveExtensionFunctionAddImports() As Task
            Dim initialMarkup = "
Imports System.Runtime.CompilerServices
Imports TestNs1
Imports TestNs2

Namespace TestNs1
    Public Module Class1
        <Extension>
        Public Function Test[||]Func(other As Other) As Integer
            Return other.OtherInt + 2
        End Function
    End Module
End Namespace

Namespace TestNs2
    Public Class Class2
        Public Function GetOtherInt() As Integer
            Dim other = New Other()
            Return other.TestFunc()
        End Function
    End Class

    Public Class Other
        Public OtherInt As Integer

        Public Sub New()
            OtherInt = 5
        End Sub
    End Class
End Namespace"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Imports System.Runtime.CompilerServices
Imports TestNs1
Imports TestNs1.ExtraNs
Imports TestNs2

Namespace TestNs1
    Public Module Class1
    End Module
End Namespace

Namespace TestNs2
    Public Class Class2
        Public Function GetOtherInt() As Integer
            Dim other = New Other()
            Return other.TestFunc()
        End Function
    End Class

    Public Class Other
        Public OtherInt As Integer

        Public Sub New()
            OtherInt = 5
        End Sub
    End Class
End Namespace"
            Dim expectedText2 = "Imports System.Runtime.CompilerServices
Imports TestNs2

Namespace TestNs1.ExtraNs
    Module Class1Helpers
        <Extension>
        Public Function TestFunc(other As Other) As Integer
            Return other.OtherInt + 2
        End Function
    End Module
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionInModule() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Module Class1
        Public Function Test[||]Func() As Integer
            Return 0
        End Function
    End Module
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Module Class1
    End Module
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Module Class1Helpers
        Public Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionRetainFileBanner() As Task
            Dim initialMarkup = "' Here is an example of a license or something
' That we want to keep/copy over

Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "' Here is an example of a license or something
' That we want to keep/copy over

Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "' Here is an example of a license or something
' That we want to keep/copy over

Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithRootNamespace() As Task
            Dim initialMarkup = "
Public Class Class1
    Public Shared Function Test[||]Func() As Integer
        Return 0
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Public Class Class1
End Class"
            ' if we cut out the root namespace, the returned namespace should still be the same
            Dim expectedText2 = "Class Class1Helpers
    Public Shared Function TestFunc() As Integer
        Return 0
    End Function
End Class
"

            Dim test = New Test(newTypeName, selection, newFileName) With {.TestCode = initialMarkup}
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add((newFileName, expectedText2))
            test.SolutionTransforms.Add(
                Function(solution, projectId)
                    Dim project = solution.GetProject(projectId)
                    Dim compilationOptions = DirectCast(project.CompilationOptions, VisualBasicCompilationOptions)
                    Return project.WithCompilationOptions(compilationOptions.WithRootNamespace("RootNs")).Solution
                End Function)
            Await test.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithMultipleRootNamespaces() As Task
            Dim initialMarkup = "
Public Class Class1
    Public Shared Function Test[||]Func() As Integer
        Return 0
    End Function
End Class"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Public Class Class1
End Class"
            ' if we cut out the root namespace, the returned namespace should still be the same
            Dim expectedText2 = "Class Class1Helpers
    Public Shared Function TestFunc() As Integer
        Return 0
    End Function
End Class
"

            Dim test = New Test(newTypeName, selection, newFileName) With {.TestCode = initialMarkup}
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add((newFileName, expectedText2))
            test.SolutionTransforms.Add(
                Function(solution, projectId)
                    Dim project = solution.GetProject(projectId)
                    Dim compilationOptions = DirectCast(project.CompilationOptions, VisualBasicCompilationOptions)
                    Return project.WithCompilationOptions(compilationOptions.WithRootNamespace("RootNs.TestNs")).Solution
                End Function)
            Await test.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithRootAndNestedNamespace() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            ' if we cut out the root namespace, the returned namespace should still be the same
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName) With {.TestCode = initialMarkup}
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add((newFileName, expectedText2))
            test.SolutionTransforms.Add(
                Function(solution, projectId)
                    Dim project = solution.GetProject(projectId)
                    Dim compilationOptions = DirectCast(project.CompilationOptions, VisualBasicCompilationOptions)
                    Return project.WithCompilationOptions(compilationOptions.WithRootNamespace("RootNs")).Solution
                End Function)
            Await test.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithRootNamespaceRefactorReferences() As Task
            Dim initialMarkup1 = "
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim initialMarkup2 = "
Imports RootNs.TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1.TestFunc()
    End Function
End Class"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText3 = "
Imports RootNs.TestNs
Imports RootNs.TestNs.ExtraNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc()
    End Function
End Class"
            Dim expectedText2 = "Namespace TestNs.ExtraNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Dim test = New Test(newTypeName, selection, newFileName)
            test.TestState.Sources.Add(initialMarkup1)
            test.TestState.Sources.Add(initialMarkup2)
            test.FixedState.Sources.Add(expectedText1)
            test.FixedState.Sources.Add(expectedText3)
            test.FixedState.Sources.Add((newFileName, expectedText2))
            test.SolutionTransforms.Add(
                Function(solution, projectId)
                    Dim project = solution.GetProject(projectId)
                    Dim compilationOptions = DirectCast(project.CompilationOptions, VisualBasicCompilationOptions)
                    Return project.WithCompilationOptions(compilationOptions.WithRootNamespace("RootNs")).Solution
                End Function)
            Await test.RunAsync()
        End Function

#End Region
#Region "SelectionTests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectBeforeDeclarationKeyword() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        [||]Public Shared TestField As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectWholeFieldDeclaration() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        [|Public Shared TestField As Integer = 0|]
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInDeclarationKeyword1() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Pub[||]lic Shared TestField As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInDeclarationKeyword2() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shar[||]ed TestField As Integer = 0
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared TestField As Integer = 0
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodParens() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc([||]) As Integer
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInTypeIdentifierMethodDeclaration() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Inte[||]ger
            Return 0
        End Function
    End Class
End Namespace"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText1 = "
Namespace TestNs
    Public Class Class1
    End Class
End Namespace"
            Dim expectedText2 = "Namespace TestNs
    Class Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Class
End Namespace
"

            Await TestMovementNewFileAsync(initialMarkup, expectedText1, expectedText2, newFileName, selection, newTypeName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInFieldInitializerEquals_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared TestField As Integer =[||] 0
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInFieldTypeIdentifier_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared TestField As Int[||]eger = 0
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodBody_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Integer
            Retu[||]rn 0
        End Function
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodClose_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Integer
            Return 0
        End Func[||]tion
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectNonSharedProperty_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public ReadOnly Property Test[||]Property As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectPropertyGetter_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Public Shared ReadOnly Property TestProperty As Integer
            Get[||]
                Return 0
            End Get
        End Property
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectConstructor1_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        Shared Sub N[||]ew()
        End Sub
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectConstructor2_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        [|Shared Sub New()|]
        End Sub
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectOperator_NoAction() As Task
            Dim initialMarkup = "
Namespace TestNs
    Public Class Class1
        [|Public Shared Operator +(a As Class1, b As Class1) As Class1|]
            Return New Class1()
        End Operator
    End Class
End Namespace"

            Await TestNoRefactoringAsync(initialMarkup)
        End Function

#End Region

        Private Class Test
            Inherits VerifyVB.Test

            Public Sub New(destinationType As String,
                           members As ImmutableArray(Of String),
                           newFileName As String)
                _destinationType = destinationType
                _members = members
                _newFileName = newFileName
            End Sub

            Private ReadOnly _destinationType As String

            Private ReadOnly _members As ImmutableArray(Of String)

            Private ReadOnly _newFileName As String

            Protected Overrides Function CreateWorkspaceImpl() As Workspace
                Dim hostServices = s_testServices.GetHostServices()

                Dim workspace = New AdhocWorkspace(hostServices)
                Dim optionsService = DirectCast(workspace.Services.GetRequiredService(Of IMoveStaticMembersOptionsService)(), TestMoveStaticMembersService)
                optionsService.DestinationType = _destinationType
                optionsService.Filename = _newFileName
                optionsService.SelectedMembers = _members

                Return workspace
            End Function
        End Class

        Private Shared Async Function TestMovementNewFileAsync(initialMarkup As String,
                                                        expectedSource As String,
                                                        expectedNewFile As String,
                                                        newFileName As String,
                                                        selectedMembers As ImmutableArray(Of String),
                                                        newTypeName As String) As Task

            Dim test = New Test(newTypeName, selectedMembers, newFileName) With
            {
                .TestCode = initialMarkup
            }
            test.FixedState.Sources.Add(expectedSource)
            test.FixedState.Sources.Add((newFileName, expectedNewFile))
            Await test.RunAsync()
        End Function

        Private Shared Async Function TestNoRefactoringAsync(initialMarkup As String) As Task
            Await New Test("", ImmutableArray(Of String).Empty, "") With
            {
                .TestCode = initialMarkup,
                .FixedCode = initialMarkup
            }.RunAsync().ConfigureAwait(False)
        End Function
    End Class
End Namespace
