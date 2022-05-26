' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    Friend Class EncapsulateFieldTestState
        Implements IDisposable

        Private ReadOnly _testDocument As TestHostDocument
        Public Workspace As TestWorkspace
        Public TargetDocument As Document

        Private Sub New(workspace As TestWorkspace)
            Me.Workspace = workspace
            _testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue OrElse d.SelectedSpans.Any())
            TargetDocument = workspace.CurrentSolution.GetDocument(_testDocument.Id)
        End Sub

        Public Shared Function Create(markup As String) As EncapsulateFieldTestState
            Dim workspace = TestWorkspace.CreateVisualBasic(markup, composition:=EditorTestCompositions.EditorFeatures)
            Return New EncapsulateFieldTestState(workspace)
        End Function

        Public Sub Encapsulate()
            Dim args = New EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer())
            Dim commandHandler = New EncapsulateFieldCommandHandler(
                Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)(),
                Workspace.GetService(Of ITextBufferUndoManagerProvider)(),
                Workspace.GlobalOptions,
                Workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider))
            commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create())
        End Sub

        Public Sub AssertEncapsulateAs(expected As String)
            Encapsulate()
            Assert.Equal(expected, _testDocument.GetTextBuffer().CurrentSnapshot.GetText().ToString())
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If Workspace IsNot Nothing Then
                Workspace.Dispose()
            End If
        End Sub

    End Class
End Namespace
