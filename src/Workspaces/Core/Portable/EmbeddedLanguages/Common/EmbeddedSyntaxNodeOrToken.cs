// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxNodeOrToken<TNode> where TNode : EmbeddedSyntaxNode<TNode>
    {
        public readonly TNode Node;
        public readonly EmbeddedSyntaxToken Token;

        private EmbeddedSyntaxNodeOrToken(TNode node) : this()
        {
            Debug.Assert(node != null);
            Node = node;
        }

        private EmbeddedSyntaxNodeOrToken(EmbeddedSyntaxToken token) : this()
        {
            Debug.Assert(token.RawKind != 0);
            Token = token;
        }

        public bool IsNode => Node != null;

        public static implicit operator EmbeddedSyntaxNodeOrToken<TNode>(TNode node)
            => new EmbeddedSyntaxNodeOrToken<TNode>(node);

        public static implicit operator EmbeddedSyntaxNodeOrToken<TNode>(EmbeddedSyntaxToken token)
            => new EmbeddedSyntaxNodeOrToken<TNode>(token);
    }
}
