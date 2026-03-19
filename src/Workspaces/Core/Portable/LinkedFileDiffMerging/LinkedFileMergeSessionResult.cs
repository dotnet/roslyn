// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed class LinkedFileMergeSessionResult
{
    public Solution MergedSolution { get; }

    public readonly Dictionary<DocumentId, ImmutableArray<TextSpan>> MergeConflictCommentSpans = [];

    public LinkedFileMergeSessionResult(Solution mergedSolution, ArrayBuilder<LinkedFileMergeResult> fileMergeResults)
    {
        this.MergedSolution = mergedSolution;

        foreach (var fileMergeResult in fileMergeResults)
        {
            foreach (var documentId in fileMergeResult.DocumentIds)
                MergeConflictCommentSpans.Add(documentId, fileMergeResult.MergeConflictResolutionSpans);
        }
    }
}
