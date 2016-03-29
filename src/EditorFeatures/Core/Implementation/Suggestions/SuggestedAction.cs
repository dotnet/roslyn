// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
    internal partial class SuggestedAction : ForegroundThreadAffinitizedObject, ISuggestedAction, IEquatable<ISuggestedAction>
    {
        protected readonly IAsynchronousOperationListener OperationListener;
        protected readonly Workspace Workspace;
        protected readonly ITextBuffer SubjectBuffer;
        protected readonly ICodeActionEditHandlerService EditHandler;

        protected readonly object Provider;
        internal readonly CodeAction CodeAction;
        private readonly ImmutableArray<SuggestedActionSet> _actionSets;
        protected readonly IWaitIndicator WaitIndicator;

        internal SuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            CodeAction codeAction,
            object provider,
            IAsynchronousOperationListener operationListener,
            IEnumerable<SuggestedActionSet> actionSets = null)
        {
            Contract.ThrowIfTrue(provider == null);

            this.Workspace = workspace;
            this.SubjectBuffer = subjectBuffer;
            this.CodeAction = codeAction;
            this.EditHandler = editHandler;
            this.WaitIndicator = waitIndicator;
            this.Provider = provider;
            OperationListener = operationListener;
            _actionSets = actionSets.AsImmutableOrEmpty();
        }

        internal virtual CodeActionPriority Priority => CodeAction?.Priority ?? CodeActionPriority.Medium;

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // TODO: this is temporary. Diagnostic team needs to figure out how to provide unique id per a fix.
            // for now, we will use type of CodeAction, but there are some predefined code actions that are used by multiple fixes
            // and this will not distinguish those

            // AssemblyQualifiedName will change across version numbers, FullName won't
            var type = CodeAction.GetType();
            type = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            telemetryId = new Guid(type.FullName.GetHashCode(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            return true;
        }

        // NOTE: We want to avoid computing the operations on the UI thread. So we use Task.Run() to do this work on the background thread.
        protected Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                async () => await CodeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
        }

        protected Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(CodeActionWithOptions actionWithOptions, object options, CancellationToken cancellationToken)
        {
            return Task.Run(
                async () => await actionWithOptions.GetOperationsAsync(options, cancellationToken).ConfigureAwait(false), cancellationToken);
        }

        protected Task<ImmutableArray<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                async () => await CodeAction.GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            
            // Create a task to do the actual async invocation of this action.
            // For testing purposes mark that we still have an outstanding async 
            // operation so that we don't try to validate things too soon.
            var asyncToken = OperationListener.BeginAsyncOperation(GetType().Name + "." + nameof(Invoke));
            var task = YieldThenInvokeAsync(cancellationToken);
            task.CompletesAsyncOperation(asyncToken);
        }

        private async Task YieldThenInvokeAsync(CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // Yield the UI thread so that the light bulb can be dismissed.  This is necessary
            // as some code actions may be long running, and we don't want the light bulb to
            // stay on screen.
            await Task.Yield();

            // Always wrap whatever we're doing in a threaded wait dialog.
            using (var context = this.WaitIndicator.StartWait(CodeAction.Title, CodeAction.Message, allowCancel: true))
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationToken))
            {
                this.AssertIsForeground();

                // Then proceed and actually do the invoke.
                await InvokeAsync(linkedSource.Token).ConfigureAwait(true);
            }
        }

        protected virtual async Task InvokeAsync(CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            var snapshot = this.SubjectBuffer.CurrentSnapshot;

            using (new CaretPositionRestorer(this.SubjectBuffer, this.EditHandler.AssociatedViewService))
            {
                Func<Document> getFromDocument = () => this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                await InvokeCoreAsync(getFromDocument, cancellationToken).ConfigureAwait(true);
            }
        }

        protected async Task InvokeCoreAsync(Func<Document> getFromDocument, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
            await extensionManager.PerformActionAsync(Provider, async () =>
            {
                await InvokeWorkerAsync(getFromDocument, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(true);
        }

        private async Task InvokeWorkerAsync(Func<Document> getFromDocument, CancellationToken cancellationToken)
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
                    operations = await GetOperationsAsync(actionWithOptions, options, cancellationToken).ConfigureAwait(true);
                    this.AssertIsForeground();
                }
            }
            else
            {
                operations = await GetOperationsAsync(cancellationToken).ConfigureAwait(true);
                this.AssertIsForeground();
            }

            if (operations != null)
            {
                EditHandler.Apply(Workspace, getFromDocument(), operations, CodeAction.Title, cancellationToken);
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

        public virtual bool HasPreview
        {
            get
            {
                // HasPreview is called synchronously on the UI thread. In order to avoid blocking the UI thread,
                // we need to provide a 'quick' answer here as opposed to the 'right' answer. Providing the 'right'
                // answer is expensive (because we will need to call CodeAction.GetPreviewOperationsAsync() for this
                // and this will involve computing the changed solution for the ApplyChangesOperation for the fix /
                // refactoring). So we always return 'true' here (so that platform will call GetActionSetsAsync()
                // below). Platform guarantees that nothing bad will happen if we return 'true' here and later return
                // 'null' / empty collection from within GetPreviewAsync().

                return true;
            }
        }

        public virtual async Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Light bulb will always invoke this function on the UI thread.
            AssertIsForeground();

            var preferredDocumentId = Workspace.GetDocumentIdInCurrentContext(SubjectBuffer.AsTextContainer());
            var preferredProjectId = preferredDocumentId?.ProjectId;

            var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
            var previewContent = await extensionManager.PerformFunctionAsync(Provider, async () =>
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
                    AssertIsForeground();
                    return await previewResult.GetPreviewsAsync(preferredDocumentId, preferredProjectId, cancellationToken).ConfigureAwait(true);
                }

                // GetPreviewPane() below needs to run on UI thread. We use ConfigureAwait(true) to stay on the UI thread.
            }, defaultValue: null).ConfigureAwait(true);

            var previewPaneService = Workspace.Services.GetService<IPreviewPaneService>();
            if (previewPaneService == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // GetPreviewPane() needs to run on the UI thread.
            AssertIsForeground();

            string language;
            string projectType;
            Workspace.GetLanguageAndProjectType(preferredProjectId, out language, out projectType);

            return previewPaneService.GetPreviewPane(GetDiagnostic(), language, projectType, previewContent);
        }

        protected virtual DiagnosticData GetDiagnostic()
        {
            return null;
        }

        public virtual bool HasActionSets => _actionSets.Length > 0;

        public virtual Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(GetActionSets());
        }

        internal ImmutableArray<SuggestedActionSet> GetActionSets()
        {
            return _actionSets;
        }

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

        string ISuggestedAction.InputGestureText
        {
            get
            {
                // no shortcut support
                return null;
            }
        }

        #endregion

        #region IEquatable<ISuggestedAction>

        public bool Equals(ISuggestedAction other)
        {
            return Equals(other as SuggestedAction);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SuggestedAction);
        }

        internal bool Equals(SuggestedAction otherSuggestedAction)
        {
            if (otherSuggestedAction == null)
            {
                return false;
            }

            if (ReferenceEquals(this, otherSuggestedAction))
            {
                return true;
            }

            if (!ReferenceEquals(Provider, otherSuggestedAction.Provider))
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
