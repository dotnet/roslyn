// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(HighlightTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class HighlighterViewTaggerProvider :
        AbstractAsynchronousViewTaggerProvider<HighlightTag>
    {
        private readonly IHighlightingService _highlighterService;

        [ImportingConstructor]
        public HighlighterViewTaggerProvider(
            IForegroundNotificationService notificationService,
            IHighlightingService highlighterService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.KeywordHighlighting), notificationService)
        {
            _highlighterService = highlighterService;
        }

        protected override bool RemoveTagsThatIntersectEdits => true;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        protected override IEnumerable<Option<bool>> TagSourceOptions
        {
            get
            {
                yield return InternalFeatureOnOffOptions.KeywordHighlight;
            }
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle, reportChangedSpans: true),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.KeywordHighlighting, TaggerDelay.NearImmediate));
        }

        protected override ITagProducer<HighlightTag> CreateTagProducer()
        {
            return new HighlighterTagProducer(_highlighterService);
        }

        protected override ProducerPopulatedTagSource<HighlightTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return new HighlightingTagSource(
                textViewOpt,
                subjectBuffer,
                CreateTagProducer(),
                CreateEventSource(textViewOpt, subjectBuffer),
                AsyncListener,
                NotificationService,
                this.RemoveTagsThatIntersectEdits,
                this.SpanTrackingMode);
        }
    }
}
