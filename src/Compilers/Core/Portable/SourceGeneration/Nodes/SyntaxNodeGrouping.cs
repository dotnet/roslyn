// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial struct SyntaxValueProvider
{
    /// <summary>
    /// Wraps a grouping of nodes within a syntax tree so we can have value-semantics around them usable by the
    /// incremental driver.  Note: we do something very sneaky here.  Specifically, as long as we have the same <see
    /// cref="SyntaxTree"/> from before, then we know we must have the same nodes as before (since the nodes are
    /// entirely determined from the text+options which is exactly what the syntax tree represents).  Similarly, if the
    /// syntax tree changes, we will always get different nodes (since they point back at the syntax tree).  So we can
    /// just use the syntax tree itself to determine value semantics here.
    /// </summary>
    private class SyntaxNodeGrouping<TSyntaxNode> : IEquatable<SyntaxNodeGrouping<TSyntaxNode>>
        where TSyntaxNode : SyntaxNode
    {
        public readonly SyntaxTree SyntaxTree;
        public readonly ImmutableArray<TSyntaxNode> SyntaxNodes;

        public SyntaxNodeGrouping(IGrouping<SyntaxTree, TSyntaxNode> grouping)
        {
            SyntaxTree = grouping.Key;
            SyntaxNodes = grouping.OrderBy(static n => n.FullSpan.Start).ToImmutableArray();
        }

        public override int GetHashCode()
            => SyntaxTree.GetHashCode();

        public override bool Equals(object? obj)
            => Equals(obj as SyntaxNodeGrouping<TSyntaxNode>);

        public bool Equals(SyntaxNodeGrouping<TSyntaxNode>? obj)
            => this.SyntaxTree == obj?.SyntaxTree;
    }
}
