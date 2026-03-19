// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (!node.WasCompilerGenerated && this.Instrument)
            {
                rewrittenCondition = Instrumenter.InstrumentWhileStatementCondition(node, rewrittenCondition, _factory);
            }

            return RewriteWhileStatement(
                node,
                node.Locals,
                rewrittenCondition,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel,
                node.HasErrors);
        }

        private BoundStatement RewriteWhileStatement(
            BoundNode loop,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenBody,
            LabelSymbol breakLabel,
            LabelSymbol continueLabel,
            bool hasErrors)
        {
            Debug.Assert(loop.Kind is BoundKind.WhileStatement or BoundKind.ForEachStatement or BoundKind.CollectionExpressionSpreadElement);

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

            SyntaxNode syntax = loop.Syntax;
            var startLabel = new GeneratedLabelSymbol("start");
            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);
            BoundStatement gotoContinue = new BoundGotoStatement(syntax, continueLabel);

            if (this.Instrument && !loop.WasCompilerGenerated)
            {
                switch (loop.Kind)
                {
                    case BoundKind.WhileStatement:
                        ifConditionGotoStart = Instrumenter.InstrumentWhileStatementConditionalGotoStartOrBreak((BoundWhileStatement)loop, ifConditionGotoStart);
                        break;

                    case BoundKind.ForEachStatement:
                        ifConditionGotoStart = Instrumenter.InstrumentForEachStatementConditionalGotoStart((BoundForEachStatement)loop, ifConditionGotoStart);
                        break;

                    case BoundKind.CollectionExpressionSpreadElement:
                        // No instrumentation needed since the loop for the spread expression
                        // was generated in lowering, and not explicit in the source.
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(loop.Kind);
                }

                // mark the initial jump as hidden. We do it to tell that this is not a part of previous statement. This
                // jump may be a target of another jump (for example if loops are nested) and that would give the
                // impression that the previous statement is being re-executed.
                gotoContinue = BoundSequencePoint.CreateHidden(gotoContinue);
            }

            return BoundStatementList.Synthesized(syntax, hasErrors,
                gotoContinue,
                new BoundLabelStatement(syntax, startLabel),
                rewrittenBody,
                new BoundLabelStatement(syntax, continueLabel),
                ifConditionGotoStart,
                new BoundLabelStatement(syntax, breakLabel));
        }

        private BoundStatement RewriteWhileStatement(
            BoundWhileStatement loop,
            ImmutableArray<LocalSymbol> locals,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenBody,
            LabelSymbol breakLabel,
            LabelSymbol continueLabel,
            bool hasErrors)
        {
            if (locals.IsEmpty)
            {
                return RewriteWhileStatement(loop, rewrittenCondition, rewrittenBody, breakLabel, continueLabel, hasErrors);
            }

            // We need to enter scope-block from the top, that is where an instance of a display class will be created
            // if any local is captured within a lambda.

            // while (condition) 
            //   body;
            //
            // becomes
            //
            // continue:
            // {
            //     GotoIfFalse condition break;
            //     body
            //     goto continue;
            // }
            // break:

            SyntaxNode syntax = loop.Syntax;
            BoundStatement continueLabelStatement = new BoundLabelStatement(syntax, continueLabel);
            BoundStatement ifNotConditionGotoBreak = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, breakLabel);

            if (this.Instrument && !loop.WasCompilerGenerated)
            {
                ifNotConditionGotoBreak = Instrumenter.InstrumentWhileStatementConditionalGotoStartOrBreak(loop, ifNotConditionGotoBreak);
                continueLabelStatement = BoundSequencePoint.CreateHidden(continueLabelStatement);
            }

            return BoundStatementList.Synthesized(syntax, hasErrors,
                continueLabelStatement,
                new BoundBlock(syntax,
                               locals,
                               ImmutableArray.Create(
                                    ifNotConditionGotoBreak,
                                    rewrittenBody,
                                    new BoundGotoStatement(syntax, continueLabel))),
                new BoundLabelStatement(syntax, breakLabel));
        }
    }
}
