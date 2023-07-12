// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class LinkedFileMergeResult(IEnumerable<DocumentId> documentIds, SourceText mergedSourceText, IEnumerable<TextSpan> mergeConflictResolutionSpans)
    {
        public IEnumerable<DocumentId> DocumentIds { get; internal set; } = documentIds;
        public SourceText MergedSourceText { get; internal set; } = mergedSourceText;
        public IEnumerable<TextSpan> MergeConflictResolutionSpans { get; } = mergeConflictResolutionSpans;
        public bool HasMergeConflicts { get { return MergeConflictResolutionSpans.Any(); } }
    }
}
