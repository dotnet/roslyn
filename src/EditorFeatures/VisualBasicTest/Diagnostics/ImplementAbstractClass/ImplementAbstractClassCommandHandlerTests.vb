' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ImplementAbstractClass
    Public Class ImplementAbstractClassCommandHandlerTests

        <WorkItem(530553)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestSimpleCases()
            Dim code = <text>
Imports System

Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
    Protected MustOverride Function Baz(s As String, ByRef d As Double) As Boolean
End Class
Public Class Bar
    Inherits Foo$$
End Class</text>

            Dim expectedText = <text>   
    Public Overrides Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub

    Protected Overrides Function Baz(s As String, ByRef d As Double) As Boolean
        Throw New NotImplementedException()
    End Function</text>

            Test(code,
             expectedText,
             Sub() Throw New Exception("The operation should have been handled."),
             Sub(x, y) AssertEx.AssertContainsToleratingWhitespaceDifferences(x, y))
        End Sub

        <WorkItem(530553)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestInvocationAfterWhitespaceTrivia()
            Dim code = <text>
Imports System

Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
End Class
Public Class Bar
    Inherits Foo $$
End Class
                      </text>

            Dim expectedText = <text>   
    Public Overrides Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub</text>

            Test(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(x, y) AssertEx.AssertContainsToleratingWhitespaceDifferences(x, y))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestInvocationAfterCommentTrivia()
            Dim code = <text>
Imports System

Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
End Class
Public Class Bar
    Inherits Foo 'Comment $$
End Class
                      </text>

            Dim expectedText = <text>   
    Public Overrides Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub</text>

            Test(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(x, y) AssertEx.AssertContainsToleratingWhitespaceDifferences(x, y))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestNoMembersToImplement()
            Dim code = <text>
Imports System

Public MustInherit Class Foo
End Class
Public Class Bar
    Inherits Foo$$
End Class</text>

            Dim expectedText = <text>   
Imports System

Public MustInherit Class Foo
End Class
Public Class Bar
    Inherits Foo

End Class</text>

            Using workspace = GetWorkspace(code)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(workspace.Documents.Single.GetTextView())
                Test(workspace,
                 expectedText,
                 Sub() editorOperations.InsertNewLine(),
                 Sub(x, y) Assert.Equal(x.Trim(), y.Trim()))
            End Using
        End Sub

        <WorkItem(544412)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestEnterNotOnSameLine()
            Dim code = <text>
MustInherit Class Base
    MustOverride Sub foo()
End Class
 
Class SomeClass
    Inherits Base 
    'comment $$
End Class
</text>

            Dim expectedText = <text>
MustInherit Class Base
    MustOverride Sub foo()
End Class
 
Class SomeClass
    Inherits Base 
    'comment 

End Class</text>

            Using workspace = GetWorkspace(code)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(workspace.Documents.Single.GetTextView())
                Test(workspace,
                 expectedText,
                 Sub() editorOperations.InsertNewLine(),
                 Sub(x, y) Assert.Equal(x.Trim(), y.Trim()))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        Public Sub TestWithEndBlockMissing()
            Dim code = <text>
Imports System

Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
End Class
Public Class Bar
    Inherits Foo$$
</text>

            Dim expectedText = <text>
Imports System

Public MustInherit Class Foo
    Public MustOverride Sub Foo(i As Integer)
End Class
Public Class Bar
    Inherits Foo

    Public Overrides Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub
End Class</text>

            Test(code,
                 expectedText,
                 Sub() Throw New Exception("The operation should have been handled."),
                 Sub(x, y) AssertEx.AssertEqualToleratingWhitespaceDifferences(x, y))
        End Sub

        Private Sub Test(code As XElement, expectedText As XElement, nextHandler As Action, assertion As Action(Of String, String))
            Using workspace = GetWorkspace(code)
                Test(workspace, expectedText, nextHandler, assertion)
            End Using
        End Sub

        Private Sub Test(workspace As TestWorkspace, expectedText As XElement, nextHandler As Action, assertion As Action(Of String, String))
            Dim document = workspace.Documents.Single()
            Dim view = document.GetTextView()
            Dim cursorPosition = document.CursorPosition.Value
            Dim snapshot = view.TextBuffer.CurrentSnapshot

            view.Caret.MoveTo(New SnapshotPoint(snapshot, cursorPosition))

            Dim commandHandler As ICommandHandler(Of ReturnKeyCommandArgs) =
                New ImplementAbstractClassCommandHandler(workspace.GetService(Of IEditorOperationsFactoryService))
            commandHandler.ExecuteCommand(New ReturnKeyCommandArgs(view, view.TextBuffer), nextHandler)

            Dim text = view.TextBuffer.CurrentSnapshot.AsText().ToString()

            assertion(expectedText.NormalizedValue, text)
        End Sub

        Private Function GetWorkspace(code As XElement) As TestWorkspace
            Return TestWorkspaceFactory.CreateWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                        <Document>
                            <%= code.Value %>
                        </Document>
                    </Project>
                </Workspace>)
        End Function
    End Class
End Namespace
