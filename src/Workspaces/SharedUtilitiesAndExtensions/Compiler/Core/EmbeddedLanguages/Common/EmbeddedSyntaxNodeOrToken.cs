// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>
        where TSyntaxKind : struct
        where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    {
        public readonly TSyntaxNode Node;
        public readonly EmbeddedSyntaxToken<TSyntaxKind> Token;

        private EmbeddedSyntaxNodeOrToken(TSyntaxNode node) : this()
        {
            Debug.Assert(node != null);
            Node = node;
        }

        private EmbeddedSyntaxNodeOrToken(EmbeddedSyntaxToken<TSyntaxKind> token) : this()
        {
            Debug.Assert((int)(object)token.Kind != 0);
            Token = token;
        }

        public bool IsNode => Node != null;

        public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(TSyntaxNode node)
            => new EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(node);

        public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(EmbeddedSyntaxToken<TSyntaxKind> token)
            => new EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(token);
    }
}
