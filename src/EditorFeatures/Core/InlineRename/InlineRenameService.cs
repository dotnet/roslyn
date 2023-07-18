// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(IInlineRenameService))]
    [Export(typeof(InlineRenameService))]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class InlineRenameService(
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        ITextBufferAssociatedViewService textBufferAssociatedViewService,
        ITextBufferFactoryService textBufferFactoryService,
        ITextBufferCloneService textBufferCloneService,
        IFeatureServiceFactory featureServiceFactory,
        IGlobalOptionService globalOptions,
        [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
        IAsynchronousOperationListenerProvider listenerProvider) : IInlineRenameService
    {
        private readonly IThreadingContext _threadingContext = threadingContext;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor = uiThreadOperationExecutor;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService = textBufferAssociatedViewService;
        private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.Rename);
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices = refactorNotifyServices;
        private readonly ITextBufferFactoryService _textBufferFactoryService = textBufferFactoryService;
        private readonly ITextBufferCloneService _textBufferCloneService = textBufferCloneService;
        private readonly IFeatureServiceFactory _featureServiceFactory = featureServiceFactory;

        internal readonly IGlobalOptionService GlobalOptions = globalOptions;

        private InlineRenameSession? _activeRenameSession;

        public InlineRenameSessionInfo StartInlineSession(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            return _threadingContext.JoinableTaskFactory.Run(() => StartInlineSessionAsync(document, textSpan, cancellationToken));
        }

        public async Task<InlineRenameSessionInfo> StartInlineSessionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            if (_activeRenameSession != null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.An_active_inline_rename_session_is_still_active_Complete_it_before_starting_a_new_one);
            }

            var editorRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            var renameInfo = await editorRenameService.GetRenameInfoAsync(document, textSpan.Start, cancellationToken).ConfigureAwait(false);

            var readOnlyOrCannotNavigateToSpanSessionInfo = await IsReadOnlyOrCannotNavigateToSpanAsync(
                _threadingContext, renameInfo, document, cancellationToken).ConfigureAwait(false);
            if (readOnlyOrCannotNavigateToSpanSessionInfo != null)
            {
                return readOnlyOrCannotNavigateToSpanSessionInfo;
            }

            if (!renameInfo.CanRename)
            {
                return new InlineRenameSessionInfo(renameInfo.LocalizedErrorMessage);
            }

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            Contract.ThrowIfNull(snapshot, "The document used for starting the inline rename session should still be open and associated with a snapshot.");

            var fileRenameInfo = renameInfo.GetFileRenameInfo();
            var canRenameFile = fileRenameInfo == InlineRenameFileRenameInfo.Allowed;

            var options = new SymbolRenameOptions(
                RenameOverloads: renameInfo.MustRenameOverloads || GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.RenameOverloads),
                RenameInStrings: GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.RenameInStrings),
                RenameInComments: GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.RenameInComments),
                RenameFile: canRenameFile && GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.RenameFile));

            var previewChanges = GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.PreviewChanges);

            // The session currently has UI thread affinity.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            ActiveSession = new InlineRenameSession(
                _threadingContext,
                this,
                document.Project.Solution.Workspace,
                renameInfo.TriggerSpan.ToSnapshotSpan(snapshot),
                renameInfo,
                options,
                previewChanges,
                _uiThreadOperationExecutor,
                _textBufferAssociatedViewService,
                _textBufferFactoryService,
                _textBufferCloneService,
                _featureServiceFactory,
                _refactorNotifyServices,
                _asyncListener);

            return new InlineRenameSessionInfo(ActiveSession);

            static async Task<InlineRenameSessionInfo?> IsReadOnlyOrCannotNavigateToSpanAsync(
                IThreadingContext threadingContext, IInlineRenameInfo renameInfo, Document document, CancellationToken cancellationToken)
            {
                if (renameInfo is IInlineRenameInfo inlineRenameInfo && inlineRenameInfo.DefinitionLocations != default)
                {
                    var workspace = document.Project.Solution.Workspace;
                    var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
                    using var _ = PooledObjects.ArrayBuilder<(ITextBuffer, SnapshotSpan)>.GetInstance(out var buffersAndSpans);
                    foreach (var documentSpan in inlineRenameInfo.DefinitionLocations)
                    {
                        var sourceText = await documentSpan.Document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                        var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();

                        if (textSnapshot != null)
                        {
                            var buffer = textSnapshot.TextBuffer;
                            var originalSpan = documentSpan.SourceSpan.ToSnapshotSpan(textSnapshot).TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
                            buffersAndSpans.Add((buffer, originalSpan));
                        }

                        var canNavigate = await navigationService.CanNavigateToSpanAsync(
                            workspace, document.Id, documentSpan.SourceSpan, cancellationToken).ConfigureAwait(false);
                        if (!canNavigate)
                        {
                            return new InlineRenameSessionInfo(EditorFeaturesResources.You_cannot_rename_this_element_because_it_is_in_a_location_that_cannot_be_navigated_to);
                        }
                    }

                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    foreach (var (buffer, originalSpan) in buffersAndSpans)
                    {
                        if (buffer.IsReadOnly(originalSpan))
                        {
                            return new InlineRenameSessionInfo(EditorFeaturesResources.You_cannot_rename_this_element_because_it_is_contained_in_a_read_only_file);
                        }
                    }
                }

                return null;
            }
        }

        IInlineRenameSession? IInlineRenameService.ActiveSession => _activeRenameSession;

        internal InlineRenameSession? ActiveSession
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();

                return _activeRenameSession;
            }

            set
            {
                _threadingContext.ThrowIfNotOnUIThread();

                // This is also checked in InlineRenameSession (which should be the only thing that ever sets this).
                // However, this just adds an extra level of safety to make sure nothing bad is about to happen.
                Contract.ThrowIfTrue(_activeRenameSession != null && value != null, "Cannot assign an active rename session when one is already in progress.");

                var previousSession = _activeRenameSession;
                _activeRenameSession = value;
                ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousSession!));
            }
        }

        /// <summary>
        /// Raised when the ActiveSession property has changed.
        /// </summary>
        internal event EventHandler<ActiveSessionChangedEventArgs>? ActiveSessionChanged;

        internal class ActiveSessionChangedEventArgs(InlineRenameSession previousSession) : EventArgs
        {
            public InlineRenameSession PreviousSession { get; } = previousSession;
        }
    }
}
