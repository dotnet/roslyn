// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Roslyn.Utilities;

#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller :
        AbstractController<Session<Controller, Model, IQuickInfoPresenterSession>, Model, IQuickInfoPresenterSession, IAsyncQuickInfoSession>
    {
        private static readonly object s_quickInfoPropertyKey = new object();
        private QuickInfoService _service;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
        }

        // For testing purposes
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            QuickInfoService service)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
            _service = service;
        }

        internal static Controller GetInstance(
            EditorCommandArgs args,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession> presenter,
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

                //sessionOpt.PresenterSession.PresentItem(trackingSpan, quickInfoItem, modelOpt.TrackMouse);
            }
        }

        public async Task<VisualStudio.Language.Intellisense.QuickInfoItem> GetQuickInfoItemAsync(
            SnapshotPoint triggerPoint,
            IAsyncQuickInfoSession augmentSession = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var service = GetService();
            if (service == null)
            {
                return (VisualStudio.Language.Intellisense.QuickInfoItem)null;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;
            this.sessionOpt = new Session<Controller, Model, IQuickInfoPresenterSession>(this, new ModelComputation<Model>(this, TaskScheduler.Default),
                this.Presenter.CreateSession(this.TextView, this.SubjectBuffer, augmentSession));

            var trackMouse = augmentSession != null && augmentSession.Options == QuickInfoSessionOptions.TrackMouse;
            try
            {
                using (Logger.LogBlock(FunctionId.QuickInfo_ModelComputation_ComputeModelInBackground, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = await DocumentProvider.GetDocumentAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    if (document == null)
                    {
                        return null;
                    }

                    var item = await service.GetQuickInfoAsync(document, triggerPoint, cancellationToken).ConfigureAwait(false);
                    if (item != null)
                    {
                        return sessionOpt.PresenterSession.ConvertQuickInfoItem(this.SubjectBuffer,triggerPoint, item);
                    }

                    return null;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private VisualStudio.Language.Intellisense.QuickInfoItem ConvertQuickInfoItem(
            SnapshotPoint triggerPoint,
            CodeAnalysis.QuickInfo.QuickInfoItem codeAnalysisQuickInfo)
        {

            var line = triggerPoint.GetContainingLine();
            var lineNumber = triggerPoint.GetContainingLine().LineNumber;
            var lineSpan = this.SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(
                line.Extent,
                SpanTrackingMode.EdgeInclusive);

            var textList = new List<ClassifiedTextRun>();
            foreach(var section in codeAnalysisQuickInfo.Sections)
            {
                textList.Add(new ClassifiedTextRun(section.Kind, section.Text));
            }

            return new VisualStudio.Language.Intellisense.QuickInfoItem(lineSpan, new ClassifiedTextElement(textList));
        }

        public QuickInfoService GetService()
        {
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
    }
}
