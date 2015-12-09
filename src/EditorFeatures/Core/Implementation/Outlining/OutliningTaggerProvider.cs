// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    /// <summary>
    /// Shared implementation of the outliner tagger provider.
    /// 
    /// Note: the outliner tagger is a normal buffer tagger provider and not a view tagger provider.
    /// This is important for two reasons.  The first is that if it were view-based then we would lose
    /// the state of the collapsed/open regions when they scrolled in and out of view.  Also, if the
    /// editor doesn't know about all the regions in the file, then it wouldn't be able to
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

        protected override IEqualityComparer<IOutliningRegionTag> TagComparer => this;

        bool IEqualityComparer<IOutliningRegionTag>.Equals(IOutliningRegionTag x, IOutliningRegionTag y)
        {
            // This is only called if the spans for the tags were the same. In that case, we consider ourselves the same
            // unless the CollapsedForm properties are different.
            return EqualityComparer<object>.Default.Equals(x.CollapsedForm, y.CollapsedForm);
        }

        int IEqualityComparer<IOutliningRegionTag>.GetHashCode(IOutliningRegionTag obj)
        {
            return EqualityComparer<object>.Default.GetHashCode(obj.CollapsedForm);
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
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

        protected override async Task ProduceTagsAsync(TaggerContext<IOutliningRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var cancellationToken = context.CancellationToken;
                using (Logger.LogBlock(FunctionId.Tagger_Outlining_TagProducer_ProduceTags, cancellationToken))
                {
                    var document = documentSnapshotSpan.Document;
                    var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                    var snapshot = snapshotSpan.Snapshot;

                    if (document != null)
                    {
                        var outliningService = document.Project.LanguageServices.GetService<IOutliningService>();
                        if (outliningService != null)
                        {
                            var regions = await outliningService.GetOutliningSpansAsync(document, cancellationToken).ConfigureAwait(false);
                            if (regions != null)
                            {
                                regions = GetMultiLineRegions(outliningService, regions, snapshotSpan.Snapshot);

                                // Create the outlining tags.
                                var tagSpans =
                                    from region in regions
                                    let spanToCollapse = new SnapshotSpan(snapshot, region.TextSpan.ToSpan())
                                    let hintSpan = new SnapshotSpan(snapshot, region.HintSpan.ToSpan())
                                    let tag = new Tag(snapshot.TextBuffer,
                                                      region.BannerText,
                                                      hintSpan,
                                                      region.AutoCollapse,
                                                      region.IsDefaultCollapsed,
                                                      _textEditorFactoryService,
                                                      _projectionBufferFactoryService,
                                                      _editorOptionsFactoryService)
                                    select new TagSpan<IOutliningRegionTag>(spanToCollapse, tag);

                                foreach (var tagSpan in tagSpans)
                                {
                                    context.AddTag(tagSpan);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool s_exceptionReported = false;

        private IList<OutliningSpan> GetMultiLineRegions(IOutliningService service, IList<OutliningSpan> regions, ITextSnapshot snapshot)
        {
            // Remove any spans that aren't multiline.
            var multiLineRegions = new List<OutliningSpan>(regions.Count);
            foreach (var region in regions)
            {
                if (region != null && region.TextSpan.Length > 0)
                {
                    // Check if any clients produced an invalid OutliningSpan.  If so, filter them
                    // out and report a non-fatal watson so we can attempt to determine the source
                    // of the issue.
                    var snapshotSpan = snapshot.GetFullSpan().Span;
                    var regionSpan = region.TextSpan.ToSpan();
                    if (!snapshotSpan.Contains(regionSpan))
                    {
                        if (!s_exceptionReported)
                        {
                            s_exceptionReported = true;
                            try
                            {
                                throw new InvalidOutliningRegionException(service, snapshot, snapshotSpan, regionSpan);
                            }
                            catch (InvalidOutliningRegionException e) when (FatalError.ReportWithoutCrash(e))
                            {
                            }
                        }
                        continue;
                    }

                    var startLine = snapshot.GetLineNumberFromPosition(region.TextSpan.Start);
                    var endLine = snapshot.GetLineNumberFromPosition(region.TextSpan.End);
                    if (startLine != endLine)
                    {
                        multiLineRegions.Add(region);
                    }
                }
            }

            return multiLineRegions;
        }
    }
}
