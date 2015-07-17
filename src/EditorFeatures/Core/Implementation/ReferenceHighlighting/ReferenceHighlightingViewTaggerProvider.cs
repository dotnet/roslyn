// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(AbstractNavigatableReferenceHighlightingTag))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal partial class ReferenceHighlightingViewTaggerProvider :
        ForegroundThreadAffinitizedObject,
        IViewTaggerProvider,
        IAsynchronousTaggerDataSource<AbstractNavigatableReferenceHighlightingTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly Lazy<IViewTaggerProvider> _asynchronousTaggerProvider;

        public bool RemoveTagsThatIntersectEdits => true;
        public TaggerDelay? UIUpdateDelay => TaggerDelay.NearImmediate;
        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public bool IgnoreCaretMovementToExistingTag => true;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public IEqualityComparer<AbstractNavigatableReferenceHighlightingTag> TagComparer => null;
        public IEnumerable<Option<bool>> Options => null;
        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.ReferenceHighlighting);

        [ImportingConstructor]
        public ReferenceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _asynchronousTaggerProvider = new Lazy<IViewTaggerProvider>(() =>
                new AsynchronousViewTaggerProviderWithTagSource<AbstractNavigatableReferenceHighlightingTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ReferenceHighlighting),
                    notificationService,
                    this.CreateTagSource));
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(textView, buffer);
        }

        public ITagProducer<AbstractNavigatableReferenceHighlightingTag> CreateTagProducer()
        {
            return new TagProducer();
        }

        public ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // PERF: use a longer delay for OnTextChanged to minimize the impact of GCs while typing
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer, TaggerDelay.Short),
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.OnIdle, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.ReferenceHighlighting, TaggerDelay.NearImmediate));
        }

        private ProducerPopulatedTagSource<AbstractNavigatableReferenceHighlightingTag> CreateTagSource(
            ITextView textViewOpt, ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            return new ReferenceHighlightingTagSource(textViewOpt, subjectBuffer, this, asyncListener, notificationService);
        }

        public IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return textViewOpt.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType))
                              .Select(b => b.CurrentSnapshot.GetFullSpan())
                              .ToList();
        }
    }
}