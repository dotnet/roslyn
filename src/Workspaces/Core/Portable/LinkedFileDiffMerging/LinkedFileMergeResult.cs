// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal readonly struct LinkedFileMergeResult(ImmutableArray<DocumentId> documentIds, SourceText mergedSourceText, ImmutableArray<TextSpan> mergeConflictResolutionSpans)
{
    public readonly ImmutableArray<DocumentId> DocumentIds = documentIds;
    public readonly SourceText MergedSourceText = mergedSourceText;
    public readonly ImmutableArray<TextSpan> MergeConflictResolutionSpans = mergeConflictResolutionSpans;
    public bool HasMergeConflicts => !MergeConflictResolutionSpans.IsEmpty;
}
