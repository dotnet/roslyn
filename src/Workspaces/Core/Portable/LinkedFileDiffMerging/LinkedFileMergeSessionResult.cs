// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class LinkedFileMergeSessionResult
    {
        public Solution MergedSolution { get; }

        private readonly Dictionary<DocumentId, IEnumerable<TextSpan>> _mergeConflictCommentSpans = new();
        public Dictionary<DocumentId, IEnumerable<TextSpan>> MergeConflictCommentSpans => _mergeConflictCommentSpans;

        public LinkedFileMergeSessionResult(Solution mergedSolution, IEnumerable<LinkedFileMergeResult> fileMergeResults)
        {
            this.MergedSolution = mergedSolution;

            foreach (var fileMergeResult in fileMergeResults)
            {
                foreach (var documentId in fileMergeResult.DocumentIds)
                {
                    _mergeConflictCommentSpans.Add(documentId, fileMergeResult.MergeConflictResolutionSpans);
                }
            }
        }
    }
}
