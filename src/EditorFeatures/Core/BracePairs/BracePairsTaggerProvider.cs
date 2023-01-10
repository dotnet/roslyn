// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BracePairs
{
    internal interface IBracePairTag : ITag
    {
        SnapshotSpan? Start { get; }
        SnapshotSpan? End { get; }
    }

    internal class BracePairTag : IBracePairTag
    {
        public BracePairTag(SnapshotSpan? start, SnapshotSpan? end)
        {
            if (start == null && end == null)
            {
                throw new ArgumentNullException(nameof(start), "start and end cannot both be null");
            }
            this.Start = start;
            this.End = end;
        }

        public SnapshotSpan? Start { get; }
        public SnapshotSpan? End { get; }
    }

    [Export(typeof(ITaggerProvider))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(IBracePairTag))]
    internal sealed class BracePairsTaggerProvider : AsynchronousTaggerProvider<IBracePairTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BracePairsTaggerProvider(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptionService,
            ITextBufferVisibilityTracker visibilityTracker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext,
                  globalOptionService,
                  visibilityTracker,
                  listenerProvider.GetListener(FeatureAttribute.BracePairs))
        {
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer));
        }

        protected override async Task ProduceTagsAsync(TaggerContext<IBracePairTag> context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<BracePairs>.GetInstance(out var bracePairs);
            foreach (var spanToTag in context.SpansToTag)
            {
                var document = spanToTag.Document;
                if (document is null)
                    continue;

                var service = document.GetLanguageService<IBracePairsService>();
                if (service is null)
                    continue;

                bracePairs.Clear();
                await service.AddBracePairsAsync(document, bracePairs, cancellationToken).ConfigureAwait(false);

                var snapshot = spanToTag.SnapshotSpan.Snapshot;
                foreach (var bracePair in bracePairs)
                {
                    var start = CreateSnapshotSpan(bracePair.Start, snapshot);
                    var end = CreateSnapshotSpan(bracePair.End, snapshot);
                    if (start is null && end is null)
                        continue;

                    context.AddTag(new TagSpan<IBracePairTag>(start ?? end.Value, new BracePairTag(start, end)));
                }
            }

            return;

            static SnapshotSpan? CreateSnapshotSpan(TextSpan span, ITextSnapshot snapshot)
                => span.IsEmpty ? null : span.ToSnapshotSpan(snapshot);
        }
    }
}
