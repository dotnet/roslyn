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
        private readonly bool includeInternalStructures;
        internal SwitchStatementStructureProvider(bool includeInternalStructures)
        {
            this.includeInternalStructures = includeInternalStructures;
        }

        protected override void CollectBlockSpans(SwitchStatementSyntax node, ArrayBuilder<BlockSpan> spans, OptionSet options, CancellationToken cancellationToken)
        {
            spans.Add(new BlockSpan(
               isCollapsible: true,
               textSpan: TextSpan.FromBounds(node.CloseParenToken.Span.End, node.CloseBraceToken.Span.End),
               hintSpan: node.Span,
               type: BlockTypes.Conditional));
            if (includeInternalStructures)
            {
                foreach (SwitchSectionSyntax switchcase in node.Sections.AsImmutable())
                {
                    var s = new BlockSpan(
                                          isCollapsible: true,
                                          textSpan: TextSpan.FromBounds(switchcase.SpanStart, switchcase.Span.End),
                                          hintSpan: switchcase.Span,
                                          type: BlockTypes.Conditional);
                    spans.Add(s);
                }
            }
        }
    }
}
