// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract partial class AbstractCodeActionTest_NoEditor : AbstractCodeActionOrUserDiagnosticTest_NoEditor
    {
        protected abstract CodeRefactoringProvider CreateCodeRefactoringProvider(
            TestWorkspace workspace, TestParameters parameters);

        protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters = null)
        {
            parameters ??= TestParameters.Default;

            GetDocumentAndSelectSpanOrAnnotatedSpan(workspace, out var document, out var span, out var annotation);

            var refactoring = await GetCodeRefactoringAsync(workspace, parameters);
            var actions = refactoring == null
                ? []
                : refactoring.CodeActions.Select(n => n.action).AsImmutable();
            actions = MassageActions(actions);

            var fixAllScope = GetFixAllScope(annotation);

            if (fixAllScope is FixAllScope.ContainingMember or FixAllScope.ContainingType &&
                document.GetLanguageService<IFixAllSpanMappingService>() is IFixAllSpanMappingService spanMappingService)
            {
                var documentsAndSpansToFix = await spanMappingService.GetFixAllSpansAsync(
                    document, span, fixAllScope.Value, CancellationToken.None).ConfigureAwait(false);
                if (documentsAndSpansToFix.IsEmpty)
                {
                    return ([], null);
                }
            }

            var actionToInvoke = actions.IsDefaultOrEmpty ? null : actions[parameters.index];
            if (actionToInvoke == null || fixAllScope == null)
                return (actions, actionToInvoke);

            var fixAllCodeAction = await GetFixAllFixAsync(actionToInvoke,
                refactoring.Provider, refactoring.CodeActionOptionsProvider, document, span, fixAllScope.Value).ConfigureAwait(false);
            if (fixAllCodeAction == null)
                return ([], null);

            return (ImmutableArray.Create(fixAllCodeAction), fixAllCodeAction);
        }

        private static async Task<CodeAction> GetFixAllFixAsync(
            CodeAction originalCodeAction,
            CodeRefactoringProvider provider,
            CodeActionOptionsProvider optionsProvider,
            Document document,
            TextSpan selectionSpan,
            FixAllScope scope)
        {
            var fixAllProvider = provider.GetFixAllProvider();
            if (fixAllProvider == null || !fixAllProvider.GetSupportedFixAllScopes().Contains(scope))
                return null;

            var fixAllState = new FixAllState(fixAllProvider, document, selectionSpan, provider, optionsProvider, scope, originalCodeAction);
            var fixAllContext = new FixAllContext(fixAllState, CodeAnalysisProgress.None, CancellationToken.None);
            return await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
        }

        protected override Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(TestWorkspace workspace, TestParameters parameters)
            => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

        internal override async Task<CodeRefactoring> GetCodeRefactoringAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            GetDocumentAndSelectSpanOrAnnotatedSpan(workspace, out var document, out var span, out _);
            return await GetCodeRefactoringAsync(document, span, workspace, parameters).ConfigureAwait(false);
        }

        internal async Task<CodeRefactoring> GetCodeRefactoringAsync(
            Document document,
            TextSpan selectedOrAnnotatedSpan,
            TestWorkspace workspace,
            TestParameters parameters)
        {
            var provider = CreateCodeRefactoringProvider(workspace, parameters);

            using var _ = ArrayBuilder<(CodeAction, TextSpan?)>.GetInstance(out var actions);

            var codeActionOptionsProvider = CodeActionOptions.DefaultProvider;
            var context = new CodeRefactoringContext(document, selectedOrAnnotatedSpan, (a, t) => actions.Add((a, t)), codeActionOptionsProvider, CancellationToken.None);
            await provider.ComputeRefactoringsAsync(context);
            var result = actions.Count > 0 ? new CodeRefactoring(provider, actions.ToImmutable(), FixAllProviderInfo.Create(provider), codeActionOptionsProvider) : null;
            return result;
        }

        protected async Task TestActionOnLinkedFiles(
            TestWorkspace workspace,
            string expectedText,
            CodeAction action,
            string expectedPreviewContents = null)
        {
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action);

#if false

            await VerifyPreviewContents(workspace, expectedPreviewContents, operations);

#endif

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            await applyChangesOperation.TryApplyAsync(workspace, workspace.CurrentSolution, CodeAnalysisProgress.None, CancellationToken.None);

            foreach (var document in workspace.Documents)
            {
                var fixedRoot = await workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync();
                var actualText = fixedRoot.ToFullString();
                Assert.Equal(expectedText, actualText);
            }
        }

#if false

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

#endif

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
            TestParameters parameters = null)
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
