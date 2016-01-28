' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Controls
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text.Differencing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
    Public Class PreviewTests : Inherits AbstractVisualBasicCodeActionTest
        Private Const s_addedDocumentName As String = "AddedDocument"
        Private Const s_addedDocumentText As String = "Class C1 : End Class"
        Private Shared s_removedMetadataReferenceDisplayName As String = ""
        Private Const s_addedProjectName As String = "AddedProject"
        Private Shared ReadOnly s_addedProjectId As ProjectId = ProjectId.CreateNewId()
        Private Const s_changedDocumentText As String = "Class C : End Class"

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New MyCodeRefactoringProvider()
        End Function

        Private Class MyCodeRefactoringProvider : Inherits CodeRefactoringProvider
            Public NotOverridable Overrides Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
                Dim codeAction = New MyCodeAction(context.Document)
                context.RegisterRefactoring(codeAction)
                Return SpecializedTasks.EmptyTask
            End Function

            Private Class MyCodeAction : Inherits CodeAction
                Private _oldDocument As Document

                Public Sub New(oldDocument As Document)
                    Me._oldDocument = oldDocument
                End Sub

                Public Overrides ReadOnly Property Title As String
                    Get
                        Return "Title"
                    End Get
                End Property

                Protected Overrides Function GetChangedSolutionAsync(cancellationToken As CancellationToken) As Task(Of Solution)
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

        Private Sub GetMainDocumentAndPreviews(workspace As TestWorkspace, ByRef document As Document, ByRef previews As SolutionPreviewResult)
            document = GetDocument(workspace)
            Dim provider = DirectCast(CreateCodeRefactoringProvider(workspace), CodeRefactoringProvider)
            Dim span = document.GetSyntaxRootAsync().Result.Span
            Dim refactorings = New List(Of CodeAction)()
            Dim context = New CodeRefactoringContext(document, span, Sub(a) refactorings.Add(a), CancellationToken.None)
            provider.ComputeRefactoringsAsync(context).Wait()
            Dim action = refactorings.Single()
            Dim editHandler = workspace.ExportProvider.GetExportedValue(Of ICodeActionEditHandlerService)()
            previews = editHandler.GetPreviews(workspace, action.GetPreviewOperationsAsync(CancellationToken.None).Result, CancellationToken.None)
        End Sub

        <WpfFact>
        Public Async Function TestPickTheRightPreview_NoPreference() As Task
            Using workspace = Await CreateWorkspaceFromFileAsync("Class D : End Class", Nothing, Nothing)
                Dim document As Document = Nothing
                Dim previews As SolutionPreviewResult = Nothing
                GetMainDocumentAndPreviews(workspace, document, previews)

                ' The changed document comes first.
                Dim previewObjects = Await previews.GetPreviewsAsync()
                Dim preview = previewObjects(0)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is IWpfDifferenceViewer)
                Dim diffView = DirectCast(preview, IWpfDifferenceViewer)
                Dim text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Equal(s_changedDocumentText, text)
                diffView.Close()

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
