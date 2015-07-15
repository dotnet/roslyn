// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
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
    internal partial class OutliningTaggerProvider : AsynchronousTaggerProvider<IOutliningRegionTag>,
        IEqualityComparer<IOutliningRegionTag>
    {
        public const string OutliningRegionTextViewRole = nameof(OutliningRegionTextViewRole);

        private const int MaxPreviewText = 1000;
        private const string Ellipsis = "...";

        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        public override bool RemoveTagsThatIntersectEdits => true;
        public override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public override bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted =>
            _computeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted;

        private bool _computeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted;

        [ImportingConstructor]
        public OutliningTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Outlining), notificationService)
        {
            _textEditorFactoryService = textEditorFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
        }

        public void SetComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted(bool value)
        {
            _computeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted = value;
        }

        public override IEqualityComparer<IOutliningRegionTag> TagComparer => this;

        bool IEqualityComparer<IOutliningRegionTag>.Equals(IOutliningRegionTag x, IOutliningRegionTag y)
        {
            // This is only called if the spans for the tags were the same. In that case, we consider ourselves the same
            // unless the CollapsedForm properties are different.
            return object.Equals(x.CollapsedForm, y.CollapsedForm);
        }

        int IEqualityComparer<IOutliningRegionTag>.GetHashCode(IOutliningRegionTag obj)
        {
            // This will not result in lots of hash collisions as our caller will
            // first be hashing spans, and then adding this value to that.
            // The only collisions will be for outlining tags with the same span
            // (which is what we want).
            return 0;
        }

        public override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
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

        public override ITagProducer<IOutliningRegionTag> CreateTagProducer()
        {
            return new TagProducer(
                _textEditorFactoryService,
                _editorOptionsFactoryService,
                _projectionBufferFactoryService);
        }
    }
}
