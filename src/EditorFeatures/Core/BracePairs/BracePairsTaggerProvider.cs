﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BracePairs
{
#pragma warning disable CS0618 // IBracePairTag is obsolete while editor works on this API.
    [Export(typeof(IViewTaggerProvider))]
    [VisualStudio.Utilities.Name(nameof(BracePairsTaggerProvider))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(IBracePairTag))]
    internal sealed class BracePairsTaggerProvider : AsynchronousViewTaggerProvider<IBracePairTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BracePairsTaggerProvider(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptionService,
            [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext,
                  globalOptionService,
                  visibilityTracker,
                  listenerProvider.GetListener(FeatureAttribute.BracePairs))
        {
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
                TaggerEventSources.OnTextChanged(subjectBuffer),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView? textView, ITextBuffer subjectBuffer)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to tagging all brace locations
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<IBracePairTag> context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<BracePairData>.GetInstance(out var bracePairs);
            foreach (var spanToTag in context.SpansToTag)
            {
                var document = spanToTag.Document;
                if (document is null)
                    continue;

                var service = document.GetLanguageService<IBracePairsService>();
                if (service is null)
                    continue;

                bracePairs.Clear();
                await service.AddBracePairsAsync(
                    document, spanToTag.SnapshotSpan.Span.ToTextSpan(), bracePairs, cancellationToken).ConfigureAwait(false);

                var snapshot = spanToTag.SnapshotSpan.Snapshot;
                foreach (var bracePair in bracePairs)
                {
                    var start = CreateSnapshotSpan(bracePair.Start, snapshot);
                    var end = CreateSnapshotSpan(bracePair.End, snapshot);
                    if (start is null && end is null)
                        continue;

                    context.AddTag(new TagSpan<IBracePairTag>(
                        new SnapshotSpan(snapshot, Span.FromBounds(bracePair.Start.Start, bracePair.End.End)),
                        new BracePairTag(start, end)));
                }
            }

            return;

            static SnapshotSpan? CreateSnapshotSpan(TextSpan span, ITextSnapshot snapshot)
                => span.IsEmpty ? null : span.ToSnapshotSpan(snapshot);
        }

        protected override bool TagEquals(IBracePairTag tag1, IBracePairTag tag2)
        {
            if (tag1 is null && tag2 is null)
                return true;

            if (tag1 is null || tag2 is null)
                return false;

            return SpanEquals(tag1.Start, tag2.Start) &&
                   SpanEquals(tag1.End, tag2.End);
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
