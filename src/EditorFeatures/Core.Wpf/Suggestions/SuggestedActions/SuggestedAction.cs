// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Base class for all Roslyn light bulb menu items.
    /// </summary>
    internal abstract partial class SuggestedAction : ForegroundThreadAffinitizedObject, ISuggestedAction, IEquatable<ISuggestedAction>
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

        protected Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(CodeActionWithOptions actionWithOptions, object options, CancellationToken cancellationToken)
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
            // While we're not technically doing anything async here, we need to let the
            // integration test harness know that it should not proceed until all this
            // work is done.  Otherwise it might ask to do some work before we finish.
            // That can happen because although we're on the UI thread, we may do things
            // that end up causing VS to pump the messages that the test harness enqueues
            // to the UI thread as well.
            using (SourceProvider.OperationListener.BeginAsyncOperation($"{nameof(SuggestedAction)}.{nameof(Invoke)}"))
            {
                // WaitIndicator cannot be used with async/await. Even though we call async methods
                // later in this call chain, do not await them.
                SourceProvider.WaitIndicator.Wait(CodeAction.Title, CodeAction.Message, allowCancel: true, showProgress: true, action: waitContext =>
                {
                    using var combinedCancellationToken = cancellationToken.CombineWith(waitContext.CancellationToken);
                    InnerInvoke(waitContext.ProgressTracker, combinedCancellationToken.Token);
                    foreach (var actionCallback in SourceProvider.ActionCallbacks)
                    {
                        actionCallback.Value.OnSuggestedActionExecuted(this);
                    }
                });
            }
        }

        protected virtual void InnerInvoke(IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var snapshot = SubjectBuffer.CurrentSnapshot;
            using (new CaretPositionRestorer(SubjectBuffer, EditHandler.AssociatedViewService))
            {
                Document getFromDocument() => SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                InvokeCore(getFromDocument, progressTracker, cancellationToken);
            }
        }

        protected void InvokeCore(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var extensionManager = Workspace.Services.GetService<IExtensionManager>();
            extensionManager.PerformAction(Provider, () =>
            {
                InvokeWorker(getFromDocument, progressTracker, cancellationToken);
            });
        }

        private void InvokeWorker(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            AssertIsForeground();
            IEnumerable<CodeActionOperation> operations = null;
            if (CodeAction is CodeActionWithOptions actionWithOptions)
            {
                var options = actionWithOptions.GetOptions(cancellationToken);
                if (options != null)
                {
                    // Note: we want to block the UI thread here so the user cannot modify anything while the codefix applies
                    operations = GetOperationsAsync(actionWithOptions, options, cancellationToken).WaitAndGetResult(cancellationToken);
                }
            }
            else
            {
                // Note: we want to block the UI thread here so the user cannot modify anything while the codefix applies
                operations = GetOperationsAsync(progressTracker, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            if (operations != null)
            {
                // Clear the progress we showed while computing the action.
                // We'll now show progress as we apply the action.
                progressTracker.Clear();

                using (Logger.LogBlock(
                    FunctionId.CodeFixes_ApplyChanges, KeyValueLogMessage.Create(LogType.UserAction, m => CreateLogProperties(m)), cancellationToken))
                {
                    // Note: we want to block the UI thread here so the user cannot modify anything while the codefix applies
                    _isApplied = EditHandler.Apply(Workspace, getFromDocument(),
                        operations.ToImmutableArray(), CodeAction.Title,
                        progressTracker, cancellationToken);
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
            => SpecializedTasks.Default<object>();

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
                    foreach (var service in SourceProvider.ImageMonikerServices)
                    {
                        if (service.Value.TryGetImageMoniker(tags, out var moniker) && !moniker.Equals(default(ImageMoniker)))
                        {
                            return moniker;
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
