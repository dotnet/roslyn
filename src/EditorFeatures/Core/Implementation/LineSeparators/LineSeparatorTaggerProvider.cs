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

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// This factory is called to create taggers that provide information about where line
    /// separators go.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(LineSeparatorTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class LineSeparatorTaggerProvider :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<LineSeparatorTag>
    {
        private readonly Lazy<ITaggerProvider> _asynchronousTaggerProvider;

        [ImportingConstructor]
        public LineSeparatorTaggerProvider(
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _asynchronousTaggerProvider = new Lazy<ITaggerProvider>(() =>
                new AsynchronousTaggerProvider<LineSeparatorTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.LineSeparators),
                    notificationService));
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            this.AssertIsForeground();
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(buffer);
        }

        public bool RemoveTagsThatIntersectEdits => true;

        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;

        public TaggerDelay? UIUpdateDelay => null;

        // TODO(cyrusn): Why don't these actually return real options for this feature?
        public IEnumerable<Option<bool>> Options => null;

        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, FeatureOnOffOptions.LineSeparator, TaggerDelay.NearImmediate));
        }

        public ITagProducer<LineSeparatorTag> CreateTagProducer()
        {
            return new TagProducer();
        }
    }
}
