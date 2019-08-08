// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionTest : AbstractCodeActionOrUserDiagnosticTest
    {
        protected abstract CodeRefactoringProvider CreateCodeRefactoringProvider(
            Workspace workspace, TestParameters parameters);

        protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var refactoring = await GetCodeRefactoringAsync(workspace, parameters);
            var actions = refactoring == null
                ? ImmutableArray<CodeAction>.Empty
                : refactoring.CodeActions.Select(n => n.action).AsImmutable();
            actions = MassageActions(actions);
            return (actions, actions.IsDefaultOrEmpty ? null : actions[parameters.index]);
        }

        protected override Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(TestWorkspace workspace, TestParameters parameters)
        {
            return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
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
            var documentsWithSelections = workspace.Documents.Where(d => !d.IsLinkFile && d.SelectedSpans.Count == 1);
            Debug.Assert(documentsWithSelections.Count() == 1, "One document must have a single span annotation");
            var span = documentsWithSelections.Single().SelectedSpans.Single();
            var actions = ArrayBuilder<(CodeAction, TextSpan?)>.GetInstance();
            var document = workspace.CurrentSolution.GetDocument(documentsWithSelections.Single().Id);
            var context = new CodeRefactoringContext(document, span, (a, t) => actions.Add((a, t)), CancellationToken.None);
            await provider.ComputeRefactoringsAsync(context);

            var result = actions.Count > 0 ? new CodeRefactoring(provider, actions.ToImmutable()) : null;
            actions.Free();
            return result;
        }

        protected async Task TestActionOnLinkedFiles(
            TestWorkspace workspace,
            string expectedText,
            CodeAction action,
            string expectedPreviewContents = null)
        {
            var operations = await VerifyActionAndGetOperationsAsync(action, default);

            await VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            applyChangesOperation.TryApply(workspace, new ProgressTracker(), CancellationToken.None);

            foreach (var document in workspace.Documents)
            {
                var fixedRoot = await workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync();
                var actualText = fixedRoot.ToFullString();
                Assert.Equal(expectedText, actualText);
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
            => workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

        private class TestPickMembersService : IPickMembersService
        {
            private readonly ImmutableArray<string> _memberNames;
            private readonly Action<ImmutableArray<PickMembersOption>> _optionsCallback;

            public TestPickMembersService(
                ImmutableArray<string> memberNames,
                Action<ImmutableArray<PickMembersOption>> optionsCallback)
            {
                _memberNames = memberNames;
                _optionsCallback = optionsCallback;
            }

            public PickMembersResult PickMembers(
                string title, ImmutableArray<ISymbol> members,
                ImmutableArray<PickMembersOption> options)
            {
                _optionsCallback?.Invoke(options);
                return new PickMembersResult(
                    _memberNames.IsDefault
                        ? members
                        : _memberNames.SelectAsArray(n => members.Single(m => m.Name == n)),
                    options);
            }
        }

        internal void EnableOptions(
            ImmutableArray<PickMembersOption> options,
            params string[] ids)
        {
            foreach (var id in ids)
            {
                EnableOption(options, id);
            }
        }

        internal void EnableOption(ImmutableArray<PickMembersOption> options, string id)
        {
            var option = options.FirstOrDefault(o => o.Id == id);
            if (option != null)
            {
                option.Value = true;
            }
        }

        internal Task TestWithPickMembersDialogAsync(
            string initialMarkup,
            string expectedMarkup,
            string[] chosenSymbols,
            Action<ImmutableArray<PickMembersOption>> optionsCallback = null,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default)
        {
            var pickMembersService = new TestPickMembersService(chosenSymbols.AsImmutableOrNull(), optionsCallback);
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup,
                index, priority,
                parameters.WithFixProviderData(pickMembersService));
        }
    }
}
