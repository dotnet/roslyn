// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public abstract class CSharpSyncNamespaceTestsBase : AbstractCodeActionTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected internal override string GetLanguage() => LanguageNames.CSharp;

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpSyncNamespaceCodeRefactoringProvider();

        protected static string ProjectRootPath
            => PathUtilities.IsUnixLikePlatform
            ? @"/ProjectA/"
            : @"C:\ProjectA\";

        protected static string ProjectFilePath
            => PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, "ProjectA.csproj");

        protected static (string folder, string filePath) CreateDocumentFilePath(string[] folder, string fileName = "DocumentA.cs")
        {
            if (folder == null || folder.Length == 0)
            {
                return (string.Empty, PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, fileName));
            }
            else
            {
                var folderPath = CreateFolderPath(folder);
                var relativePath = PathUtilities.CombinePossiblyRelativeAndRelativePaths(folderPath, fileName);
                return (folderPath, PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, relativePath));
            }
        }

        protected static string CreateFolderPath(params string[] folders)
            => string.Join(PathUtilities.DirectorySeparatorStr, folders);

        protected async Task TestMoveFileToMatchNamespace(string initialMarkup, List<string[]> expectedFolders = null)
        {
            var testOptions = new TestParameters();
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, testOptions))
            {
                if (expectedFolders?.Count > 0)
                {
                    var expectedFolderPaths = expectedFolders.Select(f => string.Join(PathUtilities.DirectorySeparatorStr, f));

                    var oldDocument = workspace.Documents[0];
                    var oldDocumentId = oldDocument.Id;
                    var expectedText = workspace.Documents[0].GetTextBuffer().CurrentSnapshot.GetText();

                    // a new document with the same text as old document is added.
                    var allResults = await TestOperationAsync(testOptions, workspace, expectedText);

                    var actualFolderPaths = new HashSet<string>();
                    foreach (var result in allResults)
                    {
                        // the original source document does not exist in the new solution.
                        var oldSolution = result.Item1;
                        var newSolution = result.Item2;

                        Assert.Null(newSolution.GetDocument(oldDocumentId));

                        var newDocument = GetDocumentToVerify(expectedChangedDocumentId: null, oldSolution, newSolution);
                        actualFolderPaths.Add(string.Join(PathUtilities.DirectorySeparatorStr, newDocument.Folders));
                    }

                    Assert.True(expectedFolderPaths.Count() == actualFolderPaths.Count, "Number of available \"Move file\" actions are not equal.");
                    foreach (var expected in expectedFolderPaths)
                    {
                        Assert.True(actualFolderPaths.Contains(expected));
                    }
                }
                else
                {
                    var (actions, _) = await GetCodeActionsAsync(workspace, testOptions);
                    if (actions.Length > 0)
                    {
                        var renameFileAction = actions.Any(action => action is not CodeAction.SolutionChangeAction);
                        Assert.False(renameFileAction, "Move File to match namespace code action was not expected, but shows up.");
                    }
                }
            }

            async Task<List<Tuple<Solution, Solution>>> TestOperationAsync(
                TestParameters parameters,
                TestWorkspace workspace,
                string expectedCode)
            {
                var results = new List<Tuple<Solution, Solution>>();

                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);
                var moveFileActions = actions.Where(a => a is not CodeAction.SolutionChangeAction);

                foreach (var action in moveFileActions)
                {
                    var operations = await action.GetOperationsAsync(CancellationToken.None);

                    results.Add(
                        await TestOperationsAsync(workspace,
                        expectedText: expectedCode,
                        operations: operations,
                        conflictSpans: ImmutableArray<TextSpan>.Empty,
                        renameSpans: ImmutableArray<TextSpan>.Empty,
                        warningSpans: ImmutableArray<TextSpan>.Empty,
                        navigationSpans: ImmutableArray<TextSpan>.Empty,
                        expectedChangedDocumentId: null));
                }

                return results;
            }
        }

        protected async Task TestChangeNamespaceAsync(
            string initialMarkUp,
            string expectedSourceOriginal,
            string expectedSourceReference = null)
        {
            var testOptions = new TestParameters();
            using (var workspace = CreateWorkspaceFromOptions(initialMarkUp, testOptions))
            {
                if (workspace.Projects.Count == 2)
                {
                    var project = workspace.Documents.Single(doc => !doc.SelectedSpans.IsEmpty()).Project;
                    var dependentProject = workspace.Projects.Single(proj => proj.Id != project.Id);
                    var references = dependentProject.ProjectReferences.ToList();
                    references.Add(new ProjectReference(project.Id));
                    dependentProject.ProjectReferences = references;
                    workspace.OnProjectReferenceAdded(dependentProject.Id, new ProjectReference(project.Id));
                }

                if (expectedSourceOriginal != null)
                {
                    var originalDocument = workspace.Documents.Single(doc => !doc.SelectedSpans.IsEmpty());
                    var originalDocumentId = originalDocument.Id;

                    var refDocument = workspace.Documents.Where(doc => doc.Id != originalDocumentId).SingleOrDefault();
                    var refDocumentId = refDocument?.Id;

                    var oldAndNewSolution = await TestOperationAsync(testOptions, workspace);
                    var oldSolution = oldAndNewSolution.Item1;
                    var newSolution = oldAndNewSolution.Item2;

                    var changedDocumentIds = SolutionUtilities.GetChangedDocuments(oldSolution, newSolution);

                    Assert.True(changedDocumentIds.Contains(originalDocumentId), "original document was not changed.");

                    var modifiedOriginalDocument = newSolution.GetDocument(originalDocumentId);
                    var modifiedOringinalRoot = await modifiedOriginalDocument.GetSyntaxRootAsync();

                    // One node/token will contain the warning we attached for change namespace action.
                    Assert.Single(modifiedOringinalRoot.DescendantNodesAndTokensAndSelf().Where(n =>
                        {
                            IEnumerable<SyntaxAnnotation> annotations;
                            if (n.IsNode)
                            {
                                annotations = n.AsNode().GetAnnotations(WarningAnnotation.Kind);
                            }
                            else
                            {
                                annotations = n.AsToken().GetAnnotations(WarningAnnotation.Kind);
                            }

                            return annotations.Any(annotation =>
                                WarningAnnotation.GetDescription(annotation) == FeaturesResources.Warning_colon_changing_namespace_may_produce_invalid_code_and_change_code_meaning);
                        }));

                    var actualText = (await modifiedOriginalDocument.GetTextAsync()).ToString();
                    AssertEx.EqualOrDiff(expectedSourceOriginal, actualText);

                    if (expectedSourceReference == null)
                    {
                        // there shouldn't be any textual change
                        if (changedDocumentIds.Contains(refDocumentId))
                        {
                            var oldRefText = (await oldSolution.GetDocument(refDocumentId).GetTextAsync()).ToString();
                            var newRefText = (await newSolution.GetDocument(refDocumentId).GetTextAsync()).ToString();
                            Assert.Equal(oldRefText, newRefText);
                        }
                    }
                    else
                    {
                        Assert.True(changedDocumentIds.Contains(refDocumentId));
                        var actualRefText = (await newSolution.GetDocument(refDocumentId).GetTextAsync()).ToString();
                        Assert.Equal(expectedSourceReference, actualRefText);
                    }
                }
                else
                {
                    var (actions, _) = await GetCodeActionsAsync(workspace, testOptions);
                    if (actions.Length > 0)
                    {
                        var hasChangeNamespaceAction = actions.Any(action => action is CodeAction.SolutionChangeAction);
                        Assert.False(hasChangeNamespaceAction, "Change namespace to match folder action was not expected, but shows up.");
                    }
                }
            }

            async Task<Tuple<Solution, Solution>> TestOperationAsync(TestParameters parameters, TestWorkspace workspace)
            {
                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);
                var changeNamespaceAction = actions.Single(a => a is CodeAction.SolutionChangeAction);
                var operations = await changeNamespaceAction.GetOperationsAsync(CancellationToken.None);

                return await ApplyOperationsAndGetSolutionAsync(workspace, operations);
            }
        }
    }
}
