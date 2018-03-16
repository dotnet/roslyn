// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal abstract class EmbeddedSyntaxTree<TNode, TRoot>
        where TNode : EmbeddedSyntaxNode<TNode>
        where TRoot : TNode
    {
        public readonly ImmutableArray<VirtualChar> Text;
        public readonly TRoot Root;
        public readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

        protected EmbeddedSyntaxTree(
            ImmutableArray<VirtualChar> text,
            TRoot root,
            ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            Text = text;
            Root = root;
            Diagnostics = diagnostics;
        }
    }
}
