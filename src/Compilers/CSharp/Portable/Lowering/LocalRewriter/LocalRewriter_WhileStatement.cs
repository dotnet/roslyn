// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenBody = (BoundStatement)Visit(node.Body);

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (!node.WasCompilerGenerated && this.Instrument)
            {
                rewrittenCondition = _instrumenter.InstrumentWhileStatementCondition(node, rewrittenCondition, _factory);
            }

            return RewriteWhileStatement(
                node,
                rewrittenCondition,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel,
                node.HasErrors);
        }

        private BoundStatement RewriteWhileStatement(
            BoundLoopStatement loop,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
            Debug.Assert(loop.Kind == BoundKind.WhileStatement || loop.Kind == BoundKind.ForEachStatement);
            CSharpSyntaxNode syntax = loop.Syntax;
            var startLabel = new GeneratedLabelSymbol("start");
            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);

            if (this.Instrument && !loop.WasCompilerGenerated)
            {
                switch (loop.Kind)
                {
                    case BoundKind.WhileStatement:
                        ifConditionGotoStart = _instrumenter.InstrumentWhileStatementConditionalGotoStart((BoundWhileStatement)loop, ifConditionGotoStart);
                        break;

                    case BoundKind.ForEachStatement:
                        ifConditionGotoStart = _instrumenter.InstrumentForEachStatementConditionalGotoStart((BoundForEachStatement)loop, ifConditionGotoStart);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(loop.Kind);
                }
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
            if (this.Instrument && !loop.WasCompilerGenerated)
            {
                // mark the initial jump as hidden. We do it to tell that this is not a part of previous statement. This
                // jump may be a target of another jump (for example if loops are nested) and that would give the
                // impression that the previous statement is being re-executed.
                switch (loop.Kind)
                {
                    case BoundKind.WhileStatement:
                        gotoContinue = _instrumenter.InstrumentWhileStatementGotoContinue((BoundWhileStatement)loop, gotoContinue);
                        break;

                    case BoundKind.ForEachStatement:
                        gotoContinue = _instrumenter.InstrumentForEachStatementGotoContinue((BoundForEachStatement)loop, gotoContinue);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(loop.Kind);
                }
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
