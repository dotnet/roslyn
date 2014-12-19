// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class LinkedFileMergeSessionResult
    {
        public Solution MergedSolution { get; private set; }

        private readonly Dictionary<DocumentId, IEnumerable<TextSpan>> mergeConflictCommentSpans = new Dictionary<DocumentId, IEnumerable<TextSpan>>();
        public Dictionary<DocumentId, IEnumerable<TextSpan>> MergeConflictCommentSpans { get { return mergeConflictCommentSpans; } }

        public LinkedFileMergeSessionResult(Solution mergedSolution, IEnumerable<LinkedFileMergeResult> fileMergeResults)
        {
            this.MergedSolution = mergedSolution;

            foreach (var fileMergeResult in fileMergeResults)
            {
                foreach (var documentId in fileMergeResult.DocumentIds)
                {
                    mergeConflictCommentSpans.Add(documentId, fileMergeResult.MergeConflictResolutionSpans);
                }
            }
        }
    }
}