// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenInitializer = (BoundStatement)Visit(node.Initializer);
            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenIncrement = (BoundStatement)Visit(node.Increment);
            var rewrittenBody = (BoundStatement)Visit(node.Body);

            SyntaxNodeOrToken conditionSyntax = ((BoundNode)rewrittenCondition ?? node).Syntax;

            return RewriteForStatement(
                node.Syntax,
                node.OuterLocals,
                rewrittenInitializer,
                node.InnerLocals,
                rewrittenCondition,
                conditionSyntax,
                rewrittenIncrement,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel, node.HasErrors);
        }

        private BoundStatement RewriteForStatement(
            CSharpSyntaxNode syntax,
            ImmutableArray<LocalSymbol> outerLocals,
            BoundStatement rewrittenInitializer,
            ImmutableArray<LocalSymbol> innerLocals,
            BoundExpression rewrittenCondition,
            SyntaxNodeOrToken conditionSyntax,
            BoundStatement rewrittenIncrement,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
            Debug.Assert(rewrittenBody != null);

            // The sequence point behavior exhibited here is different from that of the native compiler.  In the native
            // compiler, if you have something like 
            //
            // for(int i = 0, j = 0; ; i++, j++)
            //     ^--------------^    ^------^   
            //
            // then all the initializers are treated as a single sequence point, as are
            // all the loop incrementers.
            //
            // We now make each one individually a sequence point:
            //
            // for(int i = 0, j = 0; ; i++, j++)
            //     ^-------^  ^---^    ^-^  ^-^
            //
            // If we decide that we want to preserve the native compiler stepping behavior
            // then we'll need to be a bit fancy here. The initializer and increment statements
            // can contain lambdas whose bodies need to have sequence points inserted, so we
            // need to make sure we visit the children. But we'll also need to make sure that
            // we do not generate one sequence point for each statement in the initializers
            // and the incrementers.

            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            if (rewrittenInitializer != null)
            {
                statementBuilder.Add(rewrittenInitializer);
            }

            var startLabel = new GeneratedLabelSymbol("start");

            if (!innerLocals.IsDefaultOrEmpty)
            {
                var walker = new AnyLocalCapturedInALambdaWalker(innerLocals);

                if (walker.Analyze(rewrittenCondition) || walker.Analyze(rewrittenIncrement) || walker.Analyze(rewrittenBody))
                {
                    // If any inner local is captured within a lambda, we need to enter scope-block
                    // always from the top, that is where an instance of a display class will be created.
                    // The IL will be less optimal, but this shouldn't be a problem, given presence of lambdas.

                    // for (initializer; condition; increment)
                    //   body;
                    //
                    // becomes the following (with
                    // block added for locals)
                    //
                    // {
                    //   initializer;
                    // start:
                    //   {
                    //     GotoIfFalse condition break;
                    //     body;
                    // continue:
                    //     increment;
                    //     goto start;
                    //   }
                    // break:
                    // }

                    // start:
                    statementBuilder.Add(new BoundLabelStatement(syntax, startLabel));

                    var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                    //   GotoIfFalse condition break;
                    if (rewrittenCondition != null)
                    {
                        BoundStatement ifNotConditionGotoBreak = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, breakLabel);

                        if (this.generateDebugInfo)
                        {
                            if (conditionSyntax.IsToken)
                            {
                                ifNotConditionGotoBreak = new BoundSequencePointWithSpan(syntax, ifNotConditionGotoBreak, conditionSyntax.Span);
                            }
                            else
                            {
                                ifNotConditionGotoBreak = new BoundSequencePoint((CSharpSyntaxNode)conditionSyntax.AsNode(), ifNotConditionGotoBreak);
                            }
                        }

                        blockBuilder.Add(ifNotConditionGotoBreak);
                    }

                    // body;
                    blockBuilder.Add(rewrittenBody);

                    // continue:
                    //   increment;
                    blockBuilder.Add(new BoundLabelStatement(syntax, continueLabel));
                    if (rewrittenIncrement != null)
                    {
                        blockBuilder.Add(rewrittenIncrement);
                    }

                    //     goto start;
                    blockBuilder.Add(new BoundGotoStatement(syntax, startLabel));

                    statementBuilder.Add(new BoundBlock(syntax, innerLocals, blockBuilder.ToImmutableAndFree()));

                    // break:
                    statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

                    return new BoundBlock(syntax, outerLocals, statementBuilder.ToImmutableAndFree(), hasErrors);
                }
            }

            var endLabel = new GeneratedLabelSymbol("end");

            // for (initializer; condition; increment)
            //   body;
            //
            // becomes the following (with
            // block added for locals)
            //
            // {
            //   initializer;
            //   goto end;
            // start:
            //   body;
            // continue:
            //   increment;
            // end:
            //   GotoIfTrue condition start;
            // break:
            // }

            //  initializer;
            //  goto end;

            //mark the initial jump as hidden.
            //We do it to tell that this is not a part of previous statement.
            //This jump may be a target of another jump (for example if loops are nested) and that will make 
            //impression of the previous statement being re-executed
            var gotoEnd = new BoundSequencePoint(null, new BoundGotoStatement(syntax, endLabel));
            statementBuilder.Add(gotoEnd);

            // start:
            //   body;
            statementBuilder.Add(new BoundLabelStatement(syntax, startLabel));

            ArrayBuilder<BoundStatement> saveBuilder = null;

            if (!innerLocals.IsDefaultOrEmpty)
            {
                saveBuilder = statementBuilder;
                statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            }

            statementBuilder.Add(rewrittenBody);

            // continue:
            //   increment;
            statementBuilder.Add(new BoundLabelStatement(syntax, continueLabel));
            if (rewrittenIncrement != null)
            {
                statementBuilder.Add(rewrittenIncrement);
            }

            // end:
            //   GotoIfTrue condition start;
            statementBuilder.Add(new BoundLabelStatement(syntax, endLabel));
            BoundStatement branchBack = null;
            if (rewrittenCondition != null)
            {
                branchBack = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);
            }
            else
            {
                branchBack = new BoundGotoStatement(syntax, startLabel);
            }

            if (this.generateDebugInfo)
            {
                if (conditionSyntax.IsToken)
                {
                    branchBack = new BoundSequencePointWithSpan(syntax, branchBack, conditionSyntax.Span);
                }
                else
                {
                    //if there is no condition, make this a hidden point so that 
                    //it does not count as a part of previous statement
                    branchBack = new BoundSequencePoint((CSharpSyntaxNode)conditionSyntax.AsNode(), branchBack);
                }
            }

            statementBuilder.Add(branchBack);

            if (!innerLocals.IsDefaultOrEmpty)
            {
                var block = new BoundBlock(syntax, innerLocals, statementBuilder.ToImmutableAndFree());
                statementBuilder = saveBuilder;
                statementBuilder.Add(block);
            }

            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

            var statements = statementBuilder.ToImmutableAndFree();
            return new BoundBlock(syntax, outerLocals, statements, hasErrors);
        }
    }
}
