// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public partial class PreviewTests : AbstractCSharpCodeActionTest
    {
        private const string AddedDocumentName = "AddedDocument";
        private const string AddedDocumentText = "class C1 {}";
        private static string s_removedMetadataReferenceDisplayName = "";
        private const string AddedProjectName = "AddedProject";
        private static readonly ProjectId s_addedProjectId = ProjectId.CreateNewId();
        private const string ChangedDocumentText = "class C {}";

        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new MyCodeRefactoringProvider();
        }

        private class MyCodeRefactoringProvider : CodeRefactoringProvider
        {
            public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var codeAction = new MyCodeAction(context.Document);
                context.RegisterRefactoring(codeAction);
                return SpecializedTasks.EmptyTask;
            }

            private class MyCodeAction : CodeAction
            {
                private Document _oldDocument;

                public MyCodeAction(Document document)
                {
                    _oldDocument = document;
                }

                public override string Title
                {
                    get
                    {
                        return "Title";
                    }
                }

                protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
                {
                    var solution = _oldDocument.Project.Solution;

                    // Add a document - This will result in IWpfTextView previews.
                    solution = solution.AddDocument(DocumentId.CreateNewId(_oldDocument.Project.Id, AddedDocumentName), AddedDocumentName, AddedDocumentText);

                    // Remove a reference - This will result in a string preview.
                    var removedReference = _oldDocument.Project.MetadataReferences[_oldDocument.Project.MetadataReferences.Count - 1];
                    s_removedMetadataReferenceDisplayName = removedReference.Display;
                    solution = solution.RemoveMetadataReference(_oldDocument.Project.Id, removedReference);

                    // Add a project - This will result in a string preview.
                    solution = solution.AddProject(ProjectInfo.Create(s_addedProjectId, VersionStamp.Create(), AddedProjectName, AddedProjectName, LanguageNames.CSharp));

                    // Change a document - This will result in IWpfTextView previews.
                    solution = solution.WithDocumentSyntaxRoot(_oldDocument.Id, CSharpSyntaxTree.ParseText(ChangedDocumentText, cancellationToken: cancellationToken).GetRoot(cancellationToken));

                    return Task.FromResult(solution);
                }
            }
        }

        private void GetMainDocumentAndPreviews(TestWorkspace workspace, out Document document, out SolutionPreviewResult previews)
        {
            document = GetDocument(workspace);
            var provider = CreateCodeRefactoringProvider(workspace) as CodeRefactoringProvider;
            var span = document.GetSyntaxRootAsync().Result.Span;
            var refactorings = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => refactorings.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            var action = refactorings.Single();
            var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            previews = editHandler.GetPreviews(workspace, action.GetPreviewOperationsAsync(CancellationToken.None).Result, CancellationToken.None);
        }

        [WpfFact]
        public async Task TestPickTheRightPreview_NoPreference()
        {
            using (var workspace = CreateWorkspaceFromFile("class D {}", null, null))
            {
                Document document = null;
                SolutionPreviewResult previews = null;
                GetMainDocumentAndPreviews(workspace, out document, out previews);

                // The changed document comes first.
                var preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is IWpfDifferenceViewer);
                var diffView = preview as IWpfDifferenceViewer;
                var text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                Assert.Equal(ChangedDocumentText, text);
                diffView.Close();

                // The added document comes next.
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is IWpfDifferenceViewer);
                diffView = preview as IWpfDifferenceViewer;
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                Assert.Contains(AddedDocumentName, text, StringComparison.Ordinal);
                Assert.Contains(AddedDocumentText, text, StringComparison.Ordinal);
                diffView.Close();

                // Then comes the removed metadata reference.
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is string);
                text = preview as string;
                Assert.Contains(s_removedMetadataReferenceDisplayName, text, StringComparison.Ordinal);

                // And finally the added project.
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is string);
                text = preview as string;
                Assert.Contains(AddedProjectName, text, StringComparison.Ordinal);

                // There are no more previews.
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.Null(preview);
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.Null(preview);
            }
        }

        [WpfFact]
        public async Task TestPickTheRightPreview_WithPreference()
        {
            using (var workspace = CreateWorkspaceFromFile("class D {}", null, null))
            {
                Document document = null;
                SolutionPreviewResult previews = null;
                GetMainDocumentAndPreviews(workspace, out document, out previews);

                // Should return preview that matches the preferred (added) project.
                var preview = await previews.TakeNextPreviewAsync(preferredProjectId: s_addedProjectId).ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is string);
                var text = preview as string;
                Assert.Contains(AddedProjectName, text, StringComparison.Ordinal);

                // Should return preview that matches the preferred (changed) document.
                preview = await previews.TakeNextPreviewAsync(preferredDocumentId: document.Id).ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is IWpfDifferenceViewer);
                var diffView = preview as IWpfDifferenceViewer;
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                Assert.Equal(ChangedDocumentText, text);
                diffView.Close();

                // There is no longer a preview for the preferred project. Should return the first remaining preview.
                preview = await previews.TakeNextPreviewAsync(preferredProjectId: s_addedProjectId).ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is IWpfDifferenceViewer);
                diffView = preview as IWpfDifferenceViewer;
                text = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                Assert.Contains(AddedDocumentName, text, StringComparison.Ordinal);
                Assert.Contains(AddedDocumentText, text, StringComparison.Ordinal);
                diffView.Close();

                // There is no longer a preview for the  preferred document. Should return the first remaining preview.
                preview = await previews.TakeNextPreviewAsync(preferredDocumentId: document.Id).ConfigureAwait(true);
                Assert.NotNull(preview);
                Assert.True(preview is string);
                text = preview as string;
                Assert.Contains(s_removedMetadataReferenceDisplayName, text, StringComparison.Ordinal);

                // There are no more previews.
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.Null(preview);
                preview = await previews.TakeNextPreviewAsync().ConfigureAwait(true);
                Assert.Null(preview);
            }
        }
    }
}
