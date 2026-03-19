// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Preview;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;

public sealed partial class PreviewTests : AbstractCSharpCodeActionTest
{
    private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures
        .AddParts(
            typeof(MockPreviewPaneService));

    private const string AddedDocumentName = "AddedDocument";
    private const string AddedDocumentText = "class C1 {}";
    private static string s_removedMetadataReferenceDisplayName = "";
    private const string AddedProjectName = "AddedProject";
    private static readonly ProjectId s_addedProjectId = ProjectId.CreateNewId();
    private const string ChangedDocumentText = "class C {}";

    protected override TestComposition GetComposition() => s_composition;

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new MyCodeRefactoringProvider();

    private sealed class MyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var codeAction = new TestCodeAction(context.Document);
            context.RegisterRefactoring(codeAction, context.Span);
        }

        private sealed class TestCodeAction : CodeAction
        {
            private readonly Document _oldDocument;

            public TestCodeAction(Document document)
                => _oldDocument = document;

            public override string Title
            {
                get
                {
                    return "Title";
                }
            }

            protected override async Task<Solution> GetChangedSolutionAsync(
                IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
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

                return solution;
            }
        }
    }

    private async Task<(Document document, SolutionPreviewResult previews)> GetMainDocumentAndPreviewsAsync(TestParameters parameters, EditorTestWorkspace workspace)
    {
        var document = GetDocument(workspace);
        var provider = CreateCodeRefactoringProvider(workspace, parameters);
        var span = document.GetSyntaxRootAsync().Result.Span;
        var refactorings = new List<CodeAction>();
        var context = new CodeRefactoringContext(document, span, refactorings.Add, CancellationToken.None);
        provider.ComputeRefactoringsAsync(context).Wait();
        var action = refactorings.Single();
        var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
        var previews = await editHandler.GetPreviewsAsync(workspace, action.GetPreviewOperationsAsync(CancellationToken.None).Result, CancellationToken.None);

        return (document, previews);
    }

    [WpfFact]
    public async Task TestPickTheRightPreview_NoPreference()
    {
        var parameters = TestParameters.Default;
        using var workspace = CreateWorkspaceFromOptions("class D {}", parameters);

        var (document, previews) = await GetMainDocumentAndPreviewsAsync(parameters, workspace);

        // The changed document comes first.
        var previewObjects = await previews.GetPreviewsAsync();
        var preview = previewObjects[0];
        Assert.NotNull(preview);
        Assert.True(preview is DifferenceViewerPreview);
        var diffView = preview as DifferenceViewerPreview;
        var text = diffView.Viewer.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
        Assert.Equal(ChangedDocumentText, text);
        diffView.Dispose();

        // Then comes the removed metadata reference.
        preview = previewObjects[1];
        Assert.NotNull(preview);
        Assert.True(preview is string);
        text = preview as string;
        Assert.Contains(s_removedMetadataReferenceDisplayName, text, StringComparison.Ordinal);

        // And finally the added project.
        preview = previewObjects[2];
        Assert.NotNull(preview);
        Assert.True(preview is string);
        text = preview as string;
        Assert.Contains(AddedProjectName, text, StringComparison.Ordinal);
    }
}
