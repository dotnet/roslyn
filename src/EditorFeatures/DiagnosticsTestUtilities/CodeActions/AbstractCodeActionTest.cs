// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

public abstract partial class AbstractCodeActionTest : AbstractCodeActionOrUserDiagnosticTest
{
    protected abstract CodeRefactoringProvider CreateCodeRefactoringProvider(
        EditorTestWorkspace workspace, TestParameters parameters);

    protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
        EditorTestWorkspace workspace, TestParameters parameters = null)
    {
        parameters ??= TestParameters.Default;

        var (document, span, annotation) = await GetDocumentAndSelectSpanOrAnnotatedSpan(workspace);

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
            refactoring.Provider, document, span, fixAllScope.Value.ToRefactorAllScope()).ConfigureAwait(false);
        if (fixAllCodeAction == null)
            return ([], null);

        return (ImmutableArray.Create(fixAllCodeAction), fixAllCodeAction);
    }

    private static async Task<CodeAction> GetFixAllFixAsync(
        CodeAction originalCodeAction,
        CodeRefactoringProvider provider,
        Document document,
        TextSpan selectionSpan,
        RefactorAllScope scope)
    {
        var refactorAllProvider = provider.GetRefactorAllProvider();
        if (refactorAllProvider == null || !refactorAllProvider.GetSupportedRefactorAllScopes().Contains(scope))
            return null;

        var refactorAllState = new RefactorAllState(refactorAllProvider, document, selectionSpan, provider, scope, originalCodeAction);
        var refactorAllContext = new RefactorAllContext(refactorAllState, CodeAnalysisProgress.None, CancellationToken.None);
        return await refactorAllProvider.GetRefactoringAsync(refactorAllContext).ConfigureAwait(false);
    }

    protected override async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(EditorTestWorkspace workspace, TestParameters parameters)
        => [];

    internal override async Task<CodeRefactoring> GetCodeRefactoringAsync(
        EditorTestWorkspace workspace, TestParameters parameters)
    {
        var (document, span, _) = await GetDocumentAndSelectSpanOrAnnotatedSpan(workspace);
        return await GetCodeRefactoringAsync(document, span, workspace, parameters).ConfigureAwait(false);
    }

    internal async Task<CodeRefactoring> GetCodeRefactoringAsync(
        Document document,
        TextSpan selectedOrAnnotatedSpan,
        EditorTestWorkspace workspace,
        TestParameters parameters)
    {
        var provider = CreateCodeRefactoringProvider(workspace, parameters);

        var actions = ArrayBuilder<(CodeAction, TextSpan?)>.GetInstance();

        var context = new CodeRefactoringContext(document, selectedOrAnnotatedSpan, (a, t) => actions.Add((a, t)), CancellationToken.None);
        await provider.ComputeRefactoringsAsync(context);
        var result = actions.Count > 0 ? new CodeRefactoring(provider, actions.ToImmutable(), FixAllProviderInfo.Create(provider)) : null;
        actions.Free();
        return result;
    }

    protected async Task TestActionOnLinkedFiles(
        EditorTestWorkspace workspace,
        string expectedText,
        CodeAction action,
        string expectedPreviewContents = null)
    {
        var operations = await VerifyActionAndGetOperationsAsync(workspace, action);

        await VerifyPreviewContents(workspace, expectedPreviewContents, operations);

        var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
        await applyChangesOperation.TryApplyAsync(workspace, workspace.CurrentSolution, CodeAnalysisProgress.None, CancellationToken.None);

        foreach (var document in workspace.Documents)
        {
            var fixedRoot = await workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync();
            var actualText = fixedRoot.ToFullString();
            Assert.Equal(expectedText, actualText);
        }
    }

    private static async Task VerifyPreviewContents(
        EditorTestWorkspace workspace, string expectedPreviewContents,
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

    protected static Document GetDocument(EditorTestWorkspace workspace)
        => workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

    internal static void EnableOptions(
        ImmutableArray<PickMembersOption> options,
        params ReadOnlySpan<string> ids)
    {
        foreach (var id in ids)
        {
            EnableOption(options, id);
        }
    }

    internal static void EnableOption(ImmutableArray<PickMembersOption> options, string id)
    {
        var option = options.FirstOrDefault(o => o.Id == id);
        option?.Value = true;
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
        return TestInRegularAndScriptAsync(
            initialMarkup, expectedMarkup,
            index,
            ps.WithFixProviderData(pickMembersService));
    }
}
