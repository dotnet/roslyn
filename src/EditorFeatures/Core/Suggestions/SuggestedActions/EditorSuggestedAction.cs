// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Base class for all Roslyn light bulb menu items.
/// </summary>
internal abstract partial class EditorSuggestedAction(
    IThreadingContext threadingContext,
    SuggestedActionsSourceProvider sourceProvider,
    Solution originalSolution,
    ITextBuffer subjectBuffer,
    object provider,
    CodeAction codeAction) : ISuggestedAction3, IEquatable<ISuggestedAction>
{
    protected readonly IThreadingContext ThreadingContext = threadingContext;
    protected readonly SuggestedActionsSourceProvider SourceProvider = sourceProvider;

    protected readonly Solution OriginalSolution = originalSolution;
    protected readonly ITextBuffer SubjectBuffer = subjectBuffer;

    protected readonly object Provider = provider;
    internal CodeAction CodeAction { get; } = codeAction;

    private ICodeActionEditHandlerService EditHandler => SourceProvider.EditHandler;

    public virtual bool TryGetTelemetryId(out Guid telemetryId)
    {
        telemetryId = CodeAction.GetTelemetryId();
        return true;
    }

    protected async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // Avoid computing the operations on the UI thread
        await TaskScheduler.Default;
        return await CodeAction.GetOperationsAsync(this.OriginalSolution, progressTracker, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(
        CodeActionWithOptions actionWithOptions, object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // Avoid computing the operations on the UI thread
        await TaskScheduler.Default;
        return await actionWithOptions.GetOperationsAsync(this.OriginalSolution, options, progressTracker, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<ImmutableArray<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken)
    {
        // Avoid computing the operations on the UI thread
        await TaskScheduler.Default;
        return await CodeAction.GetPreviewOperationsAsync(this.OriginalSolution, cancellationToken).ConfigureAwait(false);
    }

    public void Invoke(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Invoke(CancellationToken) is no longer supported. Use Invoke(IUIThreadOperationContext) instead.");
    }

    public void Invoke(IUIThreadOperationContext context)
    {
        // we're going to return immediately from Invoke and kick off our own async work to invoke the
        // code action. Once this returns, the editor will close the threaded wait dialog it created.
        // So we need to take ownership of it and start our own TWD instead to track this.
        context.TakeOwnership();

        _ = InvokeAsync();
    }

    private async Task InvokeAsync()
    {
        try
        {
            using var _ = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.SuggestedAction_Application_Summary, $"Total");

            using var token = SourceProvider.OperationListener.BeginAsyncOperation($"{nameof(EditorSuggestedAction)}.{nameof(Invoke)}");
            using var context = SourceProvider.UIThreadOperationExecutor.BeginExecute(
                EditorFeaturesResources.Execute_Suggested_Action, CodeAction.Title, allowCancellation: true, showProgress: true);
            using var scope = context.AddScope(allowCancellation: true, CodeAction.Message);
            await this.InnerInvokeAsync(scope.GetCodeAnalysisProgress(), context.UserCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
        {
        }
    }

    protected virtual async Task InnerInvokeAsync(IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        using (new CaretPositionRestorer(SubjectBuffer, EditHandler.AssociatedViewService))
        {
            // ConfigureAwait(true) so that CaretPositionRestorer.Dispose runs on the UI thread.
            await this.OriginalSolution.Services.GetService<IExtensionManager>().PerformActionAsync(
                Provider, () => InvokeWorkerAsync(progressTracker, cancellationToken)).ConfigureAwait(true);
        }
    }

    private async Task InvokeWorkerAsync(IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        IEnumerable<CodeActionOperation> operations = null;
        if (CodeAction is CodeActionWithOptions actionWithOptions)
        {
            var options = actionWithOptions.GetOptions(cancellationToken);
            if (options != null)
                operations = await GetOperationsAsync(actionWithOptions, options, progressTracker, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            operations = await GetOperationsAsync(progressTracker, cancellationToken).ConfigureAwait(true);
        }

        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (operations != null)
        {
            // Clear the progress we showed while computing the action.
            // We'll now show progress as we apply the action.
            progressTracker.Report(CodeAnalysisProgress.Clear());
            progressTracker.Report(CodeAnalysisProgress.Description(EditorFeaturesResources.Applying_changes));

            using (Logger.LogBlock(
                FunctionId.CodeFixes_ApplyChanges, KeyValueLogMessage.Create(
                    LogType.UserAction, static (m, @this) => @this.CreateLogProperties(m), this), cancellationToken))
            {
                var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

                await EditHandler.ApplyAsync(
                    this.OriginalSolution.Workspace,
                    this.OriginalSolution,
                    document,
                    [.. operations],
                    CodeAction.Title,
                    progressTracker,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void CreateLogProperties(Dictionary<string, object> map)
    {
        // set various correlation info
        if (CodeAction is RefactorOrFixAllCodeAction fixSome)
        {
            // fix all correlation info
            map[FixAllLogger.CorrelationId] = fixSome.RefactorOrFixAllState.CorrelationId;
            map[FixAllLogger.FixAllScope] = fixSome.RefactorOrFixAllState.Scope.ToString();
        }

        if (TryGetTelemetryId(out var telemetryId))
        {
            // Lightbulb correlation info
            map["TelemetryId"] = telemetryId.ToString();
        }

        if (this is ITelemetryDiagnosticID<string> telemetry && telemetry.GetDiagnosticID() is string diagnosticId)
        {
            // save what it is actually fixing
            map["DiagnosticId"] = diagnosticId;
        }
    }

    public string DisplayText
    {
        get
        {
            // Underscores will become an accelerator in the VS smart tag.  So we double all
            // underscores so they actually get represented as an underscore in the UI.
            var extensionManager = this.OriginalSolution.Services.GetService<IExtensionManager>();
            var text = extensionManager.PerformFunction(Provider, () => CodeAction.Title, defaultValue: string.Empty);
            return text.Replace("_", "__");
        }
    }

    public string DisplayTextSuffix => "";

    protected async Task<SolutionPreviewResult> GetPreviewResultAsync(CancellationToken cancellationToken)
    {
        var operations = await GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(true);

        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return await EditHandler.GetPreviewsAsync(
            this.OriginalSolution.Workspace, operations, cancellationToken).ConfigureAwait(true);
    }

    public virtual bool HasPreview => false;

    public virtual async Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        => null;

    public virtual bool HasActionSets => false;

    public virtual async Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        => [];

    #region not supported

    void IDisposable.Dispose()
    {
        // do nothing
    }

    // same as display text
    string ISuggestedAction.IconAutomationText => DisplayText;

    ImageMoniker ISuggestedAction.IconMoniker
    {
        get
        {
            var tags = CodeAction.Tags;
            if (tags.Length > 0)
            {
                foreach (var service in SourceProvider.ImageIdServices)
                {
                    if (service.Value.TryGetImageId(tags, out var imageId) && !imageId.Equals(default(ImageId)))
                    {
                        // Not using the extension method because it's not available in Cocoa
                        return new ImageMoniker
                        {
                            Guid = imageId.Guid,
                            Id = imageId.Id
                        };
                    }
                }
            }

            return default;
        }
    }

    // no shortcut support
    string ISuggestedAction.InputGestureText => null;

    #endregion

    #region IEquatable<ISuggestedAction>

    public bool Equals(ISuggestedAction other)
        => Equals(other as EditorSuggestedAction);

    public override bool Equals(object obj)
        => Equals(obj as EditorSuggestedAction);

    internal bool Equals(EditorSuggestedAction otherSuggestedAction)
    {
        if (otherSuggestedAction == null)
        {
            return false;
        }

        if (this == otherSuggestedAction)
        {
            return true;
        }

        if (Provider != otherSuggestedAction.Provider)
        {
            return false;
        }

        var otherCodeAction = otherSuggestedAction.CodeAction;
        if (CodeAction.EquivalenceKey == null || otherCodeAction.EquivalenceKey == null)
        {
            return false;
        }

        return CodeAction.EquivalenceKey == otherCodeAction.EquivalenceKey;
    }

    public override int GetHashCode()
    {
        if (CodeAction.EquivalenceKey == null)
        {
            return base.GetHashCode();
        }

        return Hash.Combine(Provider.GetHashCode(), CodeAction.EquivalenceKey.GetHashCode());
    }

    #endregion

    public static EditorSuggestedAction CreateTrivialAction(
        EditorSuggestedAction action,
        CodeAction codeAction)
        => new TrivialSuggestedAction(action.ThreadingContext, action.SourceProvider, action.OriginalSolution, action.SubjectBuffer, action.Provider, codeAction);

    private sealed class TrivialSuggestedAction(
        IThreadingContext threadingContext,
        SuggestedActionsSourceProvider sourceProvider,
        Solution originalSolution,
        ITextBuffer subjectBuffer,
        object provider,
        CodeAction codeAction) : EditorSuggestedAction(threadingContext, sourceProvider, originalSolution, subjectBuffer, provider, codeAction);
}
