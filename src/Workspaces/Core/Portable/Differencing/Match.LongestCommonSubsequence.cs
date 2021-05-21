// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Differencing
{
    public partial class Match<TNode>
    {
        internal sealed class LongestCommonSubsequence : LongestCommonSubsequence<IReadOnlyList<TNode>>
        {
            private readonly Match<TNode> _match;

            internal LongestCommonSubsequence(Match<TNode> match)
            {
                Debug.Assert(match != null);
                _match = match;
            }

            protected override bool ItemsEqual(IReadOnlyList<TNode> oldSequence, int oldIndex, IReadOnlyList<TNode> newSequence, int newIndex)
                => _match.Contains(oldSequence[oldIndex], newSequence[newIndex]);

            internal Dictionary<TNode, TNode> GetMatchingNodes(IReadOnlyList<TNode> oldNodes, IReadOnlyList<TNode> newNodes)
            {
                var result = new Dictionary<TNode, TNode>();

                foreach (var pair in GetMatchingPairs(oldNodes, oldNodes.Count, newNodes, newNodes.Count))
                {
                    result.Add(oldNodes[pair.Key], newNodes[pair.Value]);
                }

                return result;
            }

            internal IEnumerable<Edit<TNode>> GetEdits(IReadOnlyList<TNode> oldNodes, IReadOnlyList<TNode> newNodes)
            {
                foreach (var edit in GetEdits(oldNodes, oldNodes.Count, newNodes, newNodes.Count))
                {
                    yield return new Edit<TNode>(edit.Kind, _match.Comparer,
                        edit.OldIndex >= 0 ? oldNodes[edit.OldIndex] : default,
                        edit.NewIndex >= 0 ? newNodes[edit.NewIndex] : default);
                }
            }
        }
    }
}
