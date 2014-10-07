// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Differencing
{
    public partial class Match<TNode>
    {
        internal sealed class LongestCommonSubsequence : LongestCommonSubsequence<IReadOnlyList<TNode>>
        {
            private readonly Match<TNode> match;

            internal LongestCommonSubsequence(Match<TNode> match)
            {
                Debug.Assert(match != null);
                this.match = match;
            }

            protected override bool ItemsEqual(IReadOnlyList<TNode> sequenceA, int indexA, IReadOnlyList<TNode> sequenceB, int indexB)
            {
                return match.Contains(sequenceA[indexA], sequenceB[indexB]);
            }

            internal Dictionary<TNode, TNode> GetMatchingNodes(IReadOnlyList<TNode> nodes1, IReadOnlyList<TNode> nodes2)
            {
                var result = new Dictionary<TNode, TNode>();

                foreach (var pair in GetMatchingPairs(nodes1, nodes1.Count, nodes2, nodes2.Count))
                {
                    result.Add(nodes1[pair.Key], nodes2[pair.Value]);
                }

                return result;
            }

            internal IEnumerable<Edit<TNode>> GetEdits(IReadOnlyList<TNode> nodes1, IReadOnlyList<TNode> nodes2)
            {
                foreach (var edit in GetEdits(nodes1, nodes1.Count, nodes2, nodes2.Count))
                {
                    yield return new Edit<TNode>(edit.Kind, match.Comparer,
                        edit.IndexA >= 0 ? nodes1[edit.IndexA] : default(TNode),
                        edit.IndexB >= 0 ? nodes2[edit.IndexB] : default(TNode));
                }
            }
        }
    }
}
