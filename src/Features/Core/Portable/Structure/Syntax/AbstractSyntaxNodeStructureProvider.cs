// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxNodeStructureProvider<TSyntaxNode> : AbstractSyntaxStructureProvider
        where TSyntaxNode : SyntaxNode
    {
        public sealed override void CollectBlockSpans(
            Document document,
            SyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            var isMetadataAsSource = document.Project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource;
            var options = document.Project.Solution.Options;
            CollectBlockSpans(node, spans, isMetadataAsSource, options, cancellationToken);
        }

        public sealed override void CollectBlockSpans(
            Document document,
            SyntaxTrivia trivia,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private void CollectBlockSpans(
            SyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (node is TSyntaxNode tSyntax)
            {
                CollectBlockSpans(tSyntax, spans, isMetadataAsSource, options, cancellationToken);
            }
        }

        protected abstract void CollectBlockSpans(
            TSyntaxNode node, ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource, OptionSet options, CancellationToken cancellationToken);
    }
}
