// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(AbstractNavigatableReferenceHighlightingTag))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal partial class ReferenceHighlightingViewTaggerProvider :
        AbstractAsynchronousViewTaggerProvider<AbstractNavigatableReferenceHighlightingTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;

        [ImportingConstructor]
        public ReferenceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
            base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ReferenceHighlighting), notificationService)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
        }

        protected override bool RemoveTagsThatIntersectEdits => true;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        protected override IEnumerable<PerLanguageOption<bool>> TagSourcePerLanguageOptions
        {
            get
            {
                yield return FeatureOnOffOptions.ReferenceHighlighting;
            }
        }

        protected override TaggerDelay UIUpdateDelay => TaggerDelay.NearImmediate;

        protected override ITagProducer<AbstractNavigatableReferenceHighlightingTag> CreateTagProducer()
        {
            return new TagProducer();
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // PERF: use a longer delay for OnTextChanged to minimize the impact of GCs while typing
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer, TaggerDelay.Short),
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.OnIdle, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.ReferenceHighlighting, TaggerDelay.NearImmediate));
        }

        protected override ProducerPopulatedTagSource<AbstractNavigatableReferenceHighlightingTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return new ReferenceHighlightingTagSource(
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
