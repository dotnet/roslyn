// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
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
    internal class InlineRenameService : IInlineRenameService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IFeatureServiceFactory _featureServiceFactory;
        private InlineRenameSession? _activeRenameSession;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineRenameService(
            IThreadingContext threadingContext,
            IWaitIndicator waitIndicator,
            ITextBufferAssociatedViewService textBufferAssociatedViewService,
            ITextBufferFactoryService textBufferFactoryService,
            IFeatureServiceFactory featureServiceFactory,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _waitIndicator = waitIndicator;
            _textBufferAssociatedViewService = textBufferAssociatedViewService;
            _textBufferFactoryService = textBufferFactoryService;
            _featureServiceFactory = featureServiceFactory;
            _refactorNotifyServices = refactorNotifyServices;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.Rename);
        }

        public InlineRenameSessionInfo StartInlineSession(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            if (_activeRenameSession != null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.An_active_inline_rename_session_is_still_active_Complete_it_before_starting_a_new_one);
            }

            var editorRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            var renameInfo = editorRenameService.GetRenameInfoAsync(document, textSpan.Start, cancellationToken).WaitAndGetResult(cancellationToken);

            var readOnlyOrCannotNavigateToSpanSessionInfo = IsReadOnlyOrCannotNavigateToSpan(renameInfo, document, cancellationToken);
            if (readOnlyOrCannotNavigateToSpanSessionInfo != null)
            {
                return readOnlyOrCannotNavigateToSpanSessionInfo;
            }

            if (!renameInfo.CanRename)
            {
                return new InlineRenameSessionInfo(renameInfo.LocalizedErrorMessage);
            }

            var snapshot = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).FindCorrespondingEditorTextSnapshot();
            ActiveSession = new InlineRenameSession(
                _threadingContext,
                this,
                document.Project.Solution.Workspace,
                renameInfo.TriggerSpan.ToSnapshotSpan(snapshot),
                renameInfo,
                _waitIndicator,
                _textBufferAssociatedViewService,
                _textBufferFactoryService,
                _featureServiceFactory,
                _refactorNotifyServices,
                _asyncListener);

            return new InlineRenameSessionInfo(ActiveSession);

            static InlineRenameSessionInfo? IsReadOnlyOrCannotNavigateToSpan(IInlineRenameInfo renameInfo, Document document, CancellationToken cancellationToken)
            {
                if (renameInfo is IInlineRenameInfo inlineRenameInfo && inlineRenameInfo.DefinitionLocations != default)
                {
                    var workspace = document.Project.Solution.Workspace;
                    var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

                    foreach (var documentSpan in inlineRenameInfo.DefinitionLocations)
                    {
                        var sourceText = documentSpan.Document.GetTextSynchronously(cancellationToken);
                        var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();

                        if (textSnapshot != null)
                        {
                            var buffer = textSnapshot.TextBuffer;
                            var originalSpan = documentSpan.SourceSpan.ToSnapshotSpan(textSnapshot).TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                            if (buffer.IsReadOnly(originalSpan))
                            {
                                return new InlineRenameSessionInfo(EditorFeaturesResources.You_cannot_rename_this_element_because_it_is_contained_in_a_read_only_file);
                            }
                        }

                        if (!navigationService.CanNavigateToSpan(workspace, document.Id, documentSpan.SourceSpan))
                        {
                            return new InlineRenameSessionInfo(EditorFeaturesResources.You_cannot_rename_this_element_because_it_is_in_a_location_that_cannot_be_navigated_to);
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
                return _activeRenameSession;
            }

            set
            {
                var previousSession = _activeRenameSession;
                _activeRenameSession = value;
                ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousSession!));
            }
        }

        /// <summary>
        /// Raised when the ActiveSession property has changed.
        /// </summary>
        internal event EventHandler<ActiveSessionChangedEventArgs>? ActiveSessionChanged;

        internal class ActiveSessionChangedEventArgs : EventArgs
        {
            public ActiveSessionChangedEventArgs(InlineRenameSession previousSession)
                => this.PreviousSession = previousSession;

            public InlineRenameSession PreviousSession { get; }
        }
    }
}
