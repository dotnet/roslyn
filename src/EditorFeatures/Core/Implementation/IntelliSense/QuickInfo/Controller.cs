// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller :
        AbstractController<Session<Controller, Model, IQuickInfoPresenterSession>, Model, IQuickInfoPresenterSession, IQuickInfoSession>,
        ICommandHandler<InvokeQuickInfoCommandArgs>
    {
        private static readonly object s_quickInfoPropertyKey = new object();

        private readonly IList<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> _allProviders;
        private IList<IQuickInfoProvider> _providers;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            IList<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> allProviders)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
            _allProviders = allProviders;
        }

        // For testing purposes
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IDocumentProvider documentProvider,
            IList<IQuickInfoProvider> providers)
            : base(textView, subjectBuffer, presenter, asyncListener, documentProvider, "QuickInfo")
        {
            _providers = providers;
        }

        internal static Controller GetInstance(
            CommandArgs args,
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            IAsynchronousOperationListener asyncListener,
            IList<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> allProviders)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_quickInfoPropertyKey,
                (v, b) => new Controller(v, b,
                    presenter,
                    asyncListener,
                    new DocumentProvider(),
                    allProviders));
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
                var triggerSpan = modelOpt.GetCurrentSpanInSnapshot(quickInfoItem.TextSpan, this.SubjectBuffer.CurrentSnapshot);
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

            var providers = GetProviders();
            if (providers == null)
            {
                return;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;
            this.sessionOpt = new Session<Controller, Model, IQuickInfoPresenterSession>(this, new ModelComputation<Model>(this, TaskScheduler.Default),
                this.Presenter.CreateSession(this.TextView, this.SubjectBuffer, augmentSession));

            this.sessionOpt.Computation.ChainTaskAndNotifyControllerWhenFinished(
                (model, cancellationToken) => ComputeModelInBackgroundAsync(position, snapshot, providers, trackMouse, cancellationToken));
        }

        public IList<IQuickInfoProvider> GetProviders()
        {
            this.AssertIsForeground();

            if (_providers == null)
            {
                var snapshot = this.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    _providers = document.Project.LanguageServices.WorkspaceServices.SelectMatchingExtensionValues(_allProviders, this.SubjectBuffer.ContentType);
                }
            }

            return _providers;
        }

        private async Task<Model> ComputeModelInBackgroundAsync(
               int position,
               ITextSnapshot snapshot,
               IList<IQuickInfoProvider> providers,
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

                    foreach (var provider in providers)
                    {
                        // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                        // to the extension crashing.
                        var item = await provider.GetItemAsync(document, position, cancellationToken).ConfigureAwait(false);
                        if (item != null)
                        {
                            return new Model(snapshot.Version, item, provider, trackMouse);
                        }
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
