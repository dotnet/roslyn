﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
