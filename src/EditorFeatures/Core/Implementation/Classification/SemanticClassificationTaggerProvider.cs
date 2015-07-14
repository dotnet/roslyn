// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class SemanticClassificationTaggerProvider :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<IClassificationTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly ClassificationTypeMap _typeMap;
        private readonly Lazy<ITaggerProvider> _asynchronousTaggerProvider;

        [ImportingConstructor]
        public SemanticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _typeMap = typeMap;
            _asynchronousTaggerProvider = new Lazy<ITaggerProvider>(() =>
                new AsynchronousBufferTaggerProvider<IClassificationTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Classification),
                    notificationService,
                    CreateTagSource));
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(buffer);
        }

        // We don't want to remove a tag just because it intersected an edit.  This can 
        // cause flashing when a edit touches the edge of a classified symbol without
        // changing it.  For example, if you have "Console." and you remove the <dot>,
        // then you don't want to remove the classification for 'Console'.
        public bool RemoveTagsThatIntersectEdits => false;

        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        public TaggerDelay? UIUpdateDelay => null;

        public IEnumerable<Option<bool>> Options
        {
            get
            {
                yield return InternalFeatureOnOffOptions.SemanticColorizer;
            }
        }

        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;

        private ProducerPopulatedTagSource<IClassificationTag> CreateTagSource(
            ITextView textViewOpt, ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener, IForegroundNotificationService notificationService)
        {
            return new SemanticBufferTagSource<IClassificationTag>(subjectBuffer, this, asyncListener, notificationService);
        }

        public ITagProducer<IClassificationTag> CreateTagProducer()
        {
            return new TagProducer(_typeMap);
        }

        public ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.Short, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.Short, reportChangedSpans: true));
        }
    }
}
