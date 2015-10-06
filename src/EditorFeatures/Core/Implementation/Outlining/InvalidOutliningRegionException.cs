using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal class InvalidOutliningRegionException : Exception
    {
        private readonly IOutliningService _service;
        private readonly ITextSnapshot _snapshot;
        private readonly Span _snapshotSpan;
        private readonly Span _regionSpan;

        public InvalidOutliningRegionException(IOutliningService service, ITextSnapshot snapshot, Span snapshotSpan, Span regionSpan)
            : base(GetExceptionMessage(service, snapshot, snapshotSpan, regionSpan))
        {
            _service = service;
            _snapshot = snapshot;
            _snapshotSpan = snapshotSpan;
            _regionSpan = regionSpan;
        }

        private static string GetExceptionMessage(IOutliningService service, ITextSnapshot snapshot, Span snapshotSpan, Span regionSpan)
        {
            return $"OutliningService({service.GetType()}) produced an invalid region.  ITextSnapshot span is {snapshotSpan}. OutliningSpan is {regionSpan}.";
        }
    }
}