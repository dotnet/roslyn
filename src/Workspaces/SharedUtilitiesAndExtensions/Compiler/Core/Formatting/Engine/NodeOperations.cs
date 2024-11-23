// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// this collector gathers formatting operations that are based on a node
/// </summary>
internal sealed class NodeOperations : IDisposable
{
    private static readonly ObjectPool<SegmentedList<IndentBlockOperation>> s_indentBlockOperationPool = new(() => []);
    private static readonly ObjectPool<SegmentedList<SuppressOperation>> s_suppressOperationPool = new(() => []);
    private static readonly ObjectPool<SegmentedList<AlignTokensOperation>> s_alignTokensOperationPool = new(() => []);
    private static readonly ObjectPool<SegmentedList<AnchorIndentationOperation>> s_anchorIndentationOperationPool = new(() => []);

    public static NodeOperations Empty = new();

    public SegmentedList<IndentBlockOperation> IndentBlockOperation { get; } = s_indentBlockOperationPool.Allocate();
    public SegmentedList<SuppressOperation> SuppressOperation { get; } = s_suppressOperationPool.Allocate();
    public SegmentedList<AlignTokensOperation> AlignmentOperation { get; } = s_alignTokensOperationPool.Allocate();
    public SegmentedList<AnchorIndentationOperation> AnchorIndentationOperations { get; } = s_anchorIndentationOperationPool.Allocate();

    public void Dispose()
    {
        if (this == Empty)
            return;

        // Intentionally don't call ClearAndFree as these pooled lists can easily exceed the threshold
        IndentBlockOperation.Clear();
        s_indentBlockOperationPool.Free(IndentBlockOperation);

        SuppressOperation.Clear();
        s_suppressOperationPool.Free(SuppressOperation);

        AlignmentOperation.Clear();
        s_alignTokensOperationPool.Free(AlignmentOperation);

        AnchorIndentationOperations.Clear();
        s_anchorIndentationOperationPool.Free(AnchorIndentationOperations);
    }
}
