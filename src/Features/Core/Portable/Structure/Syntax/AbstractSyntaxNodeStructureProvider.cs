﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxNodeStructureProvider<TSyntaxNode> : AbstractSyntaxStructureProvider
        where TSyntaxNode : SyntaxNode
    {
        public sealed override void CollectBlockSpans(
            SyntaxTrivia trivia,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public sealed override void CollectBlockSpans(
            SyntaxNode node,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider,
            CancellationToken cancellationToken)
        {
            if (node is TSyntaxNode tSyntax)
            {
                CollectBlockSpans(tSyntax, ref spans, optionProvider, cancellationToken);
            }
        }

        protected abstract void CollectBlockSpans(
            TSyntaxNode node, ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider, CancellationToken cancellationToken);
    }
}
