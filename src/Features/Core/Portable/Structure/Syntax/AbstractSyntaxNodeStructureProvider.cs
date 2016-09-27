// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxNodeStructureProvider<TSyntaxNode> : AbstractSyntaxStructureProvider
        where TSyntaxNode : SyntaxNode
    {
        public override void CollectBlockSpans(
            Document document,
            SyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            if (!SupportedInWorkspaceKind(document.Project.Solution.Workspace.Kind))
            {
                return;
            }

            CollectBlockSpans(node, spans, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            if (node is TSyntaxNode)
            {
                CollectBlockSpans((TSyntaxNode)node, spans, cancellationToken);
            }
        }

        protected virtual bool SupportedInWorkspaceKind(string kind)
        {
            // We have other outliners specific to Metadata-as-Source.
            return kind != WorkspaceKind.MetadataAsSource;
        }

        protected abstract void CollectBlockSpans(
            TSyntaxNode node, ArrayBuilder<BlockSpan> spans, CancellationToken cancellationToken);
    }
}