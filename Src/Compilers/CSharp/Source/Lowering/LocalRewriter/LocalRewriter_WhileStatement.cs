// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            if (this.generateDebugInfo)
            {
                if (!node.WasCompilerGenerated)
                {
                    WhileStatementSyntax whileSyntax = (WhileStatementSyntax)node.Syntax;
                    conditionSequencePointSpan = TextSpan.FromBounds(
                        whileSyntax.WhileKeyword.SpanStart,
                        whileSyntax.CloseParenToken.Span.End);
                }
            }

            return RewriteWhileStatement(node.Syntax, node.InnerLocals, rewrittenCondition, conditionSequencePointSpan, rewrittenBody, node.BreakLabel, node.ContinueLabel, node.HasErrors);
        }

        private BoundStatement RewriteWhileStatement(
            CSharpSyntaxNode syntax,
            ImmutableArray<LocalSymbol> innerLocals,
            BoundExpression rewrittenCondition,
            TextSpan conditionSequencePointSpan,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
            if (!innerLocals.IsDefaultOrEmpty)
            {
                var walker = new AnyLocalCapturedInALambdaWalker(innerLocals);

                if (walker.Analyze(rewrittenCondition) || walker.Analyze(rewrittenBody))
                {
                    // If any inner local is captured within a lambda, we need to enter scope-block
                    // always from the top, that is where an instance of a display class will be created.
                    // The IL will be less optimal, but this shouldn't be a problem, given presence of lambdas.

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

                    // TODO: We could perform more fine analysis. 
                    // If locals declared in condition (the innerLocals) are captured, but not referenced in the body, we could use optimal IL by creating
                    // another block around the condition and use it as a scope for the locals declared in condition.
                    // This optimization can be applied to 'for' as well, while-body === for-body + increment.
                    // Note however that the scope adjusments will likely be observable during debugging, in locals window.

                    BoundStatement ifNotConditionGotoBreak = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, breakLabel);

                    if (this.generateDebugInfo)
                    {
                        ifNotConditionGotoBreak = new BoundSequencePointWithSpan(syntax, ifNotConditionGotoBreak, conditionSequencePointSpan);
                    }

                    return BoundStatementList.Synthesized(syntax, hasErrors,
                        new BoundLabelStatement(syntax, continueLabel),
                        new BoundBlock(syntax,
                                       innerLocals,
                                       ImmutableArray.Create<BoundStatement>(
                                            ifNotConditionGotoBreak,
                                            rewrittenBody,
                                            new BoundGotoStatement(syntax, continueLabel))),
                        new BoundLabelStatement(syntax, breakLabel));
                }
            }

            var startLabel = new GeneratedLabelSymbol("start");
            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);

            if (this.generateDebugInfo)
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
            if (this.generateDebugInfo)
            {
                // mark the initial jump as hidden. We do it to tell that this is not a part of previous statement. This
                // jump may be a target of another jump (for example if loops are nested) and that would give the
                // impression that the previous statement is being re-executed.
                gotoContinue = new BoundSequencePoint(null, gotoContinue);
            }

            if (!innerLocals.IsDefaultOrEmpty)
            {
                return BoundStatementList.Synthesized(syntax, hasErrors,
                    gotoContinue,
                    new BoundLabelStatement(syntax, startLabel),
                    new BoundBlock(syntax,
                                   innerLocals,
                                   ImmutableArray.Create<BoundStatement>(
                                        rewrittenBody,
                                        new BoundLabelStatement(syntax, continueLabel),
                                        ifConditionGotoStart)),
                    new BoundLabelStatement(syntax, breakLabel));
            }

            return BoundStatementList.Synthesized(syntax, hasErrors,
                gotoContinue,
                new BoundLabelStatement(syntax, startLabel),
                rewrittenBody,
                new BoundLabelStatement(syntax, continueLabel),
                ifConditionGotoStart,
                new BoundLabelStatement(syntax, breakLabel));
        }

        private class AnyLocalCapturedInALambdaWalker : BoundTreeWalker
        {
            private readonly SmallDictionary<LocalSymbol, bool> locals;
            private bool captured;
            private uint lambdaLevel;

            public AnyLocalCapturedInALambdaWalker(ImmutableArray<LocalSymbol> locals)
            {
                this.locals = new SmallDictionary<LocalSymbol, bool>();

                foreach (var local in locals)
                {
                    this.locals[local] = true;
                }
            }

            public bool Analyze(BoundNode node)
            {
                captured = false;
                lambdaLevel = 0;
                Visit(node);

                Debug.Assert(captured || lambdaLevel == 0);
                return captured;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (captured)
                {
                    return null;
                }

                return base.Visit(node);
            }

            public override BoundNode VisitLambda(BoundLambda node)
            {
                lambdaLevel++;
                base.VisitLambda(node);
                lambdaLevel--;
                return null;
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                if (lambdaLevel != 0 && locals.ContainsKey(node.LocalSymbol))
                {
                    captured = true;
                }

                return null;
            }
        } 
    }
}
