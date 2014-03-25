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
                node.Locals,
                rewrittenInitializer,
                rewrittenCondition,
                conditionSyntax,
                rewrittenIncrement,
                rewrittenBody,
                node.BreakLabel,
                node.ContinueLabel, node.HasErrors);
        }

        private BoundStatement RewriteForStatement(
            CSharpSyntaxNode syntax,
            ImmutableArray<LocalSymbol> locals,
            BoundStatement rewrittenInitializer,
            BoundExpression rewrittenCondition,
            SyntaxNodeOrToken conditionSyntax,
            BoundStatement rewrittenIncrement,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
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

            var startLabel = new GeneratedLabelSymbol("start");
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
            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            if (rewrittenInitializer != null)
            {
                statementBuilder.Add(rewrittenInitializer);
            }

            //mark the initial jump as hidden.
            //We do it to tell that this is not a part of previous statement.
            //This jump may be a target of another jump (for example if loops are nested) and that will make 
            //impression of the previous statement being re-executed
            var gotoEnd = new BoundSequencePoint(null, new BoundGotoStatement(syntax, endLabel));
            statementBuilder.Add(gotoEnd);

            // start:
            //   body;
            statementBuilder.Add(new BoundLabelStatement(syntax, startLabel));
            Debug.Assert(rewrittenBody != null);
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


            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

            var statements = statementBuilder.ToImmutableAndFree();
            return new BoundBlock(syntax, locals, statements, hasErrors);
        }
    }
}
