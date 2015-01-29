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
    internal class BraceHighlightingViewTaggerProvider :
        AbstractAsynchronousViewTaggerProvider<BraceHighlightTag>
    {
        private readonly IBraceMatchingService _braceMatcherService;

        [ImportingConstructor]
        public BraceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            IBraceMatchingService braceMatcherService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.BraceHighlighting),
                   notificationService)
        {
            _braceMatcherService = braceMatcherService;
        }

        protected override bool RemoveTagsThatIntersectEdits => true;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        protected override IEnumerable<Option<bool>> TagSourceOptions
        {
            get
            {
                yield return InternalFeatureOnOffOptions.BraceMatching;
            }
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate));
        }

        protected override ITagProducer<BraceHighlightTag> CreateTagProducer()
        {
            return new BraceHighlightingTagProducer(_braceMatcherService);
        }
    }
}
