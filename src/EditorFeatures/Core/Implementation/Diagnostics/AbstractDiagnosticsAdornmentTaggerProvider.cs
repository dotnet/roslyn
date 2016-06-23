using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract class AbstractDiagnosticsAdornmentTaggerProvider<TTag> :
        AbstractDiagnosticsTaggerProvider<TTag>
        where TTag : ITag
    {
        public AbstractDiagnosticsAdornmentTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners)
            : base(diagnosticService, notificationService, new AggregateAsynchronousOperationListener(listeners, FeatureAttribute.ErrorSquiggles))
        {
        }

        protected sealed internal override bool IsEnabled => true;

        protected sealed internal override ITagSpan<TTag> CreateTagSpan(
            bool isLiveUpdate, SnapshotSpan span, DiagnosticData data)
        {
            var errorTag = CreateTag(data);
            if (errorTag == null)
            {
                return null;
            }

            // Live update squiggles have to be at least 1 character long.
            var minimumLength = isLiveUpdate ? 1 : 0;
            var adjustedSpan = AdjustSnapshotSpan(span, minimumLength);
            if (adjustedSpan.Length == 0)
            {
                return null;
            }

            return new TagSpan<TTag>(adjustedSpan, errorTag);
        }

        protected virtual SnapshotSpan AdjustSnapshotSpan(SnapshotSpan span, int minimumLength)
        {
            var snapshot = span.Snapshot;

            // new length
            var length = Math.Max(span.Length, minimumLength);

            // make sure start + length is smaller than snapshot.Length and start is >= 0
            var start = Math.Max(0, Math.Min(span.Start, snapshot.Length - length));

            // make sure length is smaller than snapshot.Length which can happen if start == 0
            return new SnapshotSpan(snapshot, start, Math.Min(start + length, snapshot.Length) - start);
        }

        protected abstract TTag CreateTag(DiagnosticData diagnostic);
    }
}