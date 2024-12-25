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

        Private ReadOnly _testDocument As EditorTestHostDocument
        Public Workspace As EditorTestWorkspace
        Public TargetDocument As Document

        Private Sub New(workspace As EditorTestWorkspace)
            Me.Workspace = workspace
            _testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue OrElse d.SelectedSpans.Any())
            TargetDocument = workspace.CurrentSolution.GetDocument(_testDocument.Id)
        End Sub

        Public Shared Function Create(markup As String) As EncapsulateFieldTestState
            Dim workspace = EditorTestWorkspace.CreateVisualBasic(markup, composition:=EditorTestCompositions.EditorFeatures)
            Return New EncapsulateFieldTestState(workspace)
        End Function

        Public Async Function EncapsulateAsync() As Task
            Dim args = New EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer())
            Dim commandHandler = New EncapsulateFieldCommandHandler(
                Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)(),
                Workspace.GetService(Of ITextBufferUndoManagerProvider)(),
                Workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider))
            Dim provider = Workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)()
            Dim waiter = DirectCast(provider.GetListener(FeatureAttribute.EncapsulateField), IAsynchronousOperationWaiter)
            commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create())
            Await waiter.ExpeditedWaitAsync()
        End Function

        Public Async Function AssertEncapsulateAsAsync(expected As String) As Task
            Await EncapsulateAsync()
            Assert.Equal(expected, _testDocument.GetTextBuffer().CurrentSnapshot.GetText().ToString())
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Workspace?.Dispose()
        End Sub
    End Class
End Namespace
