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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(BraceHighlightTag))]
    internal class BraceHighlightingViewTaggerProvider : AsynchronousViewTaggerProvider<BraceHighlightTag>
    {
        private readonly IBraceMatchingService _braceMatcherService;

        public override bool RemoveTagsThatIntersectEdits => true;
        public override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public override IEnumerable<Option<bool>> Options => SpecializedCollections.SingletonEnumerable(InternalFeatureOnOffOptions.BraceMatching);

        [ImportingConstructor]
        public BraceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            IBraceMatchingService braceMatcherService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.BraceHighlighting), notificationService)
        {
            _braceMatcherService = braceMatcherService;
        }

        public override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate));
        }

        public override ITagProducer<BraceHighlightTag> CreateTagProducer()
        {
            return new BraceHighlightingTagProducer(_braceMatcherService);
        }
    }
}