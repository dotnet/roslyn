// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class LinkedFileMergeResult
    {
        public IEnumerable<DocumentId> DocumentIds { get; internal set; }
        public SourceText MergedSourceText { get; internal set; }
        public IEnumerable<TextSpan> MergeConflictResolutionSpans { get; }
        public bool HasMergeConflicts { get { return MergeConflictResolutionSpans.Any(); } }

        public LinkedFileMergeResult(IEnumerable<DocumentId> documentIds, SourceText mergedSourceText, IEnumerable<TextSpan> mergeConflictResolutionSpans)
        {
            DocumentIds = documentIds;
            MergedSourceText = mergedSourceText;
            MergeConflictResolutionSpans = mergeConflictResolutionSpans;
        }
    }
}
