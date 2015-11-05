// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(IInlineRenameService))]
    [Export(typeof(InlineRenameService))]
    internal class InlineRenameService : IInlineRenameService
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
        private readonly AggregateAsynchronousOperationListener _aggregateListener;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly ITextBufferFactoryService _textBufferFactoryService;

        private InlineRenameSession _activeRenameSession;

        [ImportingConstructor]
        public InlineRenameService(
            IWaitIndicator waitIndicator,
            ITextBufferAssociatedViewService textBufferAssociatedViewService,
            ITextBufferFactoryService textBufferFactoryService,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners)
        {
            _waitIndicator = waitIndicator;
            _textBufferAssociatedViewService = textBufferAssociatedViewService;
            _textBufferFactoryService = textBufferFactoryService;
            _refactorNotifyServices = refactorNotifyServices;
            _aggregateListener = new AggregateAsynchronousOperationListener(listeners, FeatureAttribute.Rename);
        }

        public InlineRenameSessionInfo StartInlineSession(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            if (_activeRenameSession != null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.AnActiveInlineRenameSessionIsActive);
            }

            var editorRenameService = document.GetLanguageService<IEditorInlineRenameService>();
            var renameInfo = editorRenameService.GetRenameInfoAsync(document, textSpan.Start, cancellationToken).WaitAndGetResult(cancellationToken);
            if (!renameInfo.CanRename)
            {
                return new InlineRenameSessionInfo(renameInfo.LocalizedErrorMessage);
            }

            var snapshot = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).FindCorrespondingEditorTextSnapshot();
            ActiveSession = new InlineRenameSession(
                this,
                document.Project.Solution.Workspace,
                renameInfo.TriggerSpan.ToSnapshotSpan(snapshot),
                renameInfo,
                _waitIndicator,
                _textBufferAssociatedViewService,
                _textBufferFactoryService,
                _refactorNotifyServices,
                _aggregateListener);

            return new InlineRenameSessionInfo(ActiveSession);
        }

        IInlineRenameSession IInlineRenameService.ActiveSession
        {
            get
            {
                return _activeRenameSession;
            }
        }

        internal InlineRenameSession ActiveSession
        {
            get
            {
                return _activeRenameSession;
            }

            set
            {
                var previousSession = _activeRenameSession;
                _activeRenameSession = value;
                ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousSession));
            }
        }

        /// <summary>
        /// Raised when the ActiveSession property has changed.
        /// </summary>
        internal event EventHandler<ActiveSessionChangedEventArgs> ActiveSessionChanged;

        internal class ActiveSessionChangedEventArgs : EventArgs
        {
            public ActiveSessionChangedEventArgs(InlineRenameSession previousSession)
            {
                this.PreviousSession = previousSession;
            }

            public InlineRenameSession PreviousSession { get; }
        }
    }
}
