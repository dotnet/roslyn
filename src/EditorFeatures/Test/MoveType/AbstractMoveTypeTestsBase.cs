// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MoveType
{
    public abstract class AbstractMoveTypeTestsBase : AbstractCodeActionTest
    {
        private const string SpanMarker = "[||]";

        private string StripSpanMarkers(string text)
        {
            var index = text.IndexOf(SpanMarker);
            return text.Remove(index, SpanMarker.Length);
        }

        protected async Task TestRenameFileToMatchTypeAsync(
            string originalCode,
            string expectedDocumentName)
        {
            // TODO: Implement this.
            await Task.Delay(1);
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
                    var refactoring = await GetCodeRefactoringAsync(workspace);

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
