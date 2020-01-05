' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Shared
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    Friend Class EncapsulateFieldTestState
        Implements IDisposable

        Private _testDocument As TestHostDocument
        Public Workspace As TestWorkspace
        Public TargetDocument As Document

        Private Shared ReadOnly s_exportProviderFactory As IExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                    GetType(VisualBasicEncapsulateFieldService),
                    GetType(DefaultTextBufferSupportsFeatureService)))

        Private Sub New(workspace As TestWorkspace)
            Me.Workspace = workspace
            _testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue OrElse d.SelectedSpans.Any())
            TargetDocument = workspace.CurrentSolution.GetDocument(_testDocument.Id)
        End Sub

        Public Shared Function Create(markup As String) As EncapsulateFieldTestState
            Dim workspace = TestWorkspace.CreateVisualBasic(markup, exportProvider:=s_exportProviderFactory.CreateExportProvider())
            Return New EncapsulateFieldTestState(workspace)
        End Function

        Public Sub Encapsulate()
            Dim args = New EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer())
            Dim commandHandler = New EncapsulateFieldCommandHandler(
                Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)(),
                Workspace.GetService(Of ITextBufferUndoManagerProvider)(),
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
