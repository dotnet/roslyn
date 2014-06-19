using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class ControlFlowRewriter : BoundTreeRewriter
    {
        private readonly MethodSymbol containingMethod;
        private readonly Compilation compilation;
        private readonly bool generateDebugInfo;
        private bool sawLambdas;
        private ControlFlowRewriter(MethodSymbol containingMethod, Compilation compilation, bool generateDebugInfo)
        {
            this.containingMethod = containingMethod;
            this.compilation = compilation;
            this.generateDebugInfo = generateDebugInfo && !containingMethod.SuppressDebugInfo;
        }

        public static BoundStatement Rewrite(BoundStatement node, MethodSymbol containingMethod, Compilation compilation, bool generateDebugInfo, out bool sawLambdas)
        {
            Debug.Assert(node != null);
            var rewriter = new ControlFlowRewriter(containingMethod, compilation, generateDebugInfo);
            var result = (BoundStatement)rewriter.Visit(node);
            sawLambdas = rewriter.sawLambdas;
            return result;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            sawLambdas = true;
            return base.VisitLambda(node);
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenBody = (BoundStatement)Visit(node.Body);

            var labelStatement = new BoundLabelStatement(node.Syntax, node.Label);
            if (rewrittenBody == null)
            {
                // Body may be null if the body has no associated IL
                // (declaration with no initializer for instance.)
                return labelStatement;
            }

            return BoundStatementList.Synthesized(node.Syntax, labelStatement, rewrittenBody);
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenConsequence = (BoundStatement)Visit(node.Consequence);
            var rewrittenAlternative = (BoundStatement)Visit(node.AlternativeOpt);
            return RewriteIfStatement(node.Syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.HasErrors);
        }

        private static BoundStatement RewriteIfStatement(
            SyntaxNode syntax,  
            BoundExpression rewrittenCondition, 
            BoundStatement rewrittenConsequence, 
            BoundStatement rewrittenAlternativeOpt, 
            bool hasErrors)
        {
            var afterif = new GeneratedLabelSymbol("afterif");

            // if (condition) 
            //   consequence;  
            //
            // becomes
            //
            // GotoIfFalse condition afterif;
            // consequence;
            // afterif:

            if (rewrittenAlternativeOpt == null)
            {
                return BoundStatementList.Synthesized(syntax, 
                    new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, afterif),
                    rewrittenConsequence,
                    new BoundLabelStatement(syntax, afterif));
            }

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
            return BoundStatementList.Synthesized(syntax, hasErrors,
                new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, alt),
                rewrittenConsequence,
                new BoundGotoStatement(syntax, afterif),
                new BoundLabelStatement(syntax, alt),
                rewrittenAlternativeOpt,
                new BoundLabelStatement(syntax, afterif));
        }

        public override BoundNode VisitBreakStatement(BoundBreakStatement node)
        {
            Debug.Assert(node != null);
            return new BoundGotoStatement(node.Syntax, node.Label, node.HasErrors);
        }

        public override BoundNode VisitContinueStatement(BoundContinueStatement node)
        {
            Debug.Assert(node != null);
            return new BoundGotoStatement(node.Syntax, node.Label, node.HasErrors);
        }

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
                        whileSyntax.WhileKeyword.Span.Start,
                        whileSyntax.CloseParenToken.Span.End);
                }
            }

            return RewriteWhileStatement(node.Syntax, rewrittenCondition, conditionSequencePointSpan, rewrittenBody, node.BreakLabel, node.ContinueLabel, node.HasErrors);
        }

        private BoundStatement RewriteWhileStatement(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            TextSpan conditionSequencePointSpan,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
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
            // body
            // continue:
            // GotoIfTrue condition start;
            // break:

            BoundStatement gotoContinue = new BoundGotoStatement(syntax, continueLabel);
            if (this.generateDebugInfo)
            {
                //mark the initial jump as hidden.
                //We do it to tell that this is not a part of previou statement.
                //This jump may be a target of another jump (for example if loops are nested) and that will make 
                //impression of the previous statement being re-executed
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

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            Debug.Assert(node != null);

            var rewrittenCondition = (BoundExpression)Visit(node.Condition);
            var rewrittenBody = (BoundStatement)Visit(node.Body);
            var startLabel = new GeneratedLabelSymbol("start");
            
            var syntax = node.Syntax;

            BoundStatement ifConditionGotoStart = new BoundConditionalGoto(syntax, rewrittenCondition, true, startLabel);

            if (this.generateDebugInfo)
            {
                var doSyntax = (DoStatementSyntax)syntax;
                var span = TextSpan.FromBounds(
                    doSyntax.WhileKeyword.Span.Start,
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
            // body
            // continue:
            // sequence point
            // GotoIfTrue condition start;
            // break:

            return BoundStatementList.Synthesized(syntax, node.HasErrors,
                new BoundLabelStatement(syntax, startLabel),
                rewrittenBody,
                new BoundLabelStatement(syntax, node.ContinueLabel),
                ifConditionGotoStart,
                new BoundLabelStatement(syntax, node.BreakLabel));
        }

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
            SyntaxNode syntax,
            ReadOnlyArray<LocalSymbol> locals,
            BoundStatement rewrittenInitializer,
            BoundExpression rewrittenCondition,
            SyntaxNodeOrToken conditionSyntax,
            BoundStatement rewrittenIncrement,
            BoundStatement rewrittenBody,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            bool hasErrors)
        {
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
                    branchBack = new BoundSequencePoint(conditionSyntax.AsNode(), branchBack);
                }
            }

            statementBuilder.Add(branchBack);


            // break:
            statementBuilder.Add(new BoundLabelStatement(syntax, breakLabel));

            var statements = statementBuilder.ToReadOnlyAndFree();
            return new BoundBlock(syntax, locals, statements, hasErrors);
        }

        // block introduces a new scope
        public override BoundNode VisitBlock(BoundBlock node)
        {
            var rewrittenStatements = VisitList(node.Statements);

            return new BoundBlock(node.Syntax, node.LocalsOpt, rewrittenStatements, node.HasErrors);
        }

        // records locals into given scope
        // converts initializers into assignments
        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            return RewriteLocalDeclaration(node.Syntax, node.LocalSymbol, (BoundExpression)Visit(node.InitializerOpt), node.HasErrors);
        }

        private static BoundStatement RewriteLocalDeclaration(SyntaxNode syntax, LocalSymbol localSymbol, BoundExpression rewrittenInitializer, bool hasErrors)
        {
            // A declaration of a local variable without an initializer has no associated IL.
            // Simply remove the declaration from the bound tree. The local symbol will
            // remain in the bound block, so codegen will make a stack frame location for it.
            if (rewrittenInitializer == null)
            {
                return null;
            }

            // A declaration of a local constant also does nothing, even though there is
            // an assignment. The value will be emitted directly where it is used. The 
            // local symbol remains in the bound block, but codegen will skip making a 
            // stack frame location for it. (We still need a symbol for it to hang
            // around because we'll be generating debug info for it.)
            if (localSymbol.IsConst)
            {
                return null;
            }

            return new BoundExpressionStatement(
                syntax,
                new BoundAssignmentOperator(
                    syntax,
                    new BoundLocal(
                        syntax,
                        localSymbol,
                        null,
                        localSymbol.Type
                    ),
                    rewrittenInitializer,
                    localSymbol.Type),
                hasErrors);
        }

        // collect locals into provided scope
        // leave initializers in the tree if there are any
        public override BoundNode VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node)
        {
            ArrayBuilder<BoundStatement> inits = null;

            foreach (var decl in node.LocalDeclarations)
            {
                var init = VisitLocalDeclaration(decl);

                if (init != null)
                {
                    if (inits == null)
                    {
                        inits = ArrayBuilder<BoundStatement>.GetInstance();
                    }

                    inits.Add((BoundStatement)init);
                }
            }

            if (inits != null)
            {
                return BoundStatementList.Synthesized(node.Syntax, node.HasErrors, inits.ToReadOnlyAndFree());
            }
            else
            {
                // no initializers
                return null; // TODO: but what if hasErrors?  Have we lost that?
            }
        }
    }
}
