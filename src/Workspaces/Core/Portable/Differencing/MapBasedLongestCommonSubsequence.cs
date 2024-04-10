// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Differencing;

internal sealed class MapBasedLongestCommonSubsequence<TNode>(IReadOnlyDictionary<TNode, TNode> map) : LongestCommonSubsequence<IReadOnlyList<TNode>>
    where TNode : notnull
{
    protected override bool ItemsEqual(IReadOnlyList<TNode> oldSequence, int oldIndex, IReadOnlyList<TNode> newSequence, int newIndex)
        => map.TryGetValue(oldSequence[oldIndex], out var newNode) && newNode.Equals(newSequence[newIndex]);

    internal IEnumerable<Edit<TNode>> GetEdits(IReadOnlyList<TNode> oldNodes, IReadOnlyList<TNode> newNodes, TreeComparer<TNode>? treeComparer = null)
    {
        foreach (var edit in GetEdits(oldNodes, oldNodes.Count, newNodes, newNodes.Count))
        {
            yield return new Edit<TNode>(edit.Kind, treeComparer,
                edit.OldIndex >= 0 ? oldNodes[edit.OldIndex] : default!,
                edit.NewIndex >= 0 ? newNodes[edit.NewIndex] : default!);
        }
    }
}
