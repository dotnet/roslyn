// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal abstract class AbstractSyntaxNodeOutliner<TSyntaxNode> : AbstractSyntaxOutliner
        where TSyntaxNode : SyntaxNode
    {
        public override void CollectOutliningSpans(
            Document document,
            SyntaxNode node,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            if (!SupportedInWorkspaceKind(document.Project.Solution.Workspace.Kind))
            {
                return;
            }

            CollectOutliningSpans(node, spans, cancellationToken);
        }

        public sealed override void CollectOutliningSpans(
            Document document,
            SyntaxTrivia trivia,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private void CollectOutliningSpans(SyntaxNode node, List<OutliningSpan> spans, CancellationToken cancellationToken)
        {
            if (node is TSyntaxNode)
            {
                CollectOutliningSpans((TSyntaxNode)node, spans, cancellationToken);
            }
        }

        // For testing purposes
        internal IEnumerable<OutliningSpan> GetOutliningSpans(SyntaxNode node, CancellationToken cancellationToken)
        {
            var spans = new List<OutliningSpan>();
            this.CollectOutliningSpans(node, spans, cancellationToken);
            return spans;
        }

        protected virtual bool SupportedInWorkspaceKind(string kind)
        {
            // We have other outliners specific to Metadata-as-Source.
            return kind != WorkspaceKind.MetadataAsSource;
        }

        protected abstract void CollectOutliningSpans(TSyntaxNode node, List<OutliningSpan> spans, CancellationToken cancellationToken);
    }
}
