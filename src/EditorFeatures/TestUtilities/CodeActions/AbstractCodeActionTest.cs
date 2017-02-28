// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionTest : AbstractCodeActionOrUserDiagnosticTest
    {
        protected abstract CodeRefactoringProvider CreateCodeRefactoringProvider(
            Workspace workspace, TestParameters parameters);

        protected override async Task<IList<CodeAction>> GetCodeActionsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            return (await GetCodeRefactoringAsync(workspace, parameters))?.Actions?.ToList();
        }

        internal async Task<CodeRefactoring> GetCodeRefactoringAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            return (await GetCodeRefactoringsAsync(workspace, parameters)).FirstOrDefault();
        }

        private async Task<IEnumerable<CodeRefactoring>> GetCodeRefactoringsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var provider = CreateCodeRefactoringProvider(workspace, parameters);
            return SpecializedCollections.SingletonEnumerable(
                await GetCodeRefactoringAsync(provider, workspace));
        }

        private async Task<CodeRefactoring> GetCodeRefactoringAsync(
            CodeRefactoringProvider provider,
            TestWorkspace workspace)
        {
            var document = GetDocument(workspace);
            var span = workspace.Documents.Single(d => !d.IsLinkFile && d.SelectedSpans.Count == 1).SelectedSpans.Single();
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => actions.Add(a), CancellationToken.None);
            await provider.ComputeRefactoringsAsync(context);
            return actions.Count > 0 ? new CodeRefactoring(provider, actions) : null;
        }

        protected async Task TestActionsOnLinkedFiles(
            TestWorkspace workspace,
            string expectedText,
            int index,
            IList<CodeAction> actions,
            string expectedPreviewContents = null,
            bool ignoreTrivia = true)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions);

            await VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            applyChangesOperation.TryApply(workspace, new ProgressTracker(), CancellationToken.None);

            foreach (var document in workspace.Documents)
            {
                var fixedRoot = await workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync();
                var actualText = ignoreTrivia ? fixedRoot.ToString() : fixedRoot.ToFullString();

                if (ignoreTrivia)
                {
                    TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
                }
                else
                {
                    Assert.Equal(expectedText, actualText);
                }
            }
        }

        private static async Task VerifyPreviewContents(
            TestWorkspace workspace, string expectedPreviewContents,
            ImmutableArray<CodeActionOperation> operations)
        {
            if (expectedPreviewContents != null)
            {
                var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
                var content = (await editHandler.GetPreviews(workspace, operations, CancellationToken.None).GetPreviewsAsync())[0];
                var diffView = content as DifferenceViewerPreview;
                Assert.NotNull(diffView.Viewer);
                var previewContents = diffView.Viewer.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                diffView.Dispose();

                Assert.Equal(expectedPreviewContents, previewContents);
            }
        }

        protected static Document GetDocument(TestWorkspace workspace)
        {
            return workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
        }

        private class TestPickMembersService : IPickMembersService
        {
            private readonly ImmutableArray<string> _memberNames;

            public TestPickMembersService(ImmutableArray<string> memberNames)
                => _memberNames = memberNames;

            public PickMembersResult PickMembers(string title, ImmutableArray<ISymbol> members)
                => new PickMembersResult(_memberNames.SelectAsArray(n => members.Single(m => m.Name == n)));
        }

        internal Task TestWithPickMembersDialogAsync(
            string initialMarkup,
            string expectedMarkup,
            string[] chosenSymbols,
            int index = 0,
            bool ignoreTrivia = true,
            CodeActionPriority? priority = null,
            TestParameters parameters = default(TestParameters))
        {
            var pickMembersService = new TestPickMembersService(chosenSymbols.AsImmutableOrEmpty());
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup,
                index, ignoreTrivia, priority,
                parameters.WithFixProviderData(pickMembersService));
        }
    }
}