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
        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenBody = (BoundStatement)Visit(node.Body);

            TextSpan conditionSequencePointSpan = default(TextSpan);
            if (this.GenerateDebugInfo)
            {
                if (!node.WasCompilerGenerated)
                {
                    WhileStatementSyntax whileSyntax = (WhileStatementSyntax)node.Syntax;
                    conditionSequencePointSpan = TextSpan.FromBounds(
                        whileSyntax.WhileKeyword.SpanStart,
                        whileSyntax.CloseParenToken.Span.End);
                }
            }

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return RewriteWhileStatement(
                node.Syntax,
                AddConditionSequencePoint(rewrittenCondition, node),
                conditionSequencePointSpan,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel,
                node.HasErrors);
        }

        private BoundStatement RewriteWhileStatement(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenCondition,
            TextSpan conditionSequencePointSpan,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
            var startLabel = new GeneratedLabelSymbol("start");
            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);

            if (this.GenerateDebugInfo)
            {
                ifConditionGotoStart = new BoundSequencePointWithSpan(syntax, ifConditionGotoStart, conditionSequencePointSpan);
            }

            // while (condition) 
            //   body;
            //
            // becomes
            //
            // goto continue;
            // start: 
            // {
            //     body
            //     continue:
            //     GotoIfTrue condition start;
            // }
            // break:

            BoundStatement gotoContinue = new BoundGotoStatement(syntax, continueLabel);
            if (this.GenerateDebugInfo)
            {
                // mark the initial jump as hidden. We do it to tell that this is not a part of previous statement. This
                // jump may be a target of another jump (for example if loops are nested) and that would give the
                // impression that the previous statement is being re-executed.
                gotoContinue = new BoundSequencePoint(null, gotoContinue);
            }

            return BoundStatementList.Synthesized(syntax, hasErrors,
                gotoContinue,
                new BoundLabelStatement(syntax, startLabel),
                rewrittenBody,
                new BoundLabelStatement(syntax, continueLabel),
                ifConditionGotoStart,
                new BoundLabelStatement(syntax, breakLabel));
        }
    }
}
