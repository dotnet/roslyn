// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;

internal sealed partial class Controller :
    AbstractController<Controller.Session, Model, ISignatureHelpPresenterSession, ISignatureHelpSession>,
    IChainedCommandHandler<TypeCharCommandArgs>,
    IChainedCommandHandler<InvokeSignatureHelpCommandArgs>
{
    private readonly IAsyncCompletionBroker _completionBroker;

    private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _allProviders;
    private ImmutableArray<ISignatureHelpProvider> _providers;
    private IContentType _lastSeenContentType;

    public string DisplayName => EditorFeaturesResources.Signature_Help;

    public Controller(
        IGlobalOptionService globalOptions,
        IThreadingContext threadingContext,
        ITextView textView,
        ITextBuffer subjectBuffer,
        IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
        IAsynchronousOperationListener asyncListener,
        IDocumentProvider documentProvider,
        IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders,
        IAsyncCompletionBroker completionBroker)
        : base(globalOptions, threadingContext, textView, subjectBuffer, presenter, asyncListener, documentProvider, "SignatureHelp")
    {
        _completionBroker = completionBroker;
        _allProviders = allProviders;
    }

    // For testing purposes.
    internal Controller(
        IGlobalOptionService globalOptions,
        IThreadingContext threadingContext,
        ITextView textView,
        ITextBuffer subjectBuffer,
        IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> presenter,
        IAsynchronousOperationListener asyncListener,
        IDocumentProvider documentProvider,
        IList<ISignatureHelpProvider> providers,
        IAsyncCompletionBroker completionBroker)
        : base(globalOptions, threadingContext, textView, subjectBuffer, presenter, asyncListener, documentProvider, "SignatureHelp")
    {
        _providers = [.. providers];
        _completionBroker = completionBroker;
    }

    public event EventHandler<ModelUpdatedEventsArgs> ModelUpdated;

    private SnapshotPoint GetCaretPointInViewBuffer()
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        return this.TextView.Caret.Position.BufferPosition;
    }

    internal override void OnModelUpdated(Model modelOpt, bool updateController)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (updateController)
        {
            if (modelOpt == null)
            {
                this.StopModelComputation();
            }
            else
            {
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

        ModelUpdated?.Invoke(this, new ModelUpdatedEventsArgs(modelOpt));
    }

    private void StartSession(
        ImmutableArray<ISignatureHelpProvider> providers, SignatureHelpTriggerInfo triggerInfo)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        VerifySessionIsInactive();

        this.sessionOpt = new Session(this, Presenter.CreateSession(TextView, SubjectBuffer, null));
        this.sessionOpt.ComputeModel(providers, triggerInfo);
    }

    private ImmutableArray<ISignatureHelpProvider> GetProviders()
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        var snapshot = this.SubjectBuffer.CurrentSnapshot;
        var currentContentType = snapshot.ContentType;

        // if a file's content-type changes (e.g., File.cs is renamed to File.vb) after the list of providers has been populated, then we need to re-filter
        if (_providers == null || currentContentType != _lastSeenContentType)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                _providers = [.. document.Project.Solution.Services.SelectMatchingExtensionValues(
                    _allProviders, this.SubjectBuffer.ContentType)];
                _lastSeenContentType = currentContentType;
            }
        }

        return _providers;
    }

    private void Retrigger()
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
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
