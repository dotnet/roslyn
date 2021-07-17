// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    /// <summary>
    /// Tagger for active statements. Active statements are only tracked for langauges that support EnC (C#, VB).
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ActiveStatementTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class ActiveStatementTaggerProvider : AsynchronousTaggerProvider<ITextMarkerTag>
    {
        // We want to track text changes so that we can try to only reclassify a method body if
        // all edits were contained within one.
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.TrackTextChanges;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ActiveStatementTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Classification))
        {
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            AssertIsForeground();

            return TaggerEventSources.Compose(
                new EventSource(subjectBuffer),
                TaggerEventSources.OnTextChanged(subjectBuffer),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer));
        }

        protected override async Task ProduceTagsAsync(TaggerContext<ITextMarkerTag> context)
        {
            Debug.Assert(context.SpansToTag.IsSingle());

            var spanToTag = context.SpansToTag.Single();

            var document = spanToTag.Document;
            if (document == null)
            {
                return;
            }

            var activeStatementTrackingService = document.Project.Solution.Workspace.Services.GetService<IActiveStatementTrackingService>();
            if (activeStatementTrackingService == null)
            {
                return;
            }

            var snapshot = spanToTag.SnapshotSpan.Snapshot;

            var activeStatementSpans = await activeStatementTrackingService.GetAdjustedTrackingSpansAsync(document, snapshot, context.CancellationToken).ConfigureAwait(false);
            foreach (var activeStatementSpan in activeStatementSpans)
            {
                if (activeStatementSpan.IsLeaf)
                {
                    continue;
                }

                var snapshotSpan = activeStatementSpan.Span.GetSpan(snapshot);
                if (snapshotSpan.OverlapsWith(spanToTag.SnapshotSpan))
                {
                    context.AddTag(new TagSpan<ITextMarkerTag>(snapshotSpan, ActiveStatementTag.Instance));
                }
            }

            // Let the context know that this was the span we actually tried to tag.
            context.SetSpansTagged(SpecializedCollections.SingletonEnumerable(spanToTag));
        }
    }
}
