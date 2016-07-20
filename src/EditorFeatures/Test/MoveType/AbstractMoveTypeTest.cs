// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MoveType
{
    public abstract class AbstractMoveTypeTest : AbstractCodeActionTest
    {
        private string RenameFileCodeActionTitle = FeaturesResources.Rename_file_to_0;
        private string RenameTypeCodeActionTitle = FeaturesResources.Rename_type_to_0;

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new MoveTypeCodeRefactoringProvider();
        }

        protected async Task TestRenameTypeToMatchFileAsync(
           string originalCode,
           string expectedCode = null,
           bool expectedCodeAction = true,
           bool compareTokens = true)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(originalCode, parseOptions: null, compilationOptions: null))
            {
                if (expectedCodeAction)
                {
                    Assert.True(expectedCode != null, $"{nameof(expectedCode)} should be present if {nameof(expectedCodeAction)} is true.");

                    var documentId = workspace.Documents[0].Id;
                    var documentName = workspace.Documents[0].Name;

                    string expectedText;
                    TextSpan span;
                    MarkupTestFile.GetSpan(expectedCode, out expectedText, out span);

                    var codeActionTitle = string.Format(RenameTypeCodeActionTitle, expectedText.Substring(span.Start, span.Length));

                    var oldSolutionAndNewSolution = await TestOperationAsync(
                        workspace, expectedText, codeActionTitle, compareTokens);

                    // the original source document does not exist in the new solution.
                    var newSolution = oldSolutionAndNewSolution.Item2;

                    var document = newSolution.GetDocument(documentId);
                    Assert.NotNull(document);
                    Assert.Equal(documentName, document.Name);
                }
                else
                {
                    var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                    if (actions != null)
                    {
                        var renameFileAction = actions.Any(action => action.Title.StartsWith(RenameTypeCodeActionTitle));
                        Assert.False(renameFileAction, "Rename Type to match file name code action was not expected, but shows up.");
                    }
                }
            }
        }

        protected async Task TestRenameFileToMatchTypeAsync(
            string originalCode,
            string expectedDocumentName = null,
            bool expectedCodeAction = true,
            bool compareTokens = true)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(originalCode, parseOptions: null, compilationOptions: null))
            {
                if (expectedCodeAction)
                {
                    Assert.True(expectedDocumentName != null, $"{nameof(expectedDocumentName)} should be present if {nameof(expectedCodeAction)} is true.");

                    var oldDocumentId = workspace.Documents[0].Id;

                    string expectedText;
                    IList<TextSpan> spans;
                    MarkupTestFile.GetSpans(originalCode, out expectedText, out spans);

                    var codeActionTitle = string.Format(RenameFileCodeActionTitle, expectedDocumentName);

                    // a new document with the same text as old document is added.
                    var oldSolutionAndNewSolution = await TestOperationAsync(
                        workspace, expectedText, codeActionTitle, compareTokens);

                    // the original source document does not exist in the new solution.
                    var newSolution = oldSolutionAndNewSolution.Item2;
                    Assert.Null(newSolution.GetDocument(oldDocumentId));
                }
                else
                {
                    var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                    if (actions != null)
                    {
                        var renameFileAction = actions.Any(action => action.Title.StartsWith(RenameFileCodeActionTitle));
                        Assert.False(renameFileAction, "Rename File to match type code action was not expected, but shows up.");
                    }
                }
            }
        }

        private async Task<Tuple<Solution, Solution>> TestOperationAsync(
            Workspaces.TestWorkspace workspace,
            string expectedCode,
            string operation,
            bool compareTokens)
        {
            var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);
            var action = actions.Single(a => a.Title.StartsWith(operation));
            var operations = await action.GetOperationsAsync(CancellationToken.None);

            return await TestOperationsAsync(workspace,
                expectedText: expectedCode,
                operations: operations,
                conflictSpans: null,
                renameSpans: null,
                warningSpans: null,
                compareTokens: compareTokens,
                expectedChangedDocumentId: null);
        }

        protected async Task TestMoveTypeToNewFileAsync(
            string originalCode,
            string expectedSourceTextAfterRefactoring,
            string expectedDocumentName,
            string destinationDocumentText,
            IList<string> destinationDocumentContainers = null,
            bool expectedCodeAction = true,
            int index = 0,
            bool compareTokens = true)
        {
            if (expectedCodeAction)
            {
                using (var workspace = await CreateWorkspaceFromFileAsync(originalCode, parseOptions: null, compilationOptions: null))
                {
                    // replace with default values on null.
                    if (destinationDocumentContainers == null)
                    {
                        destinationDocumentContainers = Array.Empty<string>();
                    }

                    var sourceDocumentId = workspace.Documents[0].Id;

                    // Verify the newly added document and its text
                    var oldSolutionAndNewSolution = await TestAddDocumentAsync(workspace,
                        destinationDocumentText, index, expectedDocumentName, destinationDocumentContainers, compareTokens: compareTokens);

                    // Verify source document's text after moving type.
                    var oldSolution = oldSolutionAndNewSolution.Item1;
                    var newSolution = oldSolutionAndNewSolution.Item2;
                    var changedDocumentIds = SolutionUtilities.GetChangedDocuments(oldSolution, newSolution);
                    Assert.True(changedDocumentIds.Contains(sourceDocumentId), "source document was not changed.");

                    var modifiedSourceDocument = newSolution.GetDocument(sourceDocumentId);

                    if (compareTokens)
                    {
                        TokenUtilities.AssertTokensEqual(
                            expectedSourceTextAfterRefactoring, (await modifiedSourceDocument.GetTextAsync()).ToString(), GetLanguage());
                    }
                    else
                    {
                        Assert.Equal(expectedSourceTextAfterRefactoring, (await modifiedSourceDocument.GetTextAsync()).ToString());
                    }
                }
            }
            else
            {
                await TestMissingAsync(originalCode, parseOptions: null);
            }
        }
    }
}
