// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Utility class, provides a convenient way of combining various <see cref="Instrumenter"/>s in a chain,
    /// allowing each of them to apply specific instrumentations in particular order.
    /// 
    /// Default implementation of all APIs delegates to the "previous" <see cref="Instrumenter"/> passed as a parameter
    /// to the constructor of this class. Usually, derived types are going to let the base (this class) to do its work first
    /// and then operate on the result they get back.
    /// </summary>
    internal class CompoundInstrumenter : Instrumenter
    {
        public CompoundInstrumenter(Instrumenter previous)
        {
            Debug.Assert(previous != null);
            Previous = previous;
        }

        public Instrumenter Previous { get; }

        public override BoundStatement InstrumentNoOpStatement(BoundNoOpStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentNoOpStatement(original, rewritten);
        }

        public override BoundStatement InstrumentYieldBreakStatement(BoundYieldBreakStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentYieldBreakStatement(original, rewritten);
        }

        public override BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentYieldReturnStatement(original, rewritten);
        }

        public override BoundStatement InstrumentThrowStatement(BoundThrowStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentThrowStatement(original, rewritten);
        }

        public override BoundStatement InstrumentContinueStatement(BoundContinueStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentContinueStatement(original, rewritten);
        }

        public override BoundStatement InstrumentGotoStatement(BoundGotoStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentGotoStatement(original, rewritten);
        }

        public override BoundStatement InstrumentExpressionStatement(BoundExpressionStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentExpressionStatement(original, rewritten);
        }

        public override BoundStatement InstrumentFieldOrPropertyInitializer(BoundStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentFieldOrPropertyInitializer(original, rewritten);
        }

        public override BoundStatement InstrumentBreakStatement(BoundBreakStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentBreakStatement(original, rewritten);
        }

        public override BoundStatement CreateBlockPrologue(BoundBlock original, out Symbols.LocalSymbol synthesizedLocal)
        {
            return Previous.CreateBlockPrologue(original, out synthesizedLocal);
        }

        public override BoundStatement CreateBlockEpilogue(BoundBlock original)
        {
            return Previous.CreateBlockEpilogue(original);
        }

        public override BoundExpression InstrumentDoStatementCondition(BoundDoStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentDoStatementCondition(original, rewrittenCondition, factory);
        }

        public override BoundStatement InstrumentDoStatementConditionalGotoStart(BoundDoStatement original, BoundStatement ifConditionGotoStart)
        {
            return Previous.InstrumentDoStatementConditionalGotoStart(original, ifConditionGotoStart);
        }

        public override BoundStatement InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, BoundStatement collectionVarDecl)
        {
            return Previous.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl);
        }

        public override BoundStatement InstrumentForEachStatement(BoundForEachStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentForEachStatement(original, rewritten);
        }

        public override BoundStatement InstrumentForEachStatementIterationVarDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            return Previous.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl);
        }

        public override BoundStatement InstrumentForStatementConditionalGotoStartOrBreak(BoundForStatement original, BoundStatement branchBack)
        {
            return Previous.InstrumentForStatementConditionalGotoStartOrBreak(original, branchBack);
        }

        public override BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        {
            return Previous.InstrumentForEachStatementConditionalGotoStart(original, branchBack);
        }

        public override BoundExpression InstrumentForStatementCondition(BoundForStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentForStatementCondition(original, rewrittenCondition, factory);
        }

        public override BoundStatement InstrumentIfStatement(BoundIfStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentIfStatement(original, rewritten);
        }

        public override BoundExpression InstrumentIfStatementCondition(BoundIfStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentIfStatementCondition(original, rewrittenCondition, factory);
        }

        public override BoundStatement InstrumentLabelStatement(BoundLabeledStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentLabelStatement(original, rewritten);
        }

        public override BoundStatement InstrumentLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return Previous.InstrumentLocalInitialization(original, rewritten);
        }

        public override BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            return Previous.InstrumentLockTargetCapture(original, lockTargetCapture);
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentReturnStatement(original, rewritten);
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            return Previous.InstrumentSwitchStatement(original, rewritten);
        }

        public override BoundStatement InstrumentSwitchWhenClauseConditionalGotoBody(BoundExpression original, BoundStatement ifConditionGotoBody)
        {
            return Previous.InstrumentSwitchWhenClauseConditionalGotoBody(original, ifConditionGotoBody);
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return Previous.InstrumentUsingTargetCapture(original, usingTargetCapture);
        }

        public override BoundExpression InstrumentWhileStatementCondition(BoundWhileStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentWhileStatementCondition(original, rewrittenCondition, factory);
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStartOrBreak(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            return Previous.InstrumentWhileStatementConditionalGotoStartOrBreak(original, ifConditionGotoStart);
        }

        public override BoundExpression InstrumentCatchClauseFilter(BoundCatchBlock original, BoundExpression rewrittenFilter, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentCatchClauseFilter(original, rewrittenFilter, factory);
        }

        public override BoundExpression InstrumentSwitchStatementExpression(BoundStatement original, BoundExpression rewrittenExpression, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentSwitchStatementExpression(original, rewrittenExpression, factory);
        }

        public override BoundStatement InstrumentSwitchBindCasePatternVariables(BoundStatement bindings)
        {
            return Previous.InstrumentSwitchBindCasePatternVariables(bindings);
        }

        public override BoundStatement InstrumentForEachStatementDeconstructionVariablesDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            return Previous.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl);
        }
    }
}
