// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            if (!SupportedInWorkspaceKind(document.Project.Solution.Workspace.Kind))
            {
                return;
            }

            var options = document.Project.Solution.Options;
            CollectBlockSpans(node, spans, options, cancellationToken);
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
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (node is TSyntaxNode tSyntax)
            {
                CollectBlockSpans(tSyntax, spans, options, cancellationToken);
            }
        }

        protected virtual bool SupportedInWorkspaceKind(string kind)
        {
            // We have other outliners specific to Metadata-as-Source.
            return kind != WorkspaceKind.MetadataAsSource;
        }

        protected abstract void CollectBlockSpans(
            TSyntaxNode node, ArrayBuilder<BlockSpan> spans,
            OptionSet options, CancellationToken cancellationToken);
    }
}
