// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public abstract class AbstractSyntaxNodeOutlinerTests<TSyntaxNode> : AbstractSyntaxOutlinerTests
        where TSyntaxNode : SyntaxNode
    {
        internal abstract AbstractSyntaxOutliner CreateOutliner();

        internal sealed override OutliningSpan[] GetRegions(Document document, int position)
        {
            var root = document.GetSyntaxRootAsync(CancellationToken.None).Result;
            var token = root.FindToken(position, findInsideTrivia: true);
            var node = token.Parent.FirstAncestorOrSelf<TSyntaxNode>();
            Assert.NotNull(node);

            // We prefer ancestor nodes if the position is on the edge of the located node's span.
            while (node.Parent is TSyntaxNode)
            {
                if ((position == node.SpanStart && position == node.Parent.SpanStart) ||
                    (position == node.Span.End && position == node.Parent.Span.End))
                {
                    node = (TSyntaxNode)node.Parent;
                }
                else
                {
                    break;
                }
            }

            var outliner = CreateOutliner();
            var actualRegions = new List<OutliningSpan>();
            outliner.CollectOutliningSpans(document, node, actualRegions, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.WhereNotNull().ToArray();
        }
    }
}
