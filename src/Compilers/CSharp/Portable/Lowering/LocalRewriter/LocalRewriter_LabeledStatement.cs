// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenBody = VisitStatement(node.Body);
            return MakeLabeledStatement(node, rewrittenBody);
        }

        private BoundStatement MakeLabeledStatement(BoundLabeledStatement node, BoundStatement? rewrittenBody)
        {
            BoundStatement labelStatement = new BoundLabelStatement(node.Syntax, node.Label);

            if (this.Instrument)
            {
                var labeledSyntax = node.Syntax as LabeledStatementSyntax;
                if (labeledSyntax != null)
                {
                    labelStatement = Instrumenter.InstrumentLabelStatement(node, labelStatement);
                }
            }

            if (rewrittenBody == null)
            {
                // Body may be null if the body has no associated IL
                // (declaration with no initializer for instance.)
                return labelStatement;
            }

            return BoundStatementList.Synthesized(node.Syntax, labelStatement, rewrittenBody);
        }
    }
}
