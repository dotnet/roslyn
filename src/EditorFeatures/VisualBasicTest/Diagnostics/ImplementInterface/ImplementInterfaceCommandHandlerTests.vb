' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports System.Xml
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementInterface
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ImplementInterface
    Public Class ImplementInterfaceCommandHandlerTests

        Private Async Function TestAsync(code As XElement, expectedText As XElement, nextHandler As Action(Of IWpfTextView, TestWorkspace), assertion As Action(Of String, String, IWpfTextView)) As Threading.Tasks.Task
            Using workspace = Await GetWorkspaceAsync(code.NormalizedValue)
                Dim commandHandler = MoveCaretAndCreateCommandHandler(workspace)
                Dim view = workspace.Documents.Single().GetTextView()

                commandHandler.ExecuteCommand(New ReturnKeyCommandArgs(view, view.TextBuffer), Sub() nextHandler(view, workspace))

                Dim text = view.TextBuffer.CurrentSnapshot.AsText().ToString()

                assertion(expectedText.NormalizedValue, text, view)
            End Using
        End Function

        Private Shared Function MoveCaretAndCreateCommandHandler(workspace As TestWorkspace) As ICommandHandler(Of ReturnKeyCommandArgs)
            Dim document = workspace.Documents.Single()
            Dim view = document.GetTextView()
            Dim cursorPosition = document.CursorPosition.Value

            view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, cursorPosition))
            Return New ImplementInterfaceCommandHandler(workspace.GetService(Of IEditorOperationsFactoryService))
        End Function

        Private Function GetWorkspaceAsync(code As String) As Threading.Tasks.Task(Of TestWorkspace)
            Return TestWorkspace.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                        <Document>
                            <%= code.Replace(vbCrLf, vbLf) %>
                        </Document>
                    </Project>
                </Workspace>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function FeatureDoesNothingIfDisabled() As Threading.Tasks.Task
            Using workspace = Await GetWorkspaceAsync("
Imports System

Class Foo
    Implements IFoo$$
End Class
Interface IFoo
    Sub TestSub()
End Interface")

                Dim commandHandler = MoveCaretAndCreateCommandHandler(workspace)

                Dim optionServices = workspace.Services.GetService(Of IOptionService)()
                optionServices.SetOptions(optionServices.GetOptions().WithChangedOption(FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers, LanguageNames.VisualBasic, False))

                Dim nextHandlerCalled = False
                Dim view = workspace.Documents.Single().GetTextView()
                commandHandler.ExecuteCommand(New ReturnKeyCommandArgs(view, view.TextBuffer), Sub() nextHandlerCalled = True)
                Assert.True(nextHandlerCalled, "Next handler wasn't called, which means the feature did run")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInterfaceWithSingleSub() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo$$
End Class
Interface IFoo
    Sub TestSub()
End Interface</text>

            Dim expectedText = <text>   
    Public Sub TestSub() Implements IFoo.TestSub
        Throw New NotImplementedException()
    End Sub</text>

            Await TestAsync(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(544161)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInterfacesWithDuplicateMember() As Task
            Dim code = <text>
Interface IFoo
    Sub Foo()
End Interface
Interface IBar
    Sub Foo()
End Interface
Class Zip
    Implements IFoo, IBar$$
End Class</text>

            Dim expectedText = <text>   
    Public Sub Foo() Implements IFoo.Foo
        Throw New NotImplementedException()
    End Sub

    Private Sub IBar_Foo() Implements IBar.Foo
        Throw New NotImplementedException()
    End Sub</text>

            Await TestAsync(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInterfaceWithManyMembers() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo$$
End Class
Interface IFoo
    Sub TestSub()
    Function TestFunc() As Integer
    Property TestProperty As String
End Interface</text>

            Dim expectedText = <text>   
    Public Property TestProperty As String Implements IFoo.TestProperty
        Get
            Throw New NotImplementedException()
        End Get

        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Sub TestSub() Implements IFoo.TestSub
        Throw New NotImplementedException()
    End Sub

    Public Function TestFunc() As Integer Implements IFoo.TestFunc
        Throw New NotImplementedException()
    End Function</text>

            Await TestAsync(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMultipleInterfaces() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo, IBar$$
End Class
Interface IFoo
    Sub TestSub()
End Interface
Interface IBar
    Function TestFunc() As Integer
End Interface</text>

            Dim expectedText = <text>   
    Public Sub TestSub() Implements IFoo.TestSub
        Throw New NotImplementedException()
    End Sub

    Public Function TestFunc() As Integer Implements IBar.TestFunc
        Throw New NotImplementedException()
    End Function</text>

            Await TestAsync(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestWrongCursorPlacement() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo$$, IBar
End Class
Interface IFoo
    Sub TestSub()
End Interface
Interface IBar
    Function TestFunc() As Integer
End Interface</text>

            Dim expectedText = <text>   
Class Foo
    Implements IFoo
    , IBar
End Class</text>

            Await TestAsync(code,
                 expectedText,
                 Sub(view, workspace)
                     Dim operations = workspace.GetService(Of IEditorOperationsFactoryService)() _
                                               .GetEditorOperations(view)
                     operations.InsertNewLine()
                 End Sub,
                 Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(530553)>
        <WorkItem(544087)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInvocationAfterWhitespaceTrivia() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo $$
End Class
Interface IFoo
    Sub TestSub()
End Interface</text>

            Dim expectedText = <text>   
    Public Sub TestSub() Implements IFoo.TestSub
        Throw New NotImplementedException()
    End Sub</text>

            Await TestAsync(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))

        End Function

        <WorkItem(544089)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInvocationAfterCommentTrivia() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements IFoo 'Comment $$
End Class
Interface IFoo
    Sub TestSub()
End Interface</text>


            Dim expectedText = <text>   
    Public Sub TestSub() Implements IFoo.TestSub
        Throw New NotImplementedException()
    End Sub</text>

            Await TestAsync(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoMembersToImplement() As Task
            Dim code = <text>
Class Foo
    Implements IFoo$$
End Class
Interface IFoo
End Interface</text>


            Dim expectedText = <text>   
Class Foo
    Implements IFoo

End Class
Interface IFoo
End Interface</text>

            Await TestAsync(code,
                 expectedText,
                 Sub(view, workspace)
                     Dim operations = workspace.GetService(Of IEditorOperationsFactoryService)() _
                                               .GetEditorOperations(view)
                     operations.InsertNewLine()
                 End Sub,
                 Sub(expected, actual, view) Assert.Equal(expected.Trim(), actual.Trim()))
        End Function

        <WorkItem(544211)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestWithEndBlockMissing() As Task
            Dim code = <text>
Imports System

Class Foo
    Implements ICloneable$$
</text>

            Dim expectedText = <text>   
Imports System
Class Foo
    Implements ICloneable

    Public Function Clone() As Object Implements ICloneable.Clone
        Throw New NotImplementedException()
    End Function
End Class</text>

            Await TestAsync(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(expected, actual, view) AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(529302)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestWithEndBlockMissing2() As Task
            Dim code = <text>
Imports System
Class Foo
    Implements ICloneable$$

Interface IFoo
End Interface</text>

            Dim expectedText = <text>
Imports System
Class Foo
    Implements ICloneable

    Public Function Clone() As Object Implements ICloneable.Clone
        Throw New NotImplementedException()
    End Function

    Interface IFoo
    End Interface
End Class</text>

            Await TestAsync(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(expected, actual, view) AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(530553)>
        <WorkItem(529337)>
        <WorkItem(674621)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestWithStatementSeparator() As Task
            Dim code = <text>
Imports System
Interface IFoo
    Sub Foo()
End Interface

Class CFoo : Implements IFoo$$ : End Class
</text>

            Dim expectedText = <text>   
Imports System
Interface IFoo
    Sub Foo()
End Interface

Class CFoo : Implements IFoo
    Public Sub Foo() Implements IFoo.Foo
        Throw New NotImplementedException()
    End Sub
End Class
</text>
            Await TestAsync(code,
                 expectedText,
                 Sub(view, workspace)
                     Dim operations = workspace.GetService(Of IEditorOperationsFactoryService)() _
                                               .GetEditorOperations(view)
                     operations.InsertNewLine()
                 End Sub,
                 Sub(expected, actual, view) AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(529360)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCursorNotOnSameLine() As Task
            Dim code = <text>
Imports System
Interface IFoo
    Sub FogBar()
End Interface
Public Class Bar
    Implements IFoo

$$End Class
</text>

            Dim expectedText = <text>   
Imports System
Interface IFoo
    Sub FogBar()
End Interface
Public Class Bar
    Implements IFoo


End Class
</text>
            Await TestAsync(code,
                 expectedText,
                 Sub(view, workspace)
                     Dim operations = workspace.GetService(Of IEditorOperationsFactoryService)() _
                                               .GetEditorOperations(view)
                     operations.InsertNewLine()
                 End Sub,
                 Sub(expected, actual, view) AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(529722)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCursorPlacedOnBlankLineAfter() As Task
            Dim code = <text>
Imports System
Public Class Bar
    Implements ICloneable$$

End Class
</text>

            Dim expectedText = <text>   
Imports System
Public Class Bar
    Implements ICloneable

    Public Function Clone() As Object Implements ICloneable.Clone
        Throw New NotImplementedException()
      End Function
End Class
</text>
            Await TestAsync(code,
                 expectedText,
                 Sub(view, workspace)
                     Dim operations = workspace.GetService(Of IEditorOperationsFactoryService)() _
                                               .GetEditorOperations(view)
                     operations.InsertNewLine()
                 End Sub,
                 Sub(expected, actual, view)
                     AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual)
                     Assert.Equal(4, view.Caret.Position.BufferPosition.GetContainingLine().LineNumber)
                     Assert.Equal(4, view.Caret.Position.VirtualSpaces)
                 End Sub)
        End Function

        <WorkItem(545867)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMultipleImplementationWithCaseDifference() As Task
            Dim code = <text>
Interface IA
    Sub foo()
End Interface

Interface IB
    Sub Foo()
End Interface

Class C
    Implements IA, IB$$

End Class</text>

            Dim expectedText = <text>
Class C
    Implements IA, IB

    Public Sub foo() Implements IA.foo
        Throw New NotImplementedException()
    End Sub

    Private Sub IB_Foo() Implements IB.Foo
        Throw New NotImplementedException()
    End Sub
End Class</text>

            Await TestAsync(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function

        <WorkItem(927478)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestFullyQualifiedName() As Task
            Dim code = <text>
Namespace N
    Interface IA
        Sub foo()
    End Interface
End Namespace

Class C
    Implements N.IA$$

End Class</text>

            Dim expectedText = <text>
Class C
    Implements N.IA

    Public Sub foo() Implements IA.foo
        Throw New NotImplementedException()
    End Sub
End Class</text>

            Await TestAsync(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(expected, actual, view) AssertEx.AssertContainsToleratingWhitespaceDifferences(expected, actual))
        End Function
    End Class
End Namespace
