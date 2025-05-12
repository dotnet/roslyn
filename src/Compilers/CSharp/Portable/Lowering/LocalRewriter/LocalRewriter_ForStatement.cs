// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenInitializer = VisitStatement(node.Initializer);
            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenIncrement = VisitStatement(node.Increment);
            var rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (rewrittenCondition != null && this.Instrument)
            {
                rewrittenCondition = Instrumenter.InstrumentForStatementCondition(node, rewrittenCondition, _factory);
            }

            return RewriteForStatement(
                node,
                rewrittenInitializer,
                rewrittenCondition,
                rewrittenIncrement,
                rewrittenBody);
        }

        private BoundStatement RewriteForStatementWithoutInnerLocals(
            BoundNode original,
            ImmutableArray<LocalSymbol> outerLocals,
            BoundStatement? rewrittenInitializer,
            BoundExpression? rewrittenCondition,
            BoundStatement? rewrittenIncrement,
            BoundStatement rewrittenBody,
            LabelSymbol breakLabel,
            LabelSymbol continueLabel,
            bool hasErrors)
        {
            Debug.Assert(original.Kind is BoundKind.ForStatement or BoundKind.ForEachStatement or BoundKind.CollectionExpressionSpreadElement);
            Debug.Assert(rewrittenBody != null);

            // The sequence point behavior exhibited here is different from that of the native compiler.  In the native
            // compiler, if you have something like 
            //
            // for([|int i = 0, j = 0|]; ; [|i++, j++|])
            //
            // then all the initializers are treated as a single sequence point, as are
            // all the loop incrementors.
            //
            // We now make each one individually a sequence point:
            //
            // for([|int i = 0|], [|j = 0|]; ; [|i++|], [|j++|])
            //
            // If we decide that we want to preserve the native compiler stepping behavior
            // then we'll need to be a bit fancy here. The initializer and increment statements
            // can contain lambdas whose bodies need to have sequence points inserted, so we
            // need to make sure we visit the children. But we'll also need to make sure that
            // we do not generate one sequence point for each statement in the initializers
            // and the incrementors.

            SyntaxNode syntax = original.Syntax;
            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            if (rewrittenInitializer != null)
            {
                statementBuilder.Add(rewrittenInitializer);
            }

            var startLabel = new GeneratedLabelSymbol("start");

            // for (initializer; condition; increment)
            //   body;
            //
            // becomes the following (with block added for locals)
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

            var endLabel = new GeneratedLabelSymbol("end");

            //  initializer;
            //  goto end;

            BoundStatement gotoEnd = new BoundGotoStatement(syntax, endLabel);

            if (this.Instrument)
            {
                // Mark the initial jump as hidden.
                // We do it to tell that this is not a part of previous statement.
                // This jump may be a target of another jump (for example if loops are nested) and that will make 
                // impression of the previous statement being re-executed
                gotoEnd = BoundSequencePoint.CreateHidden(gotoEnd);
            }

            statementBuilder.Add(gotoEnd);

            // start:
            //   body;
            statementBuilder.Add(new BoundLabelStatement(syntax, startLabel));

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
            BoundStatement? branchBack = null;
            if (rewrittenCondition != null)
            {
                branchBack = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, true, startLabel);
            }
            else
            {
                branchBack = new BoundGotoStatement(syntax, startLabel);
            }

            if (this.Instrument)
            {
                switch (original.Kind)
                {
                    case BoundKind.ForEachStatement:
                        branchBack = Instrumenter.InstrumentForEachStatementConditionalGotoStart((BoundForEachStatement)original, branchBack);
                        break;
                    case BoundKind.ForStatement:
                        branchBack = Instrumenter.InstrumentForStatementConditionalGotoStartOrBreak((BoundForStatement)original, branchBack);
                        break;
                    case BoundKind.CollectionExpressionSpreadElement:
                        // No instrumentation needed since the loop for the spread expression
                        // was generated in lowering, and not explicit in the source.
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(original.Kind);
                }
            }

            statementBuilder.Add(branchBack);

            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

            var statements = statementBuilder.ToImmutableAndFree();
            return new BoundBlock(syntax, outerLocals, statements, hasErrors);
        }

        private BoundStatement RewriteForStatement(
            BoundForStatement node,
            BoundStatement? rewrittenInitializer,
            BoundExpression? rewrittenCondition,
            BoundStatement? rewrittenIncrement,
            BoundStatement rewrittenBody)
        {
            if (node.InnerLocals.IsEmpty)
            {
                return RewriteForStatementWithoutInnerLocals(
                    node,
                    node.OuterLocals,
                    rewrittenInitializer,
                    rewrittenCondition,
                    rewrittenIncrement,
                    rewrittenBody,
                    node.BreakLabel,
                    node.ContinueLabel, node.HasErrors);
            }

            // We need to enter inner_scope-block from the top, that is where an instance of a display class will be created
            // if any local is captured within a lambda.

            // for (initializer; condition; increment)
            //   body;
            //
            // becomes the following (with block added for locals)
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

            Debug.Assert(rewrittenBody != null);

            SyntaxNode syntax = node.Syntax;
            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            //  initializer;
            if (rewrittenInitializer != null)
            {
                statementBuilder.Add(rewrittenInitializer);
            }

            var startLabel = new GeneratedLabelSymbol("start");

            // start:
            BoundStatement startLabelStatement = new BoundLabelStatement(syntax, startLabel);

            if (Instrument)
            {
                startLabelStatement = BoundSequencePoint.CreateHidden(startLabelStatement);
            }

            statementBuilder.Add(startLabelStatement);

            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            // GotoIfFalse condition break;
            if (rewrittenCondition != null)
            {
                BoundStatement ifNotConditionGotoBreak = new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, node.BreakLabel);

                if (this.Instrument)
                {
                    ifNotConditionGotoBreak = Instrumenter.InstrumentForStatementConditionalGotoStartOrBreak(node, ifNotConditionGotoBreak);
                }

                blockBuilder.Add(ifNotConditionGotoBreak);
            }

            // body;
            blockBuilder.Add(rewrittenBody);

            // continue:
            //   increment;
            blockBuilder.Add(new BoundLabelStatement(syntax, node.ContinueLabel));
            if (rewrittenIncrement != null)
            {
                blockBuilder.Add(rewrittenIncrement);
            }

            // goto start;
            blockBuilder.Add(new BoundGotoStatement(syntax, startLabel));

            statementBuilder.Add(new BoundBlock(syntax, node.InnerLocals, blockBuilder.ToImmutableAndFree()));

            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, node.BreakLabel));

            var statements = statementBuilder.ToImmutableAndFree();
            return new BoundBlock(syntax, node.OuterLocals, statements, node.HasErrors);
        }
    }
}
