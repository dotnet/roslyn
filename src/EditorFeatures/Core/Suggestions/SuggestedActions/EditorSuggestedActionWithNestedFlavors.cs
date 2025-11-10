// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Type for all SuggestedActions that have 'flavors'.  'Flavors' are child actions that are presented as simple links,
/// not as menu-items, in the light-bulb.  Examples of 'flavors' include 'preview changes' (for refactorings and fixes)
/// and 'fix all in document, project, solution' (for refactorings and fixes).
/// 
/// Supports 'preview changes' for all changes.
/// </summary>
internal sealed partial class EditorSuggestedActionWithNestedFlavors(
    IThreadingContext threadingContext,
    SuggestedActionsSourceProvider sourceProvider,
    TextDocument originalDocument,
    ITextBuffer subjectBuffer,
    object provider,
    CodeAction codeAction,
    SuggestedActionSet? fixAllFlavors,
    ImmutableArray<Diagnostic> diagnostics)
    : EditorSuggestedAction(threadingContext,
        sourceProvider,
        originalDocument.Project.Solution,
        subjectBuffer,
        provider,
        codeAction), ISuggestedActionWithFlavors, ITelemetryDiagnosticID<string?>
{
    private readonly SuggestedActionSet? _fixAllFlavors = fixAllFlavors;
    private readonly ImmutableArray<Diagnostic> _diagnostics = diagnostics;

    private ImmutableArray<SuggestedActionSet> _nestedFlavors;

    public TextDocument OriginalDocument { get; } = originalDocument;

    /// <summary>
    /// HasActionSets is always true because we always know we provide 'preview changes'.
    /// </summary>
    public sealed override bool HasActionSets => true;

    public sealed override async Task<IEnumerable<SuggestedActionSet>?> GetActionSetsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Light bulb will always invoke this property on the UI thread.
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (_nestedFlavors.IsDefault)
        {
            var extensionManager = this.OriginalSolution.Services.GetRequiredService<IExtensionManager>();

            // Note: We must ensure that CreateAllFlavorsAsync does not perform any expensive
            // long running operations as it will be invoked when a lightbulb preview is brought
            // up for any code action. Currently, the only async method call within CreateAllFlavorsAsync
            // is made within 'RefineUsingCopilotSuggestedAction.TryCreateAsync', which needs to
            // check if Copilot service is available using a relatively cheap, but async method call.
            _nestedFlavors = await extensionManager.PerformFunctionAsync(
                Provider, CreateAllFlavorsAsync,
                defaultValue: [], cancellationToken).ConfigureAwait(false);
        }

        Contract.ThrowIfTrue(_nestedFlavors.IsDefault);
        return _nestedFlavors;
    }

    private async Task<ImmutableArray<SuggestedActionSet>> CreateAllFlavorsAsync(CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SuggestedActionSet>.GetInstance(out var builder);

        builder.Add(await GetPrimarySuggestedActionSetAsync(cancellationToken).ConfigureAwait(false));
        builder.AddIfNotNull(_fixAllFlavors);

        return builder.ToImmutableAndClear();
    }

    private async Task<SuggestedActionSet> GetPrimarySuggestedActionSetAsync(CancellationToken cancellationToken)
    {
        // In this method we add all the primary flavored suggested actions that need to show up
        // as hyperlinks on the lightbulb preview pane for all code actions.
        //  - We always add the 'Preview Changes' suggested action.
        //  - We add the 'Refine using Copilot' suggested action, if certain conditions are met. See comments
        //    inside 'RefineUsingCopilotSuggestedAction.TryCreateAsync' for details.
        //  - We add the custom suggested actions corresponding to the additional flavored actions defined
        //    by the underlying code action.
        // Note that flavored suggested actions for Fix All operations are added in a separate
        // suggested action set by our caller, we don't add them here.

        using var _ = ArrayBuilder<EditorSuggestedAction>.GetInstance(out var suggestedActions);
        var previewChangesAction = CreateTrivialAction(
            this, new PreviewChangesCodeAction(this.CodeAction, this.GetPreviewResultAsync));
        suggestedActions.Add(previewChangesAction);

        var refineUsingCopilotAction = await TryCreateRefineSuggestedActionAsync().ConfigureAwait(false);
        if (refineUsingCopilotAction != null)
            suggestedActions.Add(refineUsingCopilotAction);

        foreach (var action in this.CodeAction.AdditionalPreviewFlavors)
            suggestedActions.Add(CreateTrivialAction(this, action));

        return new SuggestedActionSet(categoryName: null, actions: suggestedActions.ToImmutable());

        async Task<EditorSuggestedAction?> TryCreateRefineSuggestedActionAsync()
        {
            if (this.OriginalDocument is not Document originalDocument)
                return null;

            if (originalDocument.GetLanguageService<ICopilotOptionsService>() is not { } optionsService ||
                 await optionsService.IsRefineOptionEnabledAsync().ConfigureAwait(false) is false)
            {
                return null;
            }

            if (originalDocument.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
                await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
            {
                return null;
            }

            return CreateTrivialAction(
                this, new RefineUsingCopilotCodeAction(
                    this.OriginalSolution, this.CodeAction, _diagnostics.FirstOrDefault(), copilotService));
        }
    }

    // HasPreview is called synchronously on the UI thread. In order to avoid blocking the UI thread,
    // we need to provide a 'quick' answer here as opposed to the 'right' answer. Providing the 'right'
    // answer is expensive (because we will need to call CodeAction.GetPreviewOperationsAsync() for this
    // and this will involve computing the changed solution for the ApplyChangesOperation for the fix /
    // refactoring). So we always return 'true' here (so that platform will call GetActionSetsAsync()
    // below). Platform guarantees that nothing bad will happen if we return 'true' here and later return
    // 'null' / empty collection from within GetPreviewAsync().
    public override bool HasPreview => true;

    public override async Task<object?> GetPreviewAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Light bulb will always invoke this function on the UI thread.
        this.ThreadingContext.ThrowIfNotOnUIThread();

        var previewPaneService = this.OriginalSolution.Services.GetService<IPreviewPaneService>();
        if (previewPaneService == null)
        {
            return null;
        }

        // after this point, this method should only return at GetPreviewPane. otherwise, DifferenceViewer will leak
        // since there is no one to close the viewer
        var preferredDocumentId = this.OriginalDocument.Id;
        var preferredProjectId = preferredDocumentId.ProjectId;

        var extensionManager = this.OriginalSolution.Services.GetRequiredService<IExtensionManager>();
        var previewContents = await extensionManager.PerformFunctionAsync(Provider, async cancellationToken =>
        {
            // We need to stay on UI thread after GetPreviewResultAsync() so that TakeNextPreviewAsync()
            // below can execute on UI thread. We use ConfigureAwait(true) to stay on the UI thread.
            var previewResult = await GetPreviewResultAsync(cancellationToken).ConfigureAwait(true);
            if (previewResult == null)
            {
                return null;
            }
            else
            {
                // TakeNextPreviewAsync() needs to run on UI thread.
                this.ThreadingContext.ThrowIfNotOnUIThread();
                return await previewResult.GetPreviewsAsync(preferredDocumentId, preferredProjectId, cancellationToken).ConfigureAwait(true);
            }

            // GetPreviewPane() below needs to run on UI thread. We use ConfigureAwait(true) to stay on the UI thread.
        }, defaultValue: null, cancellationToken).ConfigureAwait(true);

        // GetPreviewPane() needs to run on the UI thread.
        this.ThreadingContext.ThrowIfNotOnUIThread();

        var diagnosticData = DiagnosticData.Create(_diagnostics.FirstOrDefault(), this.OriginalDocument.Project);
        return previewPaneService.GetPreviewPane(diagnosticData, previewContents!);
    }

    public string? GetDiagnosticID()
        => _diagnostics.FirstOrDefault()?.GetTelemetryDiagnosticID();
}
