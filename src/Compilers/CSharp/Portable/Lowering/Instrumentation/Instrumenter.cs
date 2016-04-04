// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class Instrumenter
    {
        public static readonly Instrumenter NoOp = new Instrumenter();

        public Instrumenter()
        {
        }

        private static BoundStatement InstrumentStatement(BoundStatement original, BoundStatement rewritten)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            return rewritten;
        }

        public virtual BoundStatement InstrumentNoOpStatement(BoundNoOpStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }


        public virtual BoundStatement InstrumentYieldBreakStatement(BoundYieldBreakStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        /// <summary>
        /// Return a node that is associated with open brace of the block. Ok to return null.
        /// </summary>
        public virtual BoundStatement CreateBlockPrologue(BoundBlock original)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.Block);
            return null;
        }

        /// <summary>
        /// Return a node that is associated with close brace of the block. Ok to return null.
        /// </summary>
        public virtual BoundStatement CreateBlockEpilogue(BoundBlock original)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.Block);
            return null;
        }

        public virtual BoundStatement InstrumentThrowStatement(BoundThrowStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentContinueStatement(BoundContinueStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentGotoStatement(BoundGotoStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentExpressionStatement(BoundExpressionStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentBreakStatement(BoundBreakStatement original, BoundStatement rewritten)
        {
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundExpression InstrumentDoStatementCondition(BoundDoStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.DoStatement);
            Debug.Assert(factory != null);
            return rewrittenCondition;
        }

        public virtual BoundExpression InstrumentWhileStatementCondition(BoundWhileStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.WhileStatement);
            Debug.Assert(factory != null);
            return rewrittenCondition;
        }

        public virtual BoundStatement InstrumentDoStatementConditionalGotoStart(BoundDoStatement original, BoundStatement ifConditionGotoStart)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.DoStatement);
            return ifConditionGotoStart; 
        }

        public virtual BoundStatement InstrumentWhileStatementConditionalGotoStart(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.WhileStatement);
            return ifConditionGotoStart;
        }

        public virtual BoundStatement InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, BoundStatement collectionVarDecl)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return collectionVarDecl;
        }

        public virtual BoundStatement InstrumentForEachStatement(BoundForEachStatement original, BoundStatement rewritten)
        {
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentForEachStatementIterationVarDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return iterationVarDecl;
        }

        public virtual BoundStatement InstrumentForStatementGotoEnd(BoundForStatement original, BoundStatement gotoEnd)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForStatement);
            return gotoEnd;
        }

        public virtual BoundStatement InstrumentForEachStatementGotoEnd(BoundForEachStatement original, BoundStatement gotoEnd)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return gotoEnd;
        }

        public virtual BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return branchBack;
        }

        public virtual BoundStatement InstrumentForStatementConditionalGotoStart(BoundForStatement original, BoundStatement branchBack)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForStatement);
            return branchBack;
        }

        public virtual BoundExpression InstrumentForStatementCondition(BoundForStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForStatement);
            Debug.Assert(factory != null);
            return rewrittenCondition;
        }

        public virtual BoundStatement InstrumentIfStatement(BoundIfStatement original, BoundStatement rewritten)
        {
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.IfStatement);
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundExpression InstrumentIfStatementCondition(BoundIfStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.IfStatement);
            Debug.Assert(factory != null);
            return rewrittenCondition;
        }

        public virtual BoundStatement InstrumentLabelStatement(BoundLabeledStatement original, BoundStatement rewritten)
        {
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.LabeledStatement);
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.VariableDeclarator ||
                         (original.Syntax.Kind() == SyntaxKind.LocalDeclarationStatement &&
                                ((LocalDeclarationStatementSyntax)original.Syntax).Declaration.Variables.Count == 1));
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.LockStatement);
            return lockTargetCapture;
        }

        public virtual BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            return rewritten;
        }

        public virtual BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.SwitchStatement);
            return InstrumentStatement(original, rewritten);
        }

        public virtual BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.UsingStatement);
            return usingTargetCapture;
        }

        public virtual BoundStatement InstrumentWhileStatementGotoContinue(BoundWhileStatement original, BoundStatement gotoContinue)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.WhileStatement);
            return gotoContinue;
        }

        public virtual BoundStatement InstrumentForEachStatementGotoContinue(BoundForEachStatement original, BoundStatement gotoContinue)
        {
            Debug.Assert(!original.WasCompilerGenerated);
            Debug.Assert(original.Syntax.Kind() == SyntaxKind.ForEachStatement);
            return gotoContinue;
        }
    }
}