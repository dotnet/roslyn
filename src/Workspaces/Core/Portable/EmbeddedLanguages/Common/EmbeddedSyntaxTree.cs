// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal abstract class EmbeddedSyntaxTree<TSyntaxKind, TSyntaxNode, TCompilationUnitSyntax>
        where TSyntaxKind : struct
        where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
        where TCompilationUnitSyntax : TSyntaxNode
    {
        public readonly VirtualCharSequence Text;
        public readonly TCompilationUnitSyntax Root;
        public readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

        protected EmbeddedSyntaxTree(
            VirtualCharSequence text,
            TCompilationUnitSyntax root,
            ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            Text = text;
            Root = root;
            Diagnostics = diagnostics;
        }
    }
}
