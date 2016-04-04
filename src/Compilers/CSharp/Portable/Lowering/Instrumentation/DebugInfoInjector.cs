// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class DebugInfoInjector : CompoundInstrumenter
    {
        public static readonly DebugInfoInjector Singleton = new DebugInfoInjector(Instrumenter.NoOp);

        public DebugInfoInjector(Instrumenter previous)
            : base (previous)
        {
        }

        private BoundStatement AddSequencePoint(BoundStatement node)
        {
            return new BoundSequencePoint(node.Syntax, node);
        }

        public override BoundStatement InstrumentNoOpStatement(BoundNoOpStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentNoOpStatement(original, rewritten));
        }

        public override BoundStatement InstrumentBreakStatement(BoundBreakStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentBreakStatement(original, rewritten));
        }

        public override BoundStatement InstrumentContinueStatement(BoundContinueStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentContinueStatement(original, rewritten));
        }

        public override BoundStatement InstrumentExpressionStatement(BoundExpressionStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentExpressionStatement(original, rewritten));
        }

        public override BoundStatement InstrumentGotoStatement(BoundGotoStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentGotoStatement(original, rewritten));
        }

        public override BoundStatement InstrumentThrowStatement(BoundThrowStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentThrowStatement(original, rewritten));
        }

        public override BoundStatement InstrumentYieldBreakStatement(BoundYieldBreakStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentYieldBreakStatement(original, rewritten));
        }

        public override BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentYieldReturnStatement(original, rewritten));
        }

        public override BoundStatement CreateBlockPrologue(BoundBlock original)
        {
            var oBspan = ((BlockSyntax)original.Syntax).OpenBraceToken.Span;
            return new BoundSequencePointWithSpan(original.Syntax, base.CreateBlockPrologue(original), oBspan);
        }

        public override BoundStatement CreateBlockEpilogue(BoundBlock original)
        {
            var previous = base.CreateBlockEpilogue(original);

            // no need to mark "}" on the outermost block
            // as it cannot leave it normally. The block will have "return" at the end.
            CSharpSyntaxNode parent = original.Syntax.Parent;
            if (parent == null || !(parent.IsAnonymousFunction() || parent is BaseMethodDeclarationSyntax))
            {
                var cBspan = ((BlockSyntax)original.Syntax).CloseBraceToken.Span;
                return new BoundSequencePointWithSpan(original.Syntax, previous, cBspan);
            }

            return previous;
        }

        public override BoundExpression InstrumentDoStatementCondition(BoundDoStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return AddConditionSequencePoint(base.InstrumentDoStatementCondition(original, rewrittenCondition, factory), original.Syntax, factory);
        }

        public override BoundExpression InstrumentWhileStatementCondition(BoundWhileStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return AddConditionSequencePoint(base.InstrumentWhileStatementCondition(original, rewrittenCondition, factory), original.Syntax, factory);
        }

        public override BoundStatement InstrumentDoStatementConditionalGotoStart(BoundDoStatement original, BoundStatement ifConditionGotoStart)
        {
            var doSyntax = (DoStatementSyntax)original.Syntax;
            var span = TextSpan.FromBounds(
                doSyntax.WhileKeyword.SpanStart,
                doSyntax.SemicolonToken.Span.End);

            return new BoundSequencePointWithSpan(doSyntax, base.InstrumentDoStatementConditionalGotoStart(original, ifConditionGotoStart), span);
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStart(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            WhileStatementSyntax whileSyntax = (WhileStatementSyntax)original.Syntax;
            TextSpan conditionSequencePointSpan = TextSpan.FromBounds(
                whileSyntax.WhileKeyword.SpanStart,
                whileSyntax.CloseParenToken.Span.End);

            return new BoundSequencePointWithSpan(whileSyntax, base.InstrumentWhileStatementConditionalGotoStart(original, ifConditionGotoStart), conditionSequencePointSpan);
        }

        private static BoundExpression AddConditionSequencePoint(BoundExpression condition, BoundStatement containingStatement, SyntheticBoundNodeFactory factory)
        {
            return AddConditionSequencePoint(condition, containingStatement.Syntax, factory);
        }

        private static BoundExpression AddConditionSequencePoint(BoundExpression condition, SyntaxNode synthesizedVariableSyntax, SyntheticBoundNodeFactory factory)
        {
            if (!factory.Compilation.Options.EnableEditAndContinue)
            {
                return condition;
            }

            // The local has to be associated with a syntax that is tracked by EnC source mapping.
            // At most one ConditionalBranchDiscriminator variable shall be associated with any given EnC tracked syntax node.
            var local = factory.SynthesizedLocal(condition.Type, synthesizedVariableSyntax, kind: SynthesizedLocalKind.ConditionalBranchDiscriminator);

            // Add hidden sequence point unless the condition is a constant expression.
            // Constant expression must stay a const to not invalidate results of control flow analysis.
            var valueExpression = (condition.ConstantValue == null) ?
                new BoundSequencePointExpression(syntax: null, expression: factory.Local(local), type: condition.Type) :
                condition;

            return new BoundSequence(
                condition.Syntax,
                ImmutableArray.Create(local),
                ImmutableArray.Create<BoundExpression>(factory.AssignmentExpression(factory.Local(local), condition)),
                valueExpression,
                condition.Type);
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// foreach (Type var in |expr|) { }
        /// </summary>
        /// <remarks>
        /// Hit once, before looping begins.
        /// </remarks>
        public override BoundStatement InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, BoundStatement collectionVarDecl)
        {
            // NOTE: This is slightly different from Dev10.  In Dev10, when you stop the debugger
            // on the collection expression, you can see the (uninitialized) iteration variable.
            // In Roslyn, you cannot because the iteration variable is re-declared in each iteration
            // of the loop and is, therefore, not yet in scope.
            return new BoundSequencePoint(((ForEachStatementSyntax)original.Syntax).Expression,
                                          base.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl));
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// |foreach| (Type var in expr) { }
        /// </summary>
        /// <remarks>
        /// Hit once, before looping begins.
        /// </remarks>
        public override BoundStatement InstrumentForEachStatement(BoundForEachStatement original, BoundStatement rewritten)
        {
            var forEachSyntax = (ForEachStatementSyntax)original.Syntax;
            BoundSequencePointWithSpan foreachKeywordSequencePoint = new BoundSequencePointWithSpan(forEachSyntax, null, forEachSyntax.ForEachKeyword.Span);
            return new BoundStatementList(forEachSyntax, 
                                            ImmutableArray.Create<BoundStatement>(foreachKeywordSequencePoint,
                                                                                base.InstrumentForEachStatement(original, rewritten)));
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// foreach (|Type var| in expr) { }
        /// </summary>
        /// <remarks>
        /// Hit every iteration.
        /// </remarks>
        public override BoundStatement InstrumentForEachStatementIterationVarDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            var forEachSyntax = (ForEachStatementSyntax)original.Syntax;
            TextSpan iterationVarDeclSpan = TextSpan.FromBounds(forEachSyntax.Type.SpanStart, forEachSyntax.Identifier.Span.End);
            return new BoundSequencePointWithSpan(forEachSyntax, 
                                                  base.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl), 
                                                  iterationVarDeclSpan);
        }

        public override BoundStatement InstrumentForEachStatementGotoEnd(BoundForEachStatement original, BoundStatement gotoEnd)
        {
            return ForStatementCreateGotoEndSequencePoint(base.InstrumentForEachStatementGotoEnd(original, gotoEnd));
        }

        public override BoundStatement InstrumentForStatementGotoEnd(BoundForStatement original, BoundStatement gotoEnd)
        {
            return ForStatementCreateGotoEndSequencePoint(base.InstrumentForStatementGotoEnd(original, gotoEnd));
        }

        private static BoundStatement ForStatementCreateGotoEndSequencePoint(BoundStatement gotoEnd)
        {
            // Mark the initial jump as hidden.
            // We do it to tell that this is not a part of previous statement.
            // This jump may be a target of another jump (for example if loops are nested) and that will make 
            // impression of the previous statement being re-executed
            return new BoundSequencePoint(null, gotoEnd);
        }

        public override BoundStatement InstrumentForStatementConditionalGotoStart(BoundForStatement original, BoundStatement branchBack)
        {
            // hidden sequence point if there is no condition
            return new BoundSequencePoint(original.Condition?.Syntax, 
                                          base.InstrumentForStatementConditionalGotoStart(original, branchBack));
        }

        public override BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        {
            var syntax = (ForEachStatementSyntax)original.Syntax;
            return new BoundSequencePointWithSpan(syntax, 
                                                  base.InstrumentForEachStatementConditionalGotoStart(original, branchBack),
                                                  syntax.InKeyword.Span);
        }

        public override BoundExpression InstrumentForStatementCondition(BoundForStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return AddConditionSequencePoint(base.InstrumentForStatementCondition(original, rewrittenCondition, factory), original.Syntax, factory);
        }

        public override BoundStatement InstrumentIfStatement(BoundIfStatement original, BoundStatement rewritten)
        {
            var syntax = (IfStatementSyntax)original.Syntax;
            return new BoundSequencePointWithSpan(
                syntax,
                base.InstrumentIfStatement(original, rewritten),
                TextSpan.FromBounds(
                    syntax.IfKeyword.SpanStart,
                    syntax.CloseParenToken.Span.End),
                original.HasErrors);
        }

        public override BoundExpression InstrumentIfStatementCondition(BoundIfStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return AddConditionSequencePoint(base.InstrumentIfStatementCondition(original, rewrittenCondition, factory), original.Syntax, factory);
        }

        public override BoundStatement InstrumentLabelStatement(BoundLabeledStatement original, BoundStatement rewritten)
        {
            var labeledSyntax = (LabeledStatementSyntax)original.Syntax;
            var span = TextSpan.FromBounds(labeledSyntax.Identifier.SpanStart, labeledSyntax.ColonToken.Span.End);
            return new BoundSequencePointWithSpan(labeledSyntax,
                                                  base.InstrumentLabelStatement(original, rewritten), 
                                                  span);
        }

        public override BoundStatement InstrumentLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return LocalRewriter.AddSequencePoint(original.Syntax.Kind() == SyntaxKind.VariableDeclarator ?
                                        (VariableDeclaratorSyntax)original.Syntax :
                                        ((LocalDeclarationStatementSyntax)original.Syntax).Declaration.Variables.First(), 
                                    base.InstrumentLocalInitialization(original, rewritten));
        }

        public override BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            LockStatementSyntax lockSyntax = (LockStatementSyntax)original.Syntax;
            return new BoundSequencePointWithSpan(lockSyntax,
                                                  base.InstrumentLockTargetCapture(original, lockTargetCapture), 
                                                  TextSpan.FromBounds(lockSyntax.LockKeyword.SpanStart, lockSyntax.CloseParenToken.Span.End));
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            return new BoundSequencePoint(original.Syntax,
                                          base.InstrumentReturnStatement(original, rewritten));
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            SwitchStatementSyntax switchSyntax = (SwitchStatementSyntax)original.Syntax;
            TextSpan switchSequencePointSpan = TextSpan.FromBounds(
                switchSyntax.SwitchKeyword.SpanStart,
                switchSyntax.CloseParenToken.Span.End);

            return new BoundSequencePointWithSpan(
                syntax: switchSyntax,
                statementOpt: base.InstrumentSwitchStatement(original, rewritten),
                span: switchSequencePointSpan,
                hasErrors: false);
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return LocalRewriter.AddSequencePoint((UsingStatementSyntax)original.Syntax, 
                                    base.InstrumentUsingTargetCapture(original, usingTargetCapture));
        }

        public override BoundStatement InstrumentForEachStatementGotoContinue(BoundForEachStatement original, BoundStatement gotoContinue)
        {
            return new BoundSequencePoint(null, base.InstrumentForEachStatementGotoContinue(original, gotoContinue));
        }

        public override BoundStatement InstrumentWhileStatementGotoContinue(BoundWhileStatement original, BoundStatement gotoContinue)
        {
            return new BoundSequencePoint(null, base.InstrumentWhileStatementGotoContinue(original, gotoContinue));
        }
    }
}