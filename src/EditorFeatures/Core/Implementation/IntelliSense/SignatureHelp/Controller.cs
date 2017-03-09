// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller :
        AbstractController<Controller.Session, Model, ISignatureHelpPresenterSession, ISignatureHelpSession>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<InvokeSignatureHelpCommandArgs>
    {
        private static readonly object s_controllerPropertyKey = new object();

        private SignatureHelpService _service;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            SignatureHelpService service = null)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "SignatureHelp")
        {
            this._service = service;
        }

        internal static Controller GetInstance(
            CommandArgs args,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
            IAsynchronousOperationListener asyncListener)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_controllerPropertyKey,
                (v, b) => new Controller(v, b,
                    presenter,
                    asyncListener,
                    new DocumentProvider()));
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

        private void StartSession(SignatureHelpTrigger trigger)
        {
            AssertIsForeground();
            VerifySessionIsInactive();

            this.sessionOpt = new Session(this, Presenter.CreateSession(TextView, SubjectBuffer, null), _service);
            this.sessionOpt.ComputeModel(trigger);
        }

        private SignatureHelpService GetSignatureHelpService()
        {
            Workspace workspace;
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                Trace.WriteLine("Failed to get a workspace, cannot have a signature help service.");
                return null;
            }

            return workspace.Services.GetLanguageServices(this.SubjectBuffer).GetService<SignatureHelpService>();
        }

        private OptionSet GetOptions()
        {
            AssertIsForeground();

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                return null;
            }

            return workspace.Options;
        }

        private void Update()
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

            sessionOpt.ComputeModel(SignatureHelpTrigger.CreateUpdateTrigger());
        }
    }
}
