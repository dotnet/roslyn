﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller :
        AbstractController<Session<Controller, Model, IQuickInfoPresenterSession>, Model, IQuickInfoPresenterSession, IQuickInfoSession>,
        ICommandHandler<InvokeQuickInfoCommandArgs>
    {
        private static readonly object s_quickInfoPropertyKey = new object();
        private QuickInfoService _service;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
        }

        // For testing purposes
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            QuickInfoService service)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
            _service = service;
        }

        internal static Controller GetInstance(
            CommandArgs args,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_quickInfoPropertyKey,
                (v, b) => new Controller(v, b,
                    presenter,
                    asyncListener,
                    new DocumentProvider()));
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            AssertIsForeground();
            if (modelOpt == null || modelOpt.TextVersion != this.SubjectBuffer.CurrentSnapshot.Version)
            {
                this.StopModelComputation();
            }
            else
            {
                var quickInfoItem = modelOpt.Item;

                // We want the span to actually only go up to the caret.  So get the expected span
                // and then update its end point accordingly.
                var triggerSpan = modelOpt.GetCurrentSpanInSnapshot(quickInfoItem.Span, this.SubjectBuffer.CurrentSnapshot);
                var trackingSpan = triggerSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

                sessionOpt.PresenterSession.PresentItem(trackingSpan, quickInfoItem, modelOpt.TrackMouse);
            }
        }

        public void StartSession(
            int position,
            bool trackMouse,
            IQuickInfoSession augmentSession = null)
        {
            AssertIsForeground();

            var service = GetService();
            if (service == null)
            {
                return;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;
            this.sessionOpt = new Session<Controller, Model, IQuickInfoPresenterSession>(this, new ModelComputation<Model>(this, TaskScheduler.Default),
                this.Presenter.CreateSession(this.TextView, this.SubjectBuffer, augmentSession));

            this.sessionOpt.Computation.ChainTaskAndNotifyControllerWhenFinished(
                (model, cancellationToken) => ComputeModelInBackgroundAsync(position, snapshot, service, trackMouse, cancellationToken));
        }

        public QuickInfoService GetService()
        {
            this.AssertIsForeground();

            if (_service == null)
            {
                var snapshot = this.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    _service = QuickInfoService.GetService(document);
                }
            }

            return _service;
        }

        private async Task<Model> ComputeModelInBackgroundAsync(
               int position,
               ITextSnapshot snapshot,
               QuickInfoService service,
               bool trackMouse,
               CancellationToken cancellationToken)
        {
            try
            {
                AssertIsBackground();

                using (Logger.LogBlock(FunctionId.QuickInfo_ModelComputation_ComputeModelInBackground, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = await DocumentProvider.GetDocumentAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    if (document == null)
                    {
                        return null;
                    }

                    var item = await service.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                    if (item != null)
                    {
                        return new Model(snapshot.Version, item, trackMouse);
                    }

                    return null;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
