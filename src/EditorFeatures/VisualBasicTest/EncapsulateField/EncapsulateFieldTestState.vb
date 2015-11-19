' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Shared
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
Imports Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    Friend Class EncapsulateFieldTestState
        Implements IDisposable

        Private _testDocument As TestHostDocument
        Public Workspace As TestWorkspace
        Public TargetDocument As Document

        Private Shared ReadOnly s_exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                GetType(VisualBasicEncapsulateFieldService),
                GetType(DefaultDocumentSupportsFeatureService)))

        Private Sub New(workspace As TestWorkspace)
            workspace = workspace
            _testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue OrElse d.SelectedSpans.Any())
            TargetDocument = workspace.CurrentSolution.GetDocument(_testDocument.Id)
        End Sub

        Public Shared Async Function CreateAsync(markup As String) As System.Threading.Tasks.Task(Of EncapsulateFieldTestState)
            Dim workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(markup, exportProvider:=s_exportProvider)
            Return New EncapsulateFieldTestState(workspace)
        End Function

        Public Sub Encapsulate()
            Dim args = New EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer())
            Dim commandHandler = New EncapsulateFieldCommandHandler(TestWaitIndicator.Default, Workspace.GetService(Of ITextBufferUndoManagerProvider)())
            commandHandler.ExecuteCommand(args, Nothing)
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
