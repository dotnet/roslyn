// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            Debug.Assert(node != null);
            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenConsequence = VisitStatement(node.Consequence);
            var rewrittenAlternative = VisitStatement(node.AlternativeOpt);
            var syntax = (IfStatementSyntax)node.Syntax;
            var result = RewriteIfStatement(syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.HasErrors);

            // add sequence point before the whole statement
            if (this.generateDebugInfo && !node.WasCompilerGenerated)
            {
                result = new BoundSequencePointWithSpan(
                    syntax,
                    result,
                    TextSpan.FromBounds(
                        syntax.IfKeyword.SpanStart,
                        syntax.CloseParenToken.Span.End),
                    node.HasErrors);
            }

            return result;
        }

        private static BoundStatement RewriteIfStatement(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement rewrittenAlternativeOpt,
            bool hasErrors)
        {
            var afterif = new GeneratedLabelSymbol("afterif");

            // if (condition) 
            //   consequence;  
            //
            // becomes
            //
            // GotoIfFalse condition afterif;
            // consequence;
            // afterif:

            if (rewrittenAlternativeOpt == null)
            {
                return BoundStatementList.Synthesized(syntax,
                    new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, afterif),
                    rewrittenConsequence,
                    new BoundLabelStatement(syntax, afterif));
            }

            // if (condition)
            //     consequence;
            // else 
            //     alternative
            //
            // becomes
            //
            // GotoIfFalse condition alt;
            // consequence
            // goto afterif;
            // alt:
            // alternative;
            // afterif:

            var alt = new GeneratedLabelSymbol("alternative");
            return BoundStatementList.Synthesized(syntax, hasErrors,
                new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, alt),
                rewrittenConsequence,
                new BoundGotoStatement(syntax, afterif),
                new BoundLabelStatement(syntax, alt),
                rewrittenAlternativeOpt,
                new BoundLabelStatement(syntax, afterif));
        }
    }
}
