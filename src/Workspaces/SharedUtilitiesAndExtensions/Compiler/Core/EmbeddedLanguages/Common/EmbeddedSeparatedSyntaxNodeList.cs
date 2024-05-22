// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

internal readonly struct EmbeddedSeparatedSyntaxNodeList<TSyntaxKind, TSyntaxNode, TDerivedNode>
    where TSyntaxKind : struct
    where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    where TDerivedNode : TSyntaxNode
{
    public ImmutableArray<EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>> NodesAndTokens { get; }
    public int Length { get; }
    public int SeparatorLength { get; }

    public static readonly EmbeddedSeparatedSyntaxNodeList<TSyntaxKind, TSyntaxNode, TDerivedNode> Empty
        = new([]);

    public EmbeddedSeparatedSyntaxNodeList(
        ImmutableArray<EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>> nodesAndTokens)
    {
        Contract.ThrowIfTrue(nodesAndTokens.IsDefault);
        NodesAndTokens = nodesAndTokens;

        var allLength = NodesAndTokens.Length;
        Length = (allLength + 1) / 2;
        SeparatorLength = allLength / 2;

        Verify();
    }

    [Conditional("DEBUG")]
    private void Verify()
    {
        for (var i = 0; i < NodesAndTokens.Length; i++)
        {
            if ((i & 1) == 0)
            {
                // All even values should be TNode
                Debug.Assert(NodesAndTokens[i].IsNode);
                Debug.Assert(NodesAndTokens[i].Node is EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>);
            }
            else
            {
                // All odd values should be separator tokens 
                Debug.Assert(!NodesAndTokens[i].IsNode);
            }
        }
    }

    /// <summary>
    /// Retrieves only nodes, skipping the separator tokens
    /// </summary>
    public TDerivedNode this[int index]
    {
        get
        {
            if (index < Length && index >= 0)
            {
                // x2 here to get only even indexed numbers. Follows same logic 
                // as SeparatedSyntaxList in that the separator tokens are not returned
                var nodeOrToken = NodesAndTokens[index * 2];
                Debug.Assert(nodeOrToken.IsNode);
                RoslynDebug.AssertNotNull(nodeOrToken.Node);
                return (TDerivedNode)nodeOrToken.Node;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator(EmbeddedSeparatedSyntaxNodeList<TSyntaxKind, TSyntaxNode, TDerivedNode> list)
    {
        private int _currentIndex = -1;

        public TDerivedNode Current { get; private set; } = null!;

        public bool MoveNext()
        {
            _currentIndex++;
            if (_currentIndex >= list.Length)
            {
                Current = null!;
                return false;
            }

            Current = list[_currentIndex];
            return true;
        }
    }
}
