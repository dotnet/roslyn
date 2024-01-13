// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
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
            Debug.Assert(rewrittenConsequence is { });
            var rewrittenAlternative = VisitStatement(node.AlternativeOpt);
            var syntax = (IfStatementSyntax)node.Syntax;

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                rewrittenCondition = Instrumenter.InstrumentIfStatementCondition(node, rewrittenCondition, _factory);
            }

            var result = RewriteIfStatement(syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.HasErrors);

            // add sequence point before the whole statement
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = Instrumenter.InstrumentIfStatement(node, result);
            }

            return result;
        }

        private static BoundStatement RewriteIfStatement(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement? rewrittenAlternativeOpt,
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
                builder.Add(BoundSequencePoint.CreateHidden());
                builder.Add(new BoundLabelStatement(syntax, afterif));
                var statements = builder.ToImmutableAndFree();
                return new BoundStatementList(syntax, statements, hasErrors);
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
                builder.Add(BoundSequencePoint.CreateHidden());
                builder.Add(new BoundGotoStatement(syntax, afterif));
                builder.Add(new BoundLabelStatement(syntax, alt));
                builder.Add(rewrittenAlternativeOpt);
                builder.Add(BoundSequencePoint.CreateHidden());
                builder.Add(new BoundLabelStatement(syntax, afterif));
                return new BoundStatementList(syntax, builder.ToImmutableAndFree(), hasErrors);
            }

        }
    }
}
