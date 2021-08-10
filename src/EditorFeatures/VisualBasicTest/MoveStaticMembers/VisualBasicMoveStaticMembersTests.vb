' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.MoveStaticMembers
Imports Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.MoveStaticMembers
    Public Class VisualBasicMoveStaticMembersTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicMoveStaticMembersRefactoringProvider(DirectCast(parameters.fixProviderData, IMoveStaticMembersOptionsService))
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        Private Async Function TestNoRefactoringProvidedAsync(initialMarkup As String) As Task
            Dim parameters = New TestParameters()
            Dim workspace = CreateWorkspaceFromOptions(initialMarkup, parameters)
            Dim tuple = Await GetCodeActionsAsync(workspace, parameters).ConfigureAwait(False)
            Dim actions = tuple.Item1
            ' No actions should be provided
            Assert.Equal(0, actions.Length)
        End Function

        Private Function TestMovementAsync(
            initialMarkup As String,
            expectedResult As String,
            destinationType As String,
            selection As ImmutableArray(Of String),
            Optional destinationName As String = "a.vb",
            Optional parameters As TestParameters = Nothing) As Task
            If IsNothing(parameters) Then
                parameters = New TestParameters()
            End If

            Dim service = New TestMoveStaticMembersService(destinationType, destinationName, selection.AsImmutable())
            Return TestInRegularAndScript1Async(initialMarkup, expectedResult, parameters.WithFixProviderData(service))
        End Function

#Region "Perform Actions From Options"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveField() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveProperty() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Property Test[||]Property As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestProperty")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Property TestProperty As Integer
            Get
                Return 0
            End Get
        End Property
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveEvent() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Event Test[||]Event As EventHandler
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestEvent")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Event TestEvent As EventHandler
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveComplexEvent() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Custom Event Cl[||]ick As EventHandler
            AddHandler(ByVal value As EventHandler)
                Events.AddHandler(""ClickEvent"", value)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                Events.RemoveHandler(""ClickEvent"", value)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                CType(Events(""ClickEvent""), EventHandler).Invoke(sender, e)
            End RaiseEvent
        End Event
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("Click")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Custom Event Click As EventHandler
            AddHandler(ByVal value As EventHandler)
                Events.AddHandler(""ClickEvent"", value)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                Events.RemoveHandler(""ClickEvent"", value)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                CType(Events(""ClickEvent""), EventHandler).Invoke(sender, e)
            End RaiseEvent
        End Event
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveSub() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Sub Test[||]Sub()
        End Sub
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestSub")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Sub TestSub()
        End Sub
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveConst() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Const Test[||]Const As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestConst")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Const TestConst As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestInNestedClass() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Class InnerClass
            Public Shared Function Test[||]Func() As Integer
                Return 0
            End Function
        End Class
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Class InnerClass
        End Class
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestInNestedNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Namespace InnerNs
        Public Class Class1
            Public Shared Function Tes[||]tFunc() As Integer
                Return 0
            End Function
        End Class
    End Namespace
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Namespace InnerNs
        Public Class Class1
        End Class
    End Namespace
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs.InnerNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldNoNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Public Class Class1
    Public Shared Test[||]Field As Integer = 0
End Class
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Public Class Class1
End Class
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Module Class1Helpers
    Public Shared TestField As Integer = 0
End Module
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldNewNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Public Class Class1
    Public Shared Test[||]Field As Integer = 0
End Class
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "TestNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Public Class Class1
End Class
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFieldAddNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "InnerNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs.InnerNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveGenericFunction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func(Of T)(item As T) As T
            Return item
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc(Of T)(item As T) As T
            Return item
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithGenericClass() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1(Of T)
        Public Shared Function Test[||]Func(item As T) As T
            Return item
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1(Of T)
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers(Of T)
        Public Shared Function TestFunc(item As T) As T
            Return item
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionWithFolders() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document Folders=""Folder1"" FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document Folders=""Folder1"" FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document Folders=""Folder1"" FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveMultipleFunctions() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc1", "TestFunc2")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveOneOfMultipleFuncs() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func1() As Integer
            Return 0
        End Function

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc2")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc1() As Integer
            Return 0
        End Function
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers

        Public Shared Function TestFunc2() As Boolean
            Return False
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveOneOfEach() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Test[||]Field As Integer = 0

        Public Shared Property TestProperty As Integer
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
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create(
                "TestField",
                "TestProperty",
                "TestFunc",
                "TestEvent",
                "TestSub")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0

        Public Shared Property TestProperty As Integer
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
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsage() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
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
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageDifferentNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
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
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
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
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageNewNamespace() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
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
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "ExtraNs.Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Imports TestNs.ExtraNs

Namespace TestNs
    Public Class Class1
    End Class

    Public Class Class2
        Public Shared Function TestFunc2() As Integer
            Return Class1Helpers.TestFunc() + 1
        End Function
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs.ExtraNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestMoveFunctionAndRefactorUsageSeparateFile() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function Test[||]Func() As Integer
            Return 0
        End Function
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class2.vb"">
Imports TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1.TestFunc() + 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class2.vb"">
Imports TestNs

Public Class Class2
    Public Shared Function TestFunc2() As Integer
        Return Class1Helpers.TestFunc() + 1
    End Function
End Class
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

#End Region
#Region "SelectionTests"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectBeforeDeclarationKeyword() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        [||]Public Shared TestField As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectWholeFieldDeclaration() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        [|Public Shared TestField As Integer = 0|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInFieldInitializerEquals_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared TestField As Integer =[||] 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInDeclarationKeyword1() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Pub[||]lic Shared TestField As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInDeclarationKeyword2() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shar[||]ed TestField As Integer = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestField")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared TestField As Integer = 0
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodParens() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc([||]) As Integer
            Return 0
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInTypeIdentifierMethodDeclaration() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Inte[||]ger
            Return 0
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Dim newTypeName = "Class1Helpers"
            Dim newFileName = "Class1Helpers.vb"
            Dim selection = ImmutableArray.Create("TestFunc")
            Dim expectedText = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
    End Class
End Namespace
        </Document>
        <Document FilePath=""Class1Helpers.vb"">Namespace TestNs
    Module Class1Helpers
        Public Shared Function TestFunc() As Integer
            Return 0
        End Function
    End Module
End Namespace
</Document>
    </Project>
</Workspace>"
            Await TestMovementAsync(initialMarkup, expectedText, newTypeName, selection, newFileName).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInFieldTypeIdentifier_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared TestField As Int[||]eger = 0
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodBody_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Integer
            Retu[||]rn 0
        End Function
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectInMethodClose_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Function TestFunc() As Integer
            Return 0
        End Func[||]tion
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectNonSharedProperty_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Property Test[||]Property As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function TestSelectPropertyGetter_NoAction() As Task
            Dim initialMarkup = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.vb"">
Namespace TestNs
    Public Class Class1
        Public Shared Property TestProperty As Integer
            Get[||]
                Return 0
            End Get
        End Property
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
"
            Await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(False)
        End Function
#End Region
    End Class
End Namespace
