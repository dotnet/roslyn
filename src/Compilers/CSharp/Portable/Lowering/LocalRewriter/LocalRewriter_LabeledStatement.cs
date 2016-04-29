// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var rewrittenBody = (BoundStatement)Visit(node.Body);

            BoundStatement labelStatement = new BoundLabelStatement(node.Syntax, node.Label);

            if (this.Instrument)
            {
                var labeledSyntax = node.Syntax as LabeledStatementSyntax;
                if (labeledSyntax != null)
                {
                    labelStatement = _instrumenter.InstrumentLabelStatement(node, labelStatement); 
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
