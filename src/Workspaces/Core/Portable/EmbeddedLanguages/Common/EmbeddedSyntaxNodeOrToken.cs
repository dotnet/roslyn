// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxNodeOrToken<TSyntaxKind, TNode>
        where TSyntaxKind : struct
        where TNode : EmbeddedSyntaxNode<TSyntaxKind, TNode>
    {
        public readonly TNode Node;
        public readonly EmbeddedSyntaxToken<TSyntaxKind> Token;

        private EmbeddedSyntaxNodeOrToken(TNode node) : this()
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

        public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TNode>(TNode node)
            => new EmbeddedSyntaxNodeOrToken<TSyntaxKind, TNode>(node);

        public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TNode>(EmbeddedSyntaxToken<TSyntaxKind> token)
            => new EmbeddedSyntaxNodeOrToken<TSyntaxKind, TNode>(token);
    }
}
