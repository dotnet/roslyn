// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller :
        AbstractController<Controller.Session, Model, ISignatureHelpPresenterSession, ISignatureHelpSession>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<InvokeSignatureHelpCommandArgs>
    {
        private static readonly object s_controllerPropertyKey = new object();

        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _allProviders;
        private IList<ISignatureHelpProvider> _providers;
        private IContentType _lastSeenContentType;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "SignatureHelp")
        {
            _allProviders = allProviders;
        }

        // For testing purposes.
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            IList<ISignatureHelpProvider> providers)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "SignatureHelp")
        {
            _providers = providers;
        }

        internal static Controller GetInstance(
            CommandArgs args,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_controllerPropertyKey,
                (v, b) => new Controller(v, b,
                    presenter,
                    asyncListener,
                    new DocumentProvider(),
                    allProviders));
        }

        private SnapshotPoint GetCaretPointInViewBuffer()
        {
            AssertIsForeground();
            return this.TextView.Caret.Position.BufferPosition;
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            AssertIsForeground();
            if (modelOpt == null)
            {
                this.StopModelComputation();
            }
            else
            {
                var selectedItem = modelOpt.SelectedItem;
                var triggerSpan = modelOpt.GetCurrentSpanInView(this.TextView.TextSnapshot);

                // We want the span to actually only go up to the caret.  So get the expected span
                // and then update its end point accordingly.
                var updatedSpan = new SnapshotSpan(triggerSpan.Snapshot, Span.FromBounds(
                    triggerSpan.Start,
                    Math.Max(Math.Min(triggerSpan.End, GetCaretPointInViewBuffer().Position), triggerSpan.Start)));

                var trackingSpan = updatedSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                this.sessionOpt.PresenterSession.PresentItems(
                     trackingSpan, modelOpt.Items, modelOpt.SelectedItem, modelOpt.SelectedParameter);
            }
        }

        private void StartSession(IList<ISignatureHelpProvider> providers, SignatureHelpTriggerInfo triggerInfo)
        {
            AssertIsForeground();
            VerifySessionIsInactive();

            this.sessionOpt = new Session(this, Presenter.CreateSession(TextView, SubjectBuffer, null));
            this.sessionOpt.ComputeModel(providers, triggerInfo);
        }

        private IList<ISignatureHelpProvider> GetProviders()
        {
            this.AssertIsForeground();

            var snapshot = this.SubjectBuffer.CurrentSnapshot;
            var currentContentType = snapshot.ContentType;

            // if a file's content-type changes (e.g., File.cs is renamed to File.vb) after the list of providers has been populated, then we need to re-filter
            if (_providers == null || currentContentType != _lastSeenContentType)
            {
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    _providers = document.Project.LanguageServices.WorkspaceServices.SelectMatchingExtensionValues(_allProviders, this.SubjectBuffer.ContentType);
                    _lastSeenContentType = currentContentType;
                }
            }

            return _providers;
        }

        private void Retrigger()
        {
            AssertIsForeground();
            if (!IsSessionActive)
            {
                return;
            }

            if (!this.TextView.GetCaretPoint(this.SubjectBuffer).HasValue)
            {
                StopModelComputation();
                return;
            }

            sessionOpt.ComputeModel(GetProviders(), new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand));
        }
    }
}
