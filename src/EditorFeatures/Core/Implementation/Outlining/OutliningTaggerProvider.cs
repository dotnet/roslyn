// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    /// <summary>
    /// Shared implementation of the outliner tagger provider.
    /// 
    /// Note: the outliner tagger is a normal buffer tagger provider and not a view tagger provider.
    /// This is important for two reason.  The first is that if it were view based then we would lose
    /// the state of the collapsed/open regions when they scrolled in and out of view.  Also, if the
    /// editor doesn't know about all the regions in the file, then it wouldn't be able to to
    /// persist them to the SUO file to persist this data across sessions.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(OutliningTaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class OutliningTaggerProvider :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<IOutliningRegionTag>
    {
        private const int MaxPreviewText = 1000;
        private const string Ellipsis = "...";

        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        private readonly Lazy<ITaggerProvider> _asynchronousTaggerProvider;

        public TaggerDelay? UIUpdateDelay => null;
        public bool RemoveTagsThatIntersectEdits => true;
        public SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted { get; set; }

        // TODO(cyrusn): Why don't these actually return real options for this feature?
        public IEnumerable<Option<bool>> Options => null;
        public IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        [ImportingConstructor]
        public OutliningTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _textEditorFactoryService = textEditorFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;

            _asynchronousTaggerProvider = new Lazy<ITaggerProvider>(() =>
                new AsynchronousTaggerProvider<IOutliningRegionTag>(
                    this,
                    new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Outlining),
                    notificationService));
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            this.AssertIsForeground();
            return _asynchronousTaggerProvider.Value.CreateTagger<T>(buffer);
        }

        public ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // We listen to the following events:
            // 1) Text changes.  These can obviously affect outlining, so we need to recompute when
            //     we hear about them.
            // 2) Parse option changes.  These can affect outlining when, for example, we change from 
            //    DEBUG to RELEASE (affecting the inactive/active regions).
            // 3) When we hear about a workspace being registered.  Outlining may run before a 
            //    we even know about a workspace.  This can happen, for example, in the TypeScript
            //    case.  With TypeScript a file is opened, but the workspace is not generated until
            //    some time later when they have examined the file system.  As such, initially,
            //    the file will not have outline spans.  When the workspace is created, we want to
            //    then produce the right outlining spans.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.OnIdle));
        }

        public ITagProducer<IOutliningRegionTag> CreateTagProducer()
        {
            return new TagProducer(
                _textEditorFactoryService,
                _editorOptionsFactoryService,
                _projectionBufferFactoryService);
        }
    }
}
