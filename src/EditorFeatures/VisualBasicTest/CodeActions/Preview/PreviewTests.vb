' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.Implementation.Preview
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Preview
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
    Public Class PreviewTests
        Inherits AbstractVisualBasicCodeActionTest

        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddParts(
                GetType(MockPreviewPaneService))

        Private Const s_addedDocumentName As String = "AddedDocument"
        Private Const s_addedDocumentText As String = "Class C1 : End Class"
        Private Shared s_removedMetadataReferenceDisplayName As String = ""
        Private Const s_addedProjectName As String = "AddedProject"
        Private Shared ReadOnly s_addedProjectId As ProjectId = ProjectId.CreateNewId()
        Private Const s_changedDocumentText As String = "Class C : End Class"

        Protected Overrides Function GetComposition() As TestComposition
            Return s_composition
        End Function

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New MyCodeRefactoringProvider()
        End Function

        Private Class MyCodeRefactoringProvider : Inherits CodeRefactoringProvider
            Public NotOverridable Overrides Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
                Dim codeAction = New TestCodeAction(context.Document)
                context.RegisterRefactoring(codeAction, context.Span)
                Return Task.CompletedTask
            End Function

            Private NotInheritable Class TestCodeAction : Inherits CodeAction
                Private ReadOnly _oldDocument As Document

                Public Sub New(oldDocument As Document)
                    Me._oldDocument = oldDocument
                End Sub

                Public Overrides ReadOnly Property Title As String
                    Get
                        Return "Title"
                    End Get
                End Property

                Protected Overrides Function GetChangedSolutionAsync(
                        progress As IProgress(Of CodeAnalysisProgress),
                        cancellationToken As CancellationToken) As Task(Of Solution)
                    Dim solution = _oldDocument.Project.Solution

                    ' Add a document - This will result in IWpfTextView previews.
                    solution = solution.AddDocument(DocumentId.CreateNewId(_oldDocument.Project.Id, s_addedDocumentName), s_addedDocumentName, s_addedDocumentText)

                    ' Remove a reference - This will result in a string preview.
                    Dim removedReference = _oldDocument.Project.MetadataReferences.Last()
                    s_removedMetadataReferenceDisplayName = removedReference.Display
                    solution = solution.RemoveMetadataReference(_oldDocument.Project.Id, removedReference)

                    ' Add a project - This will result in a string preview.
                    solution = solution.AddProject(ProjectInfo.Create(s_addedProjectId, VersionStamp.Create(), s_addedProjectName, s_addedProjectName, LanguageNames.CSharp))

                    ' Change a document - This will result in IWpfTextView previews.
                    solution = solution.WithDocumentSyntaxRoot(_oldDocument.Id, VisualBasicSyntaxTree.ParseText(s_changedDocumentText).GetRoot())

                    Return Task.FromResult(solution)
                End Function
            End Class
        End Class

        Private Async Function GetMainDocumentAndPreviewsAsync(
                parameters As TestParameters,
                workspace As EditorTestWorkspace) As Task(Of (document As Document, previews As SolutionPreviewResult))
            Dim document = GetDocument(workspace)
            Dim provider = CreateCodeRefactoringProvider(workspace, parameters)
            Dim span = document.GetSyntaxRootAsync().Result.Span
            Dim refactorings = New List(Of CodeAction)()
            Dim context = New CodeRefactoringContext(document, span, Sub(a) refactorings.Add(a), CancellationToken.None)
            provider.ComputeRefactoringsAsync(context).Wait()
            Dim action = refactorings.Single()
            Dim editHandler = workspace.ExportProvider.GetExportedValue(Of ICodeActionEditHandlerService)()
            Dim previews = Await editHandler.GetPreviewsAsync(workspace, action.GetPreviewOperationsAsync(CancellationToken.None).Result, CancellationToken.None)

            Return (document, previews)
        End Function

        <WpfFact>
        Public Async Function TestPickTheRightPreview_NoPreference() As Task
            Dim parameters As New TestParameters()
            Using workspace = CreateWorkspaceFromOptions("Class D : End Class", parameters)
                Dim tuple = Await GetMainDocumentAndPreviewsAsync(parameters, workspace)
                Dim document = tuple.document
                Dim previews = tuple.previews

                ' The changed document comes first.
                Dim previewObjects = Await previews.GetPreviewsAsync()
                Dim preview = previewObjects(0)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is DifferenceViewerPreview)
                Dim diffView = DirectCast(preview, DifferenceViewerPreview)
                Dim text = diffView.Viewer.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Equal(s_changedDocumentText, text)
                diffView.Dispose()

                ' Then comes the removed metadata reference.
                preview = previewObjects(1)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                text = DirectCast(preview, String)
                Assert.Contains(s_removedMetadataReferenceDisplayName, text, StringComparison.Ordinal)

                ' And finally the added project.
                preview = previewObjects(2)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                text = DirectCast(preview, String)
                Assert.Contains(s_addedProjectName, text, StringComparison.Ordinal)
            End Using
        End Function
    End Class
End Namespace
