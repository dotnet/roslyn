// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, diagnosticService, notificationService, listenerProvider.GetListener(FeatureAttribute.ErrorSquiggles))
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
            => AdjustSnapshotSpan(span, minimumLength, int.MaxValue);

        protected SnapshotSpan AdjustSnapshotSpan(SnapshotSpan span, int minimumLength, int maximumLength)
        {
            var snapshot = span.Snapshot;

            // new length
            var length = Math.Min(Math.Max(span.Length, minimumLength), maximumLength);

            // make sure start + length is smaller than snapshot.Length and start is >= 0
            var start = Math.Max(0, Math.Min(span.Start, snapshot.Length - length));

            // make sure length is smaller than snapshot.Length which can happen if start == 0
            return new SnapshotSpan(snapshot, start, Math.Min(start + length, snapshot.Length) - start);
        }

        protected abstract TTag CreateTag(DiagnosticData diagnostic);
    }
}
