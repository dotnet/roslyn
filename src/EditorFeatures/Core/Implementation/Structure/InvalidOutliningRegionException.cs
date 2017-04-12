// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    internal class InvalidOutliningRegionException : Exception
    {
        private readonly BlockStructureService _service;
        private readonly ITextSnapshot _snapshot;
        private readonly Span _snapshotSpan;
        private readonly Span _regionSpan;

        public InvalidOutliningRegionException(BlockStructureService service, ITextSnapshot snapshot, Span snapshotSpan, Span regionSpan)
            : base(GetExceptionMessage(service, snapshot, snapshotSpan, regionSpan))
        {
            _service = service;
            _snapshot = snapshot;
            _snapshotSpan = snapshotSpan;
            _regionSpan = regionSpan;
        }

        private static string GetExceptionMessage(BlockStructureService service, ITextSnapshot snapshot, Span snapshotSpan, Span regionSpan)
        {
            return $"OutliningService({service.GetType()}) produced an invalid region.  ITextSnapshot span is {snapshotSpan}. OutliningSpan is {regionSpan}.";
        }
    }
}