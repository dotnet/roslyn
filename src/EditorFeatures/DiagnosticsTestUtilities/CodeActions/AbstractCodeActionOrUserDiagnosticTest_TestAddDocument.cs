﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract partial class AbstractCodeActionOrUserDiagnosticTest
    {
        protected async Task TestAddDocumentInRegularAndScriptAsync(
            string initialMarkup, string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            TestParameters parameters = default)
        {
            await TestAddDocument(
                initialMarkup, expectedMarkup,
                expectedContainers, expectedDocumentName,
                WithRegularOptions(parameters));

            // VB script is not supported:
            if (GetLanguage() == LanguageNames.CSharp)
            {
                await TestAddDocument(
                    initialMarkup, expectedMarkup,
                    expectedContainers, expectedDocumentName,
                    WithScriptOptions(parameters));
            }
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocumentAsync(
            TestParameters parameters,
            TestWorkspace workspace,
            string expectedMarkup,
            string expectedDocumentName,
            ImmutableArray<string> expectedContainers)
        {
            var (_, action) = await GetCodeActionsAsync(workspace, parameters);
            return await TestAddDocument(
                workspace, expectedMarkup, expectedContainers,
                expectedDocumentName, action);
        }

        protected async Task TestAddDocument(
            string initialMarkup,
            string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                await TestAddDocument(
                    workspace, expectedMarkup, expectedContainers,
                    expectedDocumentName, action);
            }
        }

        private async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expectedMarkup,
            ImmutableArray<string> expectedFolders,
            string expectedDocumentName,
            CodeAction action)
        {
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, default);
            return await TestAddDocument(
                workspace,
                expectedMarkup,
                operations,
                hasProjectChange: false,
                modifiedProjectId: null,
                expectedFolders: expectedFolders,
                expectedDocumentName: expectedDocumentName);
        }

        protected static async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expected,
            ImmutableArray<CodeActionOperation> operations,
            bool hasProjectChange,
            ProjectId modifiedProjectId,
            ImmutableArray<string> expectedFolders,
            string expectedDocumentName)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            Document addedDocument = null;
            if (!hasProjectChange)
            {
                addedDocument = SolutionUtilities.GetSingleAddedDocument(oldSolution, newSolution);
            }
            else
            {
                Assert.NotNull(modifiedProjectId);
                addedDocument = newSolution.GetProject(modifiedProjectId).Documents.SingleOrDefault(doc => doc.Name == expectedDocumentName);
            }

            Assert.NotNull(addedDocument);

            AssertEx.Equal(expectedFolders, addedDocument.Folders);
            Assert.Equal(expectedDocumentName, addedDocument.Name);
            Assert.Equal(expected, (await addedDocument.GetTextAsync()).ToString());

            var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            if (!hasProjectChange)
            {
                // If there is just one document change then we expect the preview to be a WpfTextView
                var content = (await editHandler.GetPreviews(workspace, operations, CancellationToken.None).GetPreviewsAsync())[0];
                using (var diffView = content as DifferenceViewerPreview)
                {
                    Assert.NotNull(diffView.Viewer);
                }
            }
            else
            {
                // If there are more changes than just the document we need to browse all the changes and get the document change
                var contents = editHandler.GetPreviews(workspace, operations, CancellationToken.None);
                var hasPreview = false;
                var previews = await contents.GetPreviewsAsync();
                if (previews != null)
                {
                    foreach (var preview in previews)
                    {
                        if (preview != null)
                        {
                            var diffView = preview as DifferenceViewerPreview;
                            if (diffView?.Viewer != null)
                            {
                                hasPreview = true;
                                diffView.Dispose();
                                break;
                            }
                        }
                    }
                }

                Assert.True(hasPreview);
            }

            return Tuple.Create(oldSolution, newSolution);
        }
    }
}
