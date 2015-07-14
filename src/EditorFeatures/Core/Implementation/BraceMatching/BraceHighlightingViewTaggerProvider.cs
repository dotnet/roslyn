// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        ForegroundThreadAffinitizedObject,
        IViewTaggerProvider,
        IAsynchronousTaggerDataSource<BraceHighlightTag>
    {
        private readonly IBraceMatchingService _braceMatcherService;
        private readonly Lazy<IViewTaggerProvider> _asynchronousTaggerProvider;

        [ImportingConstructor]
        public BraceHighlightingViewTaggerProvider(
            IForegroundNotificationService notificationService,
            IBraceMatchingService braceMatcherService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _braceMatcherService = braceMatcherService;
            _asynchronousTaggerProvider = new Lazy<IViewTaggerProvider>(() =>
                new AsynchronousTaggerProvider<BraceHighlightTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.BraceHighlighting),
                    notificationService));
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(textView, buffer);
        }

        public bool RemoveTagsThatIntersectEdits => true;

        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;

        public TaggerDelay? UIUpdateDelay => null;

        public IEnumerable<Option<bool>> Options
        {
            get
            {
                yield return InternalFeatureOnOffOptions.BraceMatching;
            }
        }

        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate));
        }

        public ITagProducer<BraceHighlightTag> CreateTagProducer()
        {
            return new BraceHighlightingTagProducer(_braceMatcherService);
        }
    }
}
