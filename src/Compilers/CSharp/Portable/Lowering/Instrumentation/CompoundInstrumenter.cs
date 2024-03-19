// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Shared.Collections;

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
    internal abstract class CompoundInstrumenter : Instrumenter
    {
        public CompoundInstrumenter(Instrumenter previous)
        {
            Debug.Assert(previous != null);
            Previous = previous;
        }

        public Instrumenter Previous { get; }

        /// <summary>
        /// Returns <see cref="CompoundInstrumenter"/> with <see cref="Previous"/> instrumenter set to <paramref name="previous"/>.
        /// </summary>
        public CompoundInstrumenter WithPrevious(Instrumenter previous)
            => ReferenceEquals(previous, Previous) ? this : WithPreviousImpl(previous);

        protected abstract CompoundInstrumenter WithPreviousImpl(Instrumenter previous);

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

        public override void PreInstrumentBlock(BoundBlock original, LocalRewriter rewriter)
        {
            Previous.PreInstrumentBlock(original, rewriter);
        }

        public override void InstrumentBlock(BoundBlock original, LocalRewriter rewriter, ref TemporaryArray<LocalSymbol> additionalLocals, out BoundStatement? prologue, out BoundStatement? epilogue, out BoundBlockInstrumentation? instrumentation)
        {
            Previous.InstrumentBlock(original, rewriter, ref additionalLocals, out prologue, out epilogue, out instrumentation);
        }

        public override BoundExpression InstrumentDoStatementCondition(BoundDoStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentDoStatementCondition(original, rewrittenCondition, factory);
        }

        public override BoundStatement InstrumentDoStatementConditionalGotoStart(BoundDoStatement original, BoundStatement ifConditionGotoStart)
        {
            return Previous.InstrumentDoStatementConditionalGotoStart(original, ifConditionGotoStart);
        }

        public override BoundStatement? InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, BoundStatement? collectionVarDecl)
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

        public override BoundStatement InstrumentUserDefinedLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return Previous.InstrumentUserDefinedLocalInitialization(original, rewritten);
        }

        public override BoundExpression InstrumentUserDefinedLocalAssignment(BoundAssignmentOperator original)
        {
            return Previous.InstrumentUserDefinedLocalAssignment(original);
        }

        public override BoundExpression InstrumentCall(BoundCall original, BoundExpression rewritten)
        {
            return Previous.InstrumentCall(original, rewritten);
        }

        public override void InterceptCallAndAdjustArguments(
            ref MethodSymbol method,
            ref BoundExpression? receiver,
            ref ImmutableArray<BoundExpression> arguments,
            ref ImmutableArray<RefKind> argumentRefKindsOpt)
        {
            Previous.InterceptCallAndAdjustArguments(ref method, ref receiver, ref arguments, ref argumentRefKindsOpt);
        }

        public override BoundExpression InstrumentObjectCreationExpression(BoundObjectCreationExpression original, BoundExpression rewritten)
        {
            return Previous.InstrumentObjectCreationExpression(original, rewritten);
        }

        public override BoundExpression InstrumentFunctionPointerInvocation(BoundFunctionPointerInvocation original, BoundExpression rewritten)
        {
            return Previous.InstrumentFunctionPointerInvocation(original, rewritten);
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

        public override void InstrumentCatchBlock(
            BoundCatchBlock original,
            ref BoundExpression? rewrittenSource,
            ref BoundStatementList? rewrittenFilterPrologue,
            ref BoundExpression? rewrittenFilter,
            ref BoundBlock rewrittenBody,
            ref TypeSymbol? rewrittenType,
            SyntheticBoundNodeFactory factory)
        {
            Previous.InstrumentCatchBlock(
                original,
                ref rewrittenSource,
                ref rewrittenFilterPrologue,
                ref rewrittenFilter,
                ref rewrittenBody,
                ref rewrittenType,
                factory);
        }

        public override BoundExpression InstrumentSwitchStatementExpression(BoundStatement original, BoundExpression rewrittenExpression, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentSwitchStatementExpression(original, rewrittenExpression, factory);
        }

        public override BoundExpression InstrumentSwitchExpressionArmExpression(BoundExpression original, BoundExpression rewrittenExpression, SyntheticBoundNodeFactory factory)
        {
            return Previous.InstrumentSwitchExpressionArmExpression(original, rewrittenExpression, factory);
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
