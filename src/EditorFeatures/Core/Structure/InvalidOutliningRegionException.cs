// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    internal class InvalidOutliningRegionException : Exception
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly BlockStructureService _service;
        private readonly ITextSnapshot _snapshot;
        private readonly Span _snapshotSpan;
        private readonly Span _regionSpan;
#pragma warning restore IDE0052 // Remove unread private members

        public InvalidOutliningRegionException(BlockStructureService service, ITextSnapshot snapshot, Span snapshotSpan, Span regionSpan)
            : base(GetExceptionMessage(service, snapshotSpan, regionSpan))
        {
            _service = service;
            _snapshot = snapshot;
            _snapshotSpan = snapshotSpan;
            _regionSpan = regionSpan;
        }

        private static string GetExceptionMessage(BlockStructureService service, Span snapshotSpan, Span regionSpan)
            => $"OutliningService({service.GetType()}) produced an invalid region.  ITextSnapshot span is {snapshotSpan}. OutliningSpan is {regionSpan}.";
    }
}
