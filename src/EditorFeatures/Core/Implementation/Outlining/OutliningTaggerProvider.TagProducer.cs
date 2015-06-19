// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal partial class OutliningTaggerProvider
    {
        internal class TagProducer :
            AbstractSingleDocumentTagProducer<IOutliningRegionTag>,
            IEqualityComparer<IOutliningRegionTag>
        {
            private readonly ITextEditorFactoryService _textEditorFactoryService;
            private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
            private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

            public TagProducer(
                ITextEditorFactoryService textEditorFactoryService,
                IEditorOptionsFactoryService editorOptionsFactoryService,
                IProjectionBufferFactoryService projectionBufferFactoryService)
            {
                _textEditorFactoryService = textEditorFactoryService;
                _editorOptionsFactoryService = editorOptionsFactoryService;
                _projectionBufferFactoryService = projectionBufferFactoryService;
            }

            public override IEqualityComparer<IOutliningRegionTag> TagComparer
            {
                get
                {
                    return this;
                }
            }

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

            public override async Task<IEnumerable<ITagSpan<IOutliningRegionTag>>> ProduceTagsAsync(
                Document document,
                SnapshotSpan snapshotSpan,
                int? caretPosition,
                CancellationToken cancellationToken)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.Tagger_Outlining_TagProducer_ProduceTags, cancellationToken))
                    {
                        var snapshot = snapshotSpan.Snapshot;

                        if (document != null)
                        {
                            var outliningService = document.Project.LanguageServices.GetService<IOutliningService>();
                            if (outliningService != null)
                            {
                                // TODO: change this to shared pool once Esent branch RI
                                var regions = await outliningService.GetOutliningSpansAsync(document, cancellationToken).ConfigureAwait(false);
                                if (regions != null)
                                {
                                    regions = GetMultiLineRegions(regions, snapshotSpan.Snapshot);

                                    // Create the outlining tags.
                                    var tagSpans =
                                        from region in regions
                                        let spanToCollapse = new SnapshotSpan(snapshot, region.TextSpan.ToSpan())
                                        let hintSpan = new SnapshotSpan(snapshot, region.HintSpan.ToSpan())
                                        let tag = new Tag(snapshot.TextBuffer,
                                                          region.BannerText,
                                                          hintSpan,
                                                          region.AutoCollapse,
                                                          _textEditorFactoryService,
                                                          _projectionBufferFactoryService,
                                                          _editorOptionsFactoryService)
                                        select new TagSpan<IOutliningRegionTag>(spanToCollapse, tag);

                                    return tagSpans.ToList();
                                }
                            }
                        }

                        return SpecializedCollections.EmptyEnumerable<ITagSpan<IOutliningRegionTag>>();
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private IList<OutliningSpan> GetMultiLineRegions(IList<OutliningSpan> regions, ITextSnapshot snapshot)
            {
                // Remove any spans that aren't multiline.
                var multiLineRegions = new List<OutliningSpan>(regions.Count);
                foreach (var region in regions)
                {
                    if (region != null && region.TextSpan.Length > 0)
                    {
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
}
