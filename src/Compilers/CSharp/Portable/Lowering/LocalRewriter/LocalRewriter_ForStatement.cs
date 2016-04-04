// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (rewrittenCondition != null && this.Instrument)
            {
                rewrittenCondition = _instrumenter.InstrumentForStatementCondition(node, rewrittenCondition, _factory);
            }

            return RewriteForStatement(
                node,
                node.OuterLocals,
                rewrittenInitializer,
                rewrittenCondition,
                rewrittenIncrement,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel, node.HasErrors);
        }

        private BoundStatement RewriteForStatement(
            BoundLoopStatement original,
            ImmutableArray<LocalSymbol> outerLocals,
            BoundStatement rewrittenInitializer,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenIncrement,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
            Debug.Assert(original.Kind == BoundKind.ForStatement || original.Kind == BoundKind.ForEachStatement);
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

            CSharpSyntaxNode syntax = original.Syntax;
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
                switch (original.Kind)
                {
                    case BoundKind.ForEachStatement:
                        gotoEnd = _instrumenter.InstrumentForEachStatementGotoEnd((BoundForEachStatement)original, gotoEnd);
                        break;
                    case BoundKind.ForStatement:
                        gotoEnd = _instrumenter.InstrumentForStatementGotoEnd((BoundForStatement)original, gotoEnd);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(original.Kind);
                }
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
            BoundStatement branchBack = null;
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
                        branchBack = _instrumenter.InstrumentForEachStatementConditionalGotoStart((BoundForEachStatement)original, branchBack);
                        break;
                    case BoundKind.ForStatement:
                        branchBack = _instrumenter.InstrumentForStatementConditionalGotoStart((BoundForStatement)original, branchBack);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(original.Kind);
                }
            }

            statementBuilder.Add(branchBack);

            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

            var statements = statementBuilder.ToImmutableAndFree();
            return new BoundBlock(syntax, outerLocals, ImmutableArray<LocalFunctionSymbol>.Empty, statements, hasErrors);
        }
    }
}
