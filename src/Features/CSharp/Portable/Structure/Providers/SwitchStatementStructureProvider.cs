// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class SwitchStatementStructureProvider : AbstractSyntaxNodeStructureProvider<SwitchStatementSyntax>
    {
        protected override void CollectBlockSpans(
            SwitchStatementSyntax node,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            const bool includeInternalStructures = true;
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(node.CloseParenToken.Span.End, node.CloseBraceToken.Span.End),
                hintSpan: node.Span,
                type: BlockTypes.Conditional));
            if (includeInternalStructures)
            {
                GetInternalStructures(node, spans, options, cancellationToken);
            }
        }

        private void GetInternalStructures(SwitchStatementSyntax node, ArrayBuilder<BlockSpan> spans, OptionSet options, CancellationToken cancellationToken)
        {
            foreach (SwitchSectionSyntax switchcase in node.Sections.AsImmutable())
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var hintText = switchcase.Labels[0].ToString();
                var block = CSharpStructureHelpers.CreateBlockSpan(switchcase, hintText , false, BlockTypes.Conditional, true);
                spans.Add(block);
            }
        }
    }
}
