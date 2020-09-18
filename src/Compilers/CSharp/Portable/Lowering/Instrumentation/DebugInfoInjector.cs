// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using RoslynEx;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This type is responsible for adding debugging sequence points for the executable code.
    /// It can be combined with other <see cref="Instrumenter"/>s. Usually, this class should be 
    /// the root of the chain in order to ensure sound debugging experience for the instrumented code.
    /// In other words, sequence points are typically applied after all other changes.
    /// </summary>
    internal partial class DebugInfoInjector : CompoundInstrumenter
    {
        /// <summary>
        /// A singleton object that performs only one type of instrumentation - addition of debugging sequence points. 
        /// </summary>
        public static readonly DebugInfoInjector Singleton = new DebugInfoInjector(Instrumenter.NoOp);

        public DebugInfoInjector(Instrumenter previous)
            : base(previous)
        {
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
            rewritten = base.InstrumentExpressionStatement(original, rewritten);

            if (original.IsConstructorInitializer())
            {
                var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
                if (originalSyntax == null)
                {
                    return BoundSequencePoint.CreateHidden(rewritten);
                }

                switch (originalSyntax.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration:
                        // This is an implicit constructor initializer.
                        var decl = (ConstructorDeclarationSyntax)originalSyntax;
                        return new BoundSequencePointWithSpan(decl, rewritten, CreateSpanForConstructorInitializer(decl));
                    case SyntaxKind.BaseConstructorInitializer:
                    case SyntaxKind.ThisConstructorInitializer:
                        var init = (ConstructorInitializerSyntax)originalSyntax;
                        Debug.Assert(init.Parent is object);
                        return new BoundSequencePointWithSpan(init, rewritten, CreateSpanForConstructorInitializer((ConstructorDeclarationSyntax)init.Parent));
                }
            }

            return AddSequencePoint(rewritten);
        }

        public override BoundStatement InstrumentFieldOrPropertyInitializer(BoundStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentFieldOrPropertyInitializer(original, rewritten);
            SyntaxNode syntax = original.Syntax;

            if (rewritten.Kind == BoundKind.Block)
            {
                var block = (BoundBlock)rewritten;
                return block.Update(block.Locals, block.LocalFunctions, ImmutableArray.Create(InstrumentFieldOrPropertyInitializer(block.Statements.Single(), syntax)));
            }

            return InstrumentFieldOrPropertyInitializer(rewritten, syntax);
        }

        private static BoundStatement InstrumentFieldOrPropertyInitializer(BoundStatement rewritten, SyntaxNode syntax)
        {
            if (syntax.IsKind(SyntaxKind.Parameter))
            {
                // This is an initialization of a generated property based on record parameter.
                return AddSequencePoint(rewritten);
            }

            Debug.Assert(syntax is { Parent: { Parent: { } } });
            var grandparent = syntax.Parent.Parent;
            switch (grandparent.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var declaratorSyntax = (VariableDeclaratorSyntax)grandparent;
                    return AddSequencePoint(declaratorSyntax, rewritten);

                case SyntaxKind.PropertyDeclaration:
                    var declaration = (PropertyDeclarationSyntax)grandparent;
                    return AddSequencePoint(declaration, rewritten);

                default:
                    throw ExceptionUtilities.UnexpectedValue(grandparent.Kind());
            }
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
            rewritten = base.InstrumentYieldBreakStatement(original, rewritten);

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (original.WasCompilerGenerated && originalSyntax?.Kind() == SyntaxKind.Block)
            {
                // implicit yield break added by the compiler
                return new BoundSequencePointWithSpan(originalSyntax, rewritten, ((BlockSyntax)originalSyntax).CloseBraceToken.Span);
            }

            return AddSequencePoint(rewritten);
        }

        public override BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentYieldReturnStatement(original, rewritten));
        }

        public override BoundStatement? CreateBlockPrologue(BoundBlock original, out Symbols.LocalSymbol? synthesizedLocal)
        {
            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);

            var previous = base.CreateBlockPrologue(original, out synthesizedLocal);
            if (originalSyntax?.Kind() == SyntaxKind.Block && !original.WasCompilerGenerated)
            {
                var oBspan = ((BlockSyntax)originalSyntax).OpenBraceToken.Span;
                return new BoundSequencePointWithSpan(originalSyntax, previous, oBspan);
            }
            else if (previous != null)
            {
                return BoundSequencePoint.Create(originalSyntax, previous);
            }

            return null;
        }

        public override BoundStatement? CreateBlockEpilogue(BoundBlock original)
        {
            var previous = base.CreateBlockEpilogue(original);

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (originalSyntax?.Kind() == SyntaxKind.Block && !original.WasCompilerGenerated)
            {
                // no need to mark "}" on the outermost block
                // as it cannot leave it normally. The block will have "return" at the end.
                SyntaxNode? parent = originalSyntax.Parent;
                if (parent == null || !(parent.IsAnonymousFunction() || parent is BaseMethodDeclarationSyntax))
                {
                    var cBspan = ((BlockSyntax)originalSyntax).CloseBraceToken.Span;
                    return new BoundSequencePointWithSpan(originalSyntax, previous, cBspan);
                }
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
            TextSpan? span;
            DoStatementSyntax? doSyntax;

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (originalSyntax == null)
            {
                doSyntax = null;
                span = null;
            }
            else
            {
                doSyntax = (DoStatementSyntax)originalSyntax;
                span = TextSpan.FromBounds(
                    doSyntax.WhileKeyword.SpanStart,
                    doSyntax.SemicolonToken.Span.End);
            }

            return BoundSequencePoint.Create(doSyntax, span, base.InstrumentDoStatementConditionalGotoStart(original, ifConditionGotoStart));
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStartOrBreak(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            WhileStatementSyntax? whileSyntax;
            TextSpan? conditionSequencePointSpan;

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (originalSyntax == null)
            {
                whileSyntax = null;
                conditionSequencePointSpan = null;
            }
            else
            {
                whileSyntax = (WhileStatementSyntax)originalSyntax;
                conditionSequencePointSpan = TextSpan.FromBounds(
                    whileSyntax.WhileKeyword.SpanStart,
                    whileSyntax.CloseParenToken.Span.End);
            }

            return BoundSequencePoint.Create(whileSyntax, conditionSequencePointSpan, base.InstrumentWhileStatementConditionalGotoStartOrBreak(original, ifConditionGotoStart));
        }

        private static BoundExpression AddConditionSequencePoint(BoundExpression condition, BoundStatement containingStatement, SyntheticBoundNodeFactory factory)
        {
            return AddConditionSequencePoint(condition, containingStatement.Syntax, factory);
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// foreach (Type var in |expr|) { }
        /// </summary>
        /// <remarks>
        /// Hit once, before looping begins.
        /// </remarks>
        public override BoundStatement InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, BoundStatement? collectionVarDecl)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)original.Syntax;
            forEachSyntax = TreeTracker.GetPreTransformationNode(forEachSyntax);
            return BoundSequencePoint.Create(forEachSyntax?.Expression,
                                          base.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl));
        }

        public override BoundStatement InstrumentForEachStatementDeconstructionVariablesDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            var forEachSyntax = (ForEachVariableStatementSyntax)original.Syntax;
            forEachSyntax = TreeTracker.GetPreTransformationNode(forEachSyntax);
            return BoundSequencePoint.Create(forEachSyntax, forEachSyntax?.Variable.Span, base.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl));
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
            var forEachSyntax = (CommonForEachStatementSyntax)original.Syntax;
            forEachSyntax = TreeTracker.GetPreTransformationNode(forEachSyntax);
            TextSpan? span = forEachSyntax == null
                ? null
                : forEachSyntax.AwaitKeyword != default
                ? TextSpan.FromBounds(forEachSyntax.AwaitKeyword.Span.Start, forEachSyntax.ForEachKeyword.Span.End)
                : forEachSyntax.ForEachKeyword.Span;

            var foreachKeywordSequencePoint = BoundSequencePoint.Create(forEachSyntax, span, null!);
            return new BoundStatementList(forEachSyntax!,
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
            TextSpan? iterationVarDeclSpan;
            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            switch (originalSyntax?.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    {
                        var forEachSyntax = (ForEachStatementSyntax)originalSyntax;
                        iterationVarDeclSpan = TextSpan.FromBounds(forEachSyntax.Type.SpanStart, forEachSyntax.Identifier.Span.End);
                        break;
                    }
                case SyntaxKind.ForEachVariableStatement:
                    {
                        var forEachSyntax = (ForEachVariableStatementSyntax)originalSyntax;
                        iterationVarDeclSpan = forEachSyntax.Variable.Span;
                        break;
                    }
                case null:
                    {
                        iterationVarDeclSpan = null;
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(originalSyntax.Kind());
            }
            return BoundSequencePoint.Create(originalSyntax, iterationVarDeclSpan,
                                                  base.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl));
        }

        public override BoundStatement InstrumentForStatementConditionalGotoStartOrBreak(BoundForStatement original, BoundStatement branchBack)
        {
            var originalConditionSyntax = TreeTracker.GetPreTransformationNode(original.Condition?.Syntax);

            // hidden sequence point if there is no condition
            return BoundSequencePoint.Create(originalConditionSyntax,
                                            base.InstrumentForStatementConditionalGotoStartOrBreak(original, branchBack));
        }

        public override BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        {
            var syntax = (CommonForEachStatementSyntax?)TreeTracker.GetPreTransformationNode(original.Syntax);
            return BoundSequencePoint.Create(syntax, syntax?.InKeyword.Span,
                                                  base.InstrumentForEachStatementConditionalGotoStart(original, branchBack));
        }

        public override BoundExpression InstrumentForStatementCondition(BoundForStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            return AddConditionSequencePoint(base.InstrumentForStatementCondition(original, rewrittenCondition, factory), original.Syntax, factory);
        }

        public override BoundStatement InstrumentIfStatement(BoundIfStatement original, BoundStatement rewritten)
        {
            var syntax = (IfStatementSyntax?)TreeTracker.GetPreTransformationNode(original.Syntax);
            TextSpan? span = syntax == null ? null : TextSpan.FromBounds(syntax.IfKeyword.SpanStart, syntax.CloseParenToken.Span.End);
            return BoundSequencePoint.Create(
                syntax,
                span,
                base.InstrumentIfStatement(original, rewritten),
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
            var labeledSyntax = (LabeledStatementSyntax?)TreeTracker.GetPreTransformationNode(original.Syntax);
            TextSpan? span = labeledSyntax == null ? null : TextSpan.FromBounds(labeledSyntax.Identifier.SpanStart, labeledSyntax.ColonToken.Span.End);
            return BoundSequencePoint.Create(labeledSyntax,
                                                  span,
                                                  base.InstrumentLabelStatement(original, rewritten));
        }

        public override BoundStatement InstrumentLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return AddSequencePoint(original.Syntax.Kind() == SyntaxKind.VariableDeclarator ?
                                        (VariableDeclaratorSyntax)original.Syntax :
                                        ((LocalDeclarationStatementSyntax)original.Syntax).Declaration.Variables.First(),
                                    base.InstrumentLocalInitialization(original, rewritten));
        }

        public override BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            var lockSyntax = (LockStatementSyntax?)TreeTracker.GetPreTransformationNode(original.Syntax);
            TextSpan? span = lockSyntax == null ? null : TextSpan.FromBounds(lockSyntax.LockKeyword.SpanStart, lockSyntax.CloseParenToken.Span.End);
            return BoundSequencePoint.Create(lockSyntax,
                                                  span,
                                                  base.InstrumentLockTargetCapture(original, lockTargetCapture));
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentReturnStatement(original, rewritten);

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (original.WasCompilerGenerated && original.ExpressionOpt == null && originalSyntax?.Kind() == SyntaxKind.Block)
            {
                // implicit return added by the compiler
                return new BoundSequencePointWithSpan(originalSyntax, rewritten, ((BlockSyntax)originalSyntax).CloseBraceToken.Span);
            }

            return BoundSequencePoint.Create(originalSyntax, rewritten);
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            var switchSyntax = (SwitchStatementSyntax?)TreeTracker.GetPreTransformationNode(original.Syntax);
            TextSpan? switchSequencePointSpan = switchSyntax == null
                ? null
                : TextSpan.FromBounds(
                switchSyntax.SwitchKeyword.SpanStart,
                (switchSyntax.CloseParenToken != default) ? switchSyntax.CloseParenToken.Span.End : switchSyntax.Expression.Span.End);

            return BoundSequencePoint.Create(
                syntax: switchSyntax,
                statement: base.InstrumentSwitchStatement(original, rewritten),
                part: switchSequencePointSpan,
                hasErrors: false);
        }

        public override BoundStatement InstrumentSwitchWhenClauseConditionalGotoBody(BoundExpression original, BoundStatement ifConditionGotoBody)
        {
            WhenClauseSyntax? whenClause = TreeTracker.GetPreTransformationNode(original.Syntax.FirstAncestorOrSelf<WhenClauseSyntax>());

            return BoundSequencePoint.Create(
                syntax: whenClause,
                statement: base.InstrumentSwitchWhenClauseConditionalGotoBody(original, ifConditionGotoBody),
                part: whenClause?.Span);
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return AddSequencePoint((UsingStatementSyntax)original.Syntax,
                                    base.InstrumentUsingTargetCapture(original, usingTargetCapture));
        }

        public override BoundExpression InstrumentCatchClauseFilter(BoundCatchBlock original, BoundExpression rewrittenFilter, SyntheticBoundNodeFactory factory)
        {
            rewrittenFilter = base.InstrumentCatchClauseFilter(original, rewrittenFilter, factory);

            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);
            if (originalSyntax == null)
            {
                return new BoundSequencePointExpression(null!, rewrittenFilter, rewrittenFilter.Type);
            }

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            CatchFilterClauseSyntax? filterClause = ((CatchClauseSyntax)originalSyntax).Filter;
            Debug.Assert(filterClause is { });
            return AddConditionSequencePoint(new BoundSequencePointExpression(filterClause, rewrittenFilter, rewrittenFilter.Type), filterClause, factory);
        }

        public override BoundExpression InstrumentSwitchStatementExpression(BoundStatement original, BoundExpression rewrittenExpression, SyntheticBoundNodeFactory factory)
        {
            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the expression are being executed.
            return AddConditionSequencePoint(base.InstrumentSwitchStatementExpression(original, rewrittenExpression, factory), original.Syntax, factory);
        }

        public override BoundExpression InstrumentSwitchExpressionArmExpression(BoundExpression original, BoundExpression rewrittenExpression, SyntheticBoundNodeFactory factory)
        {
            var originalSyntax = TreeTracker.GetPreTransformationNode(original.Syntax);

            return new BoundSequencePointExpression(originalSyntax!, base.InstrumentSwitchExpressionArmExpression(original, rewrittenExpression, factory), rewrittenExpression.Type);
        }

        public override BoundStatement InstrumentSwitchBindCasePatternVariables(BoundStatement bindings)
        {
            // Mark the code that binds pattern variables to their values as hidden.
            // We do it to tell that this is not a part of previous statement.
            return BoundSequencePoint.CreateHidden(base.InstrumentSwitchBindCasePatternVariables(bindings));
        }
    }
}
