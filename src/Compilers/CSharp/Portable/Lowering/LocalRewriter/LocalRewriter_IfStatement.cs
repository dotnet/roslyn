// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

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

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            var result = RewriteIfStatement(syntax, AddConditionSequencePoint(rewrittenCondition, node), rewrittenConsequence, rewrittenAlternative, node.HasErrors);

            // add sequence point before the whole statement
            if (this.GenerateDebugInfo && !node.WasCompilerGenerated)
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
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            if (rewrittenAlternativeOpt == null)
            {
                // if (condition) 
                //   consequence;  
                //
                // becomes
                //
                // GotoIfFalse condition afterif;
                // consequence;
                // afterif:

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, afterif));
                builder.Add(rewrittenConsequence);
            }
            else
            {
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

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, alt));
                builder.Add(rewrittenConsequence);
                builder.Add(new BoundGotoStatement(syntax, afterif));
                builder.Add(new BoundLabelStatement(syntax, alt));
                builder.Add(rewrittenAlternativeOpt);
            }

            builder.Add(new BoundLabelStatement(syntax, afterif));

            return new BoundStatementList(syntax, builder.ToImmutableAndFree(), hasErrors);
        }
    }
}
