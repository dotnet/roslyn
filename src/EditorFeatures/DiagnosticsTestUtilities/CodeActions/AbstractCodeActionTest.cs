// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract partial class AbstractCodeActionTest : AbstractCodeActionOrUserDiagnosticTest
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
            => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

        internal async Task<CodeRefactoring> GetCodeRefactoringAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var provider = CreateCodeRefactoringProvider(workspace, parameters);

            var documentsWithSelections = workspace.Documents.Where(d => !d.IsLinkFile && d.SelectedSpans.Count == 1);
            Debug.Assert(documentsWithSelections.Count() == 1, "One document must have a single span annotation");
            var span = documentsWithSelections.Single().SelectedSpans.Single();
            var actions = ArrayBuilder<(CodeAction, TextSpan?)>.GetInstance();
            var document = workspace.CurrentSolution.GetDocument(documentsWithSelections.Single().Id);
            var context = new CodeRefactoringContext(document, span, (a, t) => actions.Add((a, t)), parameters.codeActionOptions, CancellationToken.None);
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
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, default);

            await VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            await applyChangesOperation.TryApplyAsync(workspace, new ProgressTracker(), CancellationToken.None);

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
                var previews = await editHandler.GetPreviewsAsync(workspace, operations, CancellationToken.None);
                var content = (await previews.GetPreviewsAsync())[0];
                var diffView = content as DifferenceViewerPreview;
                Assert.NotNull(diffView.Viewer);
                var previewContents = diffView.Viewer.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                diffView.Dispose();

                Assert.Equal(expectedPreviewContents, previewContents);
            }
        }

        protected static Document GetDocument(TestWorkspace workspace)
            => workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

        internal static void EnableOptions(
            ImmutableArray<PickMembersOption> options,
            params string[] ids)
        {
            foreach (var id in ids)
            {
                EnableOption(options, id);
            }
        }

        internal static void EnableOption(ImmutableArray<PickMembersOption> options, string id)
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
            TestParameters? parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            var pickMembersService = new TestPickMembersService(chosenSymbols.AsImmutableOrNull(), optionsCallback);
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup,
                index,
                ps.WithFixProviderData(pickMembersService));
        }
    }

    [ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Host), Shared, PartNotDiscoverable]
    internal class TestPickMembersService : IPickMembersService
    {
        public ImmutableArray<string> MemberNames;
        public Action<ImmutableArray<PickMembersOption>> OptionsCallback;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestPickMembersService()
        {
        }

#pragma warning disable RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'
        public TestPickMembersService(
            ImmutableArray<string> memberNames,
            Action<ImmutableArray<PickMembersOption>> optionsCallback)
        {
            MemberNames = memberNames;
            OptionsCallback = optionsCallback;
        }
#pragma warning restore RS0034 // Exported parts should be marked with 'ImportingConstructorAttribute'

        public PickMembersResult PickMembers(
            string title,
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options,
            bool selectAll)
        {
            OptionsCallback?.Invoke(options);
            return new PickMembersResult(
                MemberNames.IsDefault
                    ? members
                    : MemberNames.SelectAsArray(n => members.Single(m => m.Name == n)),
                options,
                selectAll);
        }
    }
}
