// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
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

        private ICodeActionEditHandlerService EditHandler => SourceProvider.EditHandler;

        internal SuggestedAction(
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            object provider,
            CodeAction codeAction)
        {
            Contract.ThrowIfNull(provider);
            Contract.ThrowIfNull(codeAction);

            this.SourceProvider = sourceProvider;
            this.Workspace = workspace;
            this.SubjectBuffer = subjectBuffer;
            this.Provider = provider;
            this.CodeAction = codeAction;
        }

        internal virtual CodeActionPriority Priority => CodeAction.Priority;

        protected static int GetTelemetryPrefix(CodeAction codeAction)
        {
            // AssemblyQualifiedName will change across version numbers, FullName won't
            var type = codeAction.GetType();
            type = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
            return type.FullName.GetHashCode();
        }

        public virtual bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = new Guid(GetTelemetryPrefix(this.CodeAction), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
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
            this.AssertIsForeground();
            
            // Create a task to do the actual async invocation of this action.
            // For testing purposes mark that we still have an outstanding async 
            // operation so that we don't try to validate things too soon.
            var asyncToken = SourceProvider.OperationListener.BeginAsyncOperation(GetType().Name + "." + nameof(Invoke));
            var task = YieldThenInvokeAsync(cancellationToken);
            task.CompletesAsyncOperation(asyncToken);
        }

        private async Task YieldThenInvokeAsync(CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // Always wrap whatever we're doing in a threaded wait dialog.
            using (var context = this.SourceProvider.WaitIndicator.StartWait(CodeAction.Title, CodeAction.Message, allowCancel: true, showProgress: true))
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationToken))
            {
                // Yield the UI thread so that the light bulb can be dismissed.  This is necessary
                // as some code actions may be long running, and we don't want the light bulb to
                // stay on screen.
                await Task.Yield();

                this.AssertIsForeground();

                // Then proceed and actually do the invoke.
                await InvokeAsync(context.ProgressTracker, linkedSource.Token).ConfigureAwait(true);
            }
        }

        protected virtual async Task InvokeAsync( 
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            var snapshot = this.SubjectBuffer.CurrentSnapshot;

            using (new CaretPositionRestorer(this.SubjectBuffer, this.EditHandler.AssociatedViewService))
            {
                Func<Document> getFromDocument = () => this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                await InvokeCoreAsync(getFromDocument, progressTracker, cancellationToken).ConfigureAwait(true);
            }
        }

        protected async Task InvokeCoreAsync(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
            await extensionManager.PerformActionAsync(Provider, async () =>
            {
                await InvokeWorkerAsync(getFromDocument, progressTracker, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(true);
        }

        private async Task InvokeWorkerAsync(
            Func<Document> getFromDocument, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            IEnumerable<CodeActionOperation> operations = null;

            // NOTE: As mentioned above, we want to avoid computing the operations on the UI thread.
            // However, for CodeActionWithOptions, GetOptions() might involve spinning up a dialog
            // to compute the options and must be done on the UI thread.
            var actionWithOptions = this.CodeAction as CodeActionWithOptions;
            if (actionWithOptions != null)
            {
                var options = actionWithOptions.GetOptions(cancellationToken);
                if (options != null)
                {
                    // ConfigureAwait(true) so we come back to the same thread as 
                    // we do all application on the UI thread.
                    operations = await GetOperationsAsync(actionWithOptions, options, cancellationToken).ConfigureAwait(true);
                    this.AssertIsForeground();
                }
            }
            else
            {
                // ConfigureAwait(true) so we come back to the same thread as 
                // we do all application on the UI thread.
                operations = await GetOperationsAsync(progressTracker, cancellationToken).ConfigureAwait(true);
                this.AssertIsForeground();
            }

            if (operations != null)
            {
                // Clear the progress we showed while computing the action.
                // We'll now show progress as we apply the action.
                progressTracker.Clear();

                // ConfigureAwait(true) so we come back to the same thread as 
                // we do all application on the UI thread.
                await EditHandler.ApplyAsync(Workspace, getFromDocument(), 
                    operations.ToImmutableArray(), CodeAction.Title, 
                    progressTracker, cancellationToken).ConfigureAwait(true);
            }
        }

        public string DisplayText
        {
            get
            {
                // Underscores will become an accelerator in the VS smart tag.  So we double all
                // underscores so they actually get represented as an underscore in the UI.
                var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
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
                if (CodeAction.Glyph.HasValue)
                {
                    var imageService = Workspace.Services.GetService<IImageMonikerService>();
                    return imageService.GetImageMoniker((Glyph)CodeAction.Glyph.Value);
                }

                return default(ImageMoniker);
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
    }
}