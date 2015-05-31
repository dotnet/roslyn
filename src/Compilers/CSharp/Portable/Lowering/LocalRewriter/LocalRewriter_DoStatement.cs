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
        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenBody = (BoundStatement)Visit(node.Body);
            var startLabel = new GeneratedLabelSymbol("start");

            var syntax = node.Syntax;

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(syntax, AddConditionSequencePoint(rewrittenCondition, node), true, startLabel);

            if (this.GenerateDebugInfo)
            {
                var doSyntax = (DoStatementSyntax)syntax;
                var span = TextSpan.FromBounds(
                    doSyntax.WhileKeyword.SpanStart,
                    doSyntax.SemicolonToken.Span.End);

                ifConditionGotoStart = new BoundSequencePointWithSpan(doSyntax, ifConditionGotoStart, span);
            }

            // do
            //   body
            // while (condition);
            //
            // becomes
            //
            // start: 
            // {
            //   body
            //   continue:
            //   sequence point
            //   GotoIfTrue condition start;
            // }
            // break:

            return BoundStatementList.Synthesized(syntax, node.HasErrors,
                new BoundLabelStatement(syntax, startLabel),
                rewrittenBody,
                new BoundLabelStatement(syntax, node.ContinueLabel),
                ifConditionGotoStart,
                new BoundLabelStatement(syntax, node.BreakLabel));
        }
    }
}
