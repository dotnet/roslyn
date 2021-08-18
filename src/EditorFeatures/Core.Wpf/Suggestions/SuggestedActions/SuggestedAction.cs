// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Base class for all Roslyn light bulb menu items.
    /// </summary>
    internal abstract partial class SuggestedAction : ForegroundThreadAffinitizedObject, ISuggestedAction3, IEquatable<ISuggestedAction>
    {
        protected readonly SuggestedActionsSourceProvider SourceProvider;

        protected readonly Workspace Workspace;
        protected readonly ITextBuffer SubjectBuffer;

        protected readonly object Provider;
        internal readonly CodeAction CodeAction;

        private bool _isApplied;

        private ICodeActionEditHandlerService EditHandler => SourceProvider.EditHandler;

        internal SuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            object provider,
            CodeAction codeAction)
            : base(threadingContext)
        {
            Contract.ThrowIfNull(provider);
            Contract.ThrowIfNull(codeAction);

            SourceProvider = sourceProvider;
            Workspace = workspace;
            SubjectBuffer = subjectBuffer;
            Provider = provider;
            CodeAction = codeAction;
        }

        internal virtual CodeActionPriority Priority => CodeAction.Priority;

        internal bool IsForCodeQualityImprovement
            => (Provider as SyntaxEditorBasedCodeFixProvider)?.CodeFixCategory == CodeFixCategory.CodeQuality;

        public virtual bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = CodeAction.GetType().GetTelemetryId();
            return true;
        }

        // NOTE: We want to avoid computing the operations on the UI thread. So we use Task.Run() to do this work on the background thread.
        protected Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return Task.Run(
                () => CodeAction.GetOperationsAsync(progressTracker, cancellationToken), cancellationToken);
        }

        protected static Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(CodeActionWithOptions actionWithOptions, object options, CancellationToken cancellationToken)
        {
            return Task.Run(
                () => actionWithOptions.GetOperationsAsync(options, cancellationToken), cancellationToken);
        }

        protected Task<ImmutableArray<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                () => CodeAction.GetPreviewOperationsAsync(cancellationToken), cancellationToken);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var token = SourceProvider.OperationListener.BeginAsyncOperation($"{nameof(SuggestedAction)}.{nameof(Invoke)}");
            InvokeAsync(cancellationToken).CompletesAsyncOperation(token);
        }

        private async Task InvokeAsync(CancellationToken cancellationToken)
        {
            try
            {
                // If we want to report progress, we need to create a scope inside the context -- the main context itself doesn't have a way
                // to report progress. We pass the same allow cancellation and message so otherwise nothing changes.
                using var context = SourceProvider.UIThreadOperationExecutor.BeginExecute(CodeAction.Title, CodeAction.Message, allowCancellation: true, showProgress: true);
                using var scope = context.AddScope(allowCancellation: true, CodeAction.Message);
                using var combinedCancellationToken = cancellationToken.CombineWith(context.UserCancellationToken);

                await InnerInvokeAsync(new UIThreadOperationContextProgressTracker(scope), combinedCancellationToken.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        }

        public void Invoke(IUIThreadOperationContext context)
        {
            var token = SourceProvider.OperationListener.BeginAsyncOperation($"{nameof(SuggestedAction)}.{nameof(Invoke)}");
            InvokeAsync(context).CompletesAsyncOperation(token);
        }

        private async Task InvokeAsync(IUIThreadOperationContext context)
        {
            try
            {
                using var scope = context.AddScope(allowCancellation: true, CodeAction.Message);
                await this.InnerInvokeAsync(new UIThreadOperationContextProgressTracker(scope), context.UserCancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        }

        protected virtual async Task InnerInvokeAsync(IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var snapshot = SubjectBuffer.CurrentSnapshot;
            using (new CaretPositionRestorer(SubjectBuffer, EditHandler.AssociatedViewService))
            {
                Document getFromDocument() => SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                await InvokeCoreAsync(getFromDocument, progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task InvokeCoreAsync(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var extensionManager = Workspace.Services.GetService<IExtensionManager>();
            await extensionManager.PerformActionAsync(Provider, async () =>
            {
                await InvokeWorkerAsync(getFromDocument, progressTracker, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task InvokeWorkerAsync(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IEnumerable<CodeActionOperation> operations = null;
            if (CodeAction is CodeActionWithOptions actionWithOptions)
            {
                var options = actionWithOptions.GetOptions(cancellationToken);
                if (options != null)
                    operations = await GetOperationsAsync(actionWithOptions, options, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                operations = await GetOperationsAsync(progressTracker, cancellationToken).ConfigureAwait(true);
            }

            AssertIsForeground();

            if (operations != null)
            {
                // Clear the progress we showed while computing the action.
                // We'll now show progress as we apply the action.
                progressTracker.Clear();

                using (Logger.LogBlock(
                    FunctionId.CodeFixes_ApplyChanges, KeyValueLogMessage.Create(LogType.UserAction, m => CreateLogProperties(m)), cancellationToken))
                {
                    _isApplied = await EditHandler.ApplyAsync(Workspace, getFromDocument(),
                        operations.ToImmutableArray(), CodeAction.Title,
                        progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private void CreateLogProperties(Dictionary<string, object> map)
        {
            // set various correlation info
            if (CodeAction is FixSomeCodeAction fixSome)
            {
                // fix all correlation info
                map[FixAllLogger.CorrelationId] = fixSome.FixAllState.CorrelationId;
                map[FixAllLogger.FixAllScope] = fixSome.FixAllState.Scope.ToString();
            }

            if (TryGetTelemetryId(out var telemetryId))
            {
                // Lightbulb correlation info
                map["TelemetryId"] = telemetryId.ToString();
            }

            if (this is ITelemetryDiagnosticID<string> diagnosticId)
            {
                // save what it is actually fixing
                map["DiagnosticId"] = diagnosticId.GetDiagnosticID();
            }
        }

        public string DisplayText
        {
            get
            {
                // Underscores will become an accelerator in the VS smart tag.  So we double all
                // underscores so they actually get represented as an underscore in the UI.
                var extensionManager = Workspace.Services.GetService<IExtensionManager>();
                var text = extensionManager.PerformFunction(Provider, () => CodeAction.Title, defaultValue: string.Empty);
                return text.Replace("_", "__");
            }
        }

        public string DisplayTextSuffix => "";

        protected async Task<SolutionPreviewResult> GetPreviewResultAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We will always invoke this from the UI thread.
            AssertIsForeground();

            // We use ConfigureAwait(true) to stay on the UI thread.
            var operations = await GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(true);

            return EditHandler.GetPreviews(Workspace, operations, cancellationToken);
        }

        public virtual bool HasPreview => false;

        public virtual Task<object> GetPreviewAsync(CancellationToken cancellationToken)
            => SpecializedTasks.Null<object>();

        public virtual bool HasActionSets => false;

        public virtual Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => SpecializedTasks.EmptyEnumerable<SuggestedActionSet>();

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
            => Equals(other as SuggestedAction);

        public override bool Equals(object obj)
            => Equals(obj as SuggestedAction);

        internal bool Equals(SuggestedAction otherSuggestedAction)
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

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly SuggestedAction _suggestedAction;

            public TestAccessor(SuggestedAction suggestedAction)
                => _suggestedAction = suggestedAction;

            public ref bool IsApplied => ref _suggestedAction._isApplied;
        }
    }
}
