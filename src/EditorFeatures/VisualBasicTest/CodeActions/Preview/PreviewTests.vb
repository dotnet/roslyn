' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text.Differencing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
    Public Class PreviewTests : Inherits AbstractVisualBasicCodeActionTest
        Private Const AddedDocumentName As String = "AddedDocument"
        Private Const AddedDocumentText As String = "Class C1 : End Class"
        Private Shared removedMetadataReferenceDisplayName As String = ""
        Private Const AddedProjectName As String = "AddedProject"
        Private Shared ReadOnly AddedProjectId As ProjectId = ProjectId.CreateNewId()
        Private Const ChangedDocumentText As String = "Class C : End Class"

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
                Private oldDocument As Document

                Public Sub New(oldDocument As Document)
                    Me.oldDocument = oldDocument
                End Sub

                Public Overrides ReadOnly Property Title As String
                    Get
                        Return "Title"
                    End Get
                End Property

                Protected Overrides Function GetChangedSolutionAsync(cancellationToken As CancellationToken) As Task(Of Solution)
                    Dim solution = oldDocument.Project.Solution

                    ' Add a document - This will result in IWpfTextView previews.
                    solution = solution.AddDocument(DocumentId.CreateNewId(oldDocument.Project.Id, AddedDocumentName), AddedDocumentName, AddedDocumentText)

                    ' Remove a reference - This will result in a string preview.
                    Dim removedReference = oldDocument.Project.MetadataReferences.Last()
                    removedMetadataReferenceDisplayName = removedReference.Display
                    solution = solution.RemoveMetadataReference(oldDocument.Project.Id, removedReference)

                    ' Add a project - This will result in a string preview.
                    solution = solution.AddProject(ProjectInfo.Create(AddedProjectId, VersionStamp.Create(), AddedProjectName, AddedProjectName, LanguageNames.CSharp))

                    ' Change a document - This will result in IWpfTextView previews.
                    solution = solution.WithDocumentSyntaxRoot(oldDocument.Id, VisualBasicSyntaxTree.ParseText(ChangedDocumentText).GetRoot())

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

        <Fact>
        Public Sub TestPickTheRightPreview_NoPreference()
            Using workspace = CreateWorkspaceFromFile("Class D : End Class", Nothing, Nothing)
                Dim document As Document = Nothing
                Dim previews As SolutionPreviewResult = Nothing
                GetMainDocumentAndPreviews(workspace, document, previews)

                ' The changed document comes first.
                Dim preview = previews.TakeNextPreview()
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is IWpfDifferenceViewer)
                Dim diffView = DirectCast(preview, IWpfDifferenceViewer)
                Dim text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Equal(ChangedDocumentText, text)
                diffView.Close()

                ' The added document comes next.
                preview = previews.TakeNextPreview()
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is IWpfDifferenceViewer)
                diffView = DirectCast(preview, IWpfDifferenceViewer)
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Contains(AddedDocumentName, text, StringComparison.Ordinal)
                Assert.Contains(AddedDocumentText, text, StringComparison.Ordinal)
                diffView.Close()

                ' Then comes the removed metadata reference.
                preview = previews.TakeNextPreview()
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                text = DirectCast(preview, String)
                Assert.Contains(removedMetadataReferenceDisplayName, text, StringComparison.Ordinal)

                ' And finally the added project.
                preview = previews.TakeNextPreview()
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                text = DirectCast(preview, String)
                Assert.Contains(AddedProjectName, text, StringComparison.Ordinal)

                ' There are no more previews.
                preview = previews.TakeNextPreview()
                Assert.Null(preview)
                preview = previews.TakeNextPreview()
                Assert.Null(preview)
            End Using
        End Sub

        <Fact>
        Public Sub TestPickTheRightPreview_WithPreference()
            Using workspace = CreateWorkspaceFromFile("Class D : End Class", Nothing, Nothing)
                Dim document As Document = Nothing
                Dim previews As SolutionPreviewResult = Nothing
                GetMainDocumentAndPreviews(workspace, document, previews)

                ' Should return preview that matches the preferred (added) project.
                Dim preview = previews.TakeNextPreview(preferredProjectId:=AddedProjectId)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                Dim text = DirectCast(preview, String)
                Assert.Contains(AddedProjectName, text, StringComparison.Ordinal)

                ' Should return preview that matches the preferred (changed) document.
                preview = previews.TakeNextPreview(preferredDocumentId:=document.Id)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is IWpfDifferenceViewer)
                Dim diffView = DirectCast(preview, IWpfDifferenceViewer)
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Equal(ChangedDocumentText, text)
                diffView.Close()

                ' There is no longer a preview for the preferred project. Should return the first remaining preview.
                preview = previews.TakeNextPreview(preferredProjectId:=AddedProjectId)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is IWpfDifferenceViewer)
                diffView = DirectCast(preview, IWpfDifferenceViewer)
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString()
                Assert.Contains(AddedDocumentName, text, StringComparison.Ordinal)
                Assert.Contains(AddedDocumentText, text, StringComparison.Ordinal)
                diffView.Close()

                ' There is no longer a preview for the  preferred document. Should return the first remaining preview.
                preview = previews.TakeNextPreview(preferredDocumentId:=document.Id)
                Assert.NotNull(preview)
                Assert.True(TypeOf preview Is String)
                text = DirectCast(preview, String)
                Assert.Contains(removedMetadataReferenceDisplayName, text, StringComparison.Ordinal)

                ' There are no more previews.
                preview = previews.TakeNextPreview()
                Assert.Null(preview)
                preview = previews.TakeNextPreview()
                Assert.Null(preview)
            End Using
        End Sub
    End Class
End Namespace
