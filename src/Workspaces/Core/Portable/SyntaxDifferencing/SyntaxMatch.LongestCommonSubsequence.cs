// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    internal partial class SyntaxMatch
    {
        internal sealed class LongestCommonSubsequence : LongestCommonSubsequence<IReadOnlyList<SyntaxNode>>
        {
            private readonly SyntaxMatch _match;

            internal LongestCommonSubsequence(SyntaxMatch match)
            {
                Debug.Assert(match != null);
                _match = match;
            }

            protected override bool ItemsEqual(
                IReadOnlyList<SyntaxNode> oldSequence, int oldIndex, 
                IReadOnlyList<SyntaxNode> newSequence, int newIndex)
            {
                return _match.Contains(oldSequence[oldIndex], newSequence[newIndex]);
            }

            internal Dictionary<SyntaxNode, SyntaxNode> GetMatchingNodes(
                IReadOnlyList<SyntaxNode> oldNodes,
                IReadOnlyList<SyntaxNode> newNodes)
            {
                var result = new Dictionary<SyntaxNode, SyntaxNode>();

                foreach (var pair in GetMatchingPairs(oldNodes, oldNodes.Count, newNodes, newNodes.Count))
                {
                    result.Add(oldNodes[pair.Key], newNodes[pair.Value]);
                }

                return result;
            }

            internal IEnumerable<SyntaxEdit> GetEdits(
                IReadOnlyList<SyntaxNode> oldNodes,
                IReadOnlyList<SyntaxNode> newNodes)
            {
                foreach (var edit in GetEdits(oldNodes, oldNodes.Count, newNodes, newNodes.Count))
                {
                    yield return new SyntaxEdit(edit.Kind, _match.Comparer,
                        edit.OldIndex >= 0 ? oldNodes[edit.OldIndex] : default(SyntaxNode),
                        edit.NewIndex >= 0 ? newNodes[edit.NewIndex] : default(SyntaxNode));
                }
            }
        }
    }
}