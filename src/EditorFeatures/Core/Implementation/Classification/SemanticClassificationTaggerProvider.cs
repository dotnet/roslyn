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
    internal partial class SemanticClassificationTaggerProvider : AbstractAsynchronousBufferTaggerProvider<IClassificationTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public SemanticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Classification), notificationService)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _typeMap = typeMap;
        }

        // We don't want to remove a tag just because it intersected an edit.  This can 
        // cause flashing when a edit touches the edge of a classified symbol without
        // changing it.  For example, if you have "Console." and you remove the <dot>,
        // then you don't want to remove the classification for 'Console'.
        protected override bool RemoveTagsThatIntersectEdits => false;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        protected override IEnumerable<Option<bool>> TagSourceOptions
        {
            get
            {
                yield return InternalFeatureOnOffOptions.SemanticColorizer;
            }
        }

        protected override ProducerPopulatedTagSource<IClassificationTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return new SemanticBufferTagSource<IClassificationTag>(
                subjectBuffer,
                CreateTagProducer(),
                CreateEventSource(textViewOpt, subjectBuffer),
                AsyncListener,
                NotificationService,
                this.RemoveTagsThatIntersectEdits,
                this.SpanTrackingMode);
        }

        protected override ITagProducer<IClassificationTag> CreateTagProducer()
        {
            return new TagProducer(_typeMap);
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.Short, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short),
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.Short, reportChangedSpans: true));
        }
    }
}
