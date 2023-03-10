// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
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
                switch (original.Syntax.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration:
                        // This is an implicit constructor initializer.
                        var decl = (ConstructorDeclarationSyntax)original.Syntax;
                        return new BoundSequencePointWithSpan(decl, rewritten, CreateSpanForConstructorInitializer(decl));
                    case SyntaxKind.BaseConstructorInitializer:
                    case SyntaxKind.ThisConstructorInitializer:
                        var init = (ConstructorInitializerSyntax)original.Syntax;
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
                return block.Update(block.Locals, block.LocalFunctions, block.HasUnsafeModifier, ImmutableArray.Create(InstrumentFieldOrPropertyInitializer(block.Statements.Single(), syntax)));
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

            if (original.WasCompilerGenerated && original.Syntax.Kind() == SyntaxKind.Block)
            {
                // implicit yield break added by the compiler
                return new BoundSequencePointWithSpan(original.Syntax, rewritten, ((BlockSyntax)original.Syntax).CloseBraceToken.Span);
            }

            return AddSequencePoint(rewritten);
        }

        public override BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return AddSequencePoint(base.InstrumentYieldReturnStatement(original, rewritten));
        }

        public override BoundStatement? CreateBlockPrologue(BoundBlock original, out Symbols.LocalSymbol? synthesizedLocal)
        {
            var previous = base.CreateBlockPrologue(original, out synthesizedLocal);
            if (original.Syntax.Kind() == SyntaxKind.Block && !original.WasCompilerGenerated)
            {
                var oBspan = ((BlockSyntax)original.Syntax).OpenBraceToken.Span;
                return new BoundSequencePointWithSpan(original.Syntax, previous, oBspan);
            }
            else if (previous != null)
            {
                // <Metalama> changed from constructor call to call to Create method
                return BoundSequencePoint.Create(original.Syntax, previous);
                // </Metalama>
            }

            return null;
        }

        public override BoundStatement? CreateBlockEpilogue(BoundBlock original)
        {
            var previous = base.CreateBlockEpilogue(original);

            if (original.Syntax.Kind() == SyntaxKind.Block && !original.WasCompilerGenerated)
            {
                // no need to mark "}" on the outermost block
                // as it cannot leave it normally. The block will have "return" at the end.
                SyntaxNode? parent = original.Syntax.Parent;
                if (parent == null || !(parent.IsAnonymousFunction() || parent is BaseMethodDeclarationSyntax))
                {
                    var cBspan = ((BlockSyntax)original.Syntax).CloseBraceToken.Span;
                    return new BoundSequencePointWithSpan(original.Syntax, previous, cBspan);
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
            var doSyntax = (DoStatementSyntax)original.Syntax;
            var span = TextSpan.FromBounds(
                doSyntax.WhileKeyword.SpanStart,
                doSyntax.SemicolonToken.Span.End);

            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(doSyntax, span, base.InstrumentDoStatementConditionalGotoStart(original, ifConditionGotoStart));
            // </Metalama>
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStartOrBreak(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            WhileStatementSyntax whileSyntax = (WhileStatementSyntax)original.Syntax;
            TextSpan conditionSequencePointSpan = TextSpan.FromBounds(
                whileSyntax.WhileKeyword.SpanStart,
                whileSyntax.CloseParenToken.Span.End);

            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(whileSyntax, conditionSequencePointSpan, base.InstrumentWhileStatementConditionalGotoStartOrBreak(original, ifConditionGotoStart));
            // </Metalama>
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
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(forEachSyntax.Expression,
                                          base.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl));
            // </Metalama>
        }

        public override BoundStatement InstrumentForEachStatementDeconstructionVariablesDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            var forEachSyntax = (ForEachVariableStatementSyntax)original.Syntax;
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(forEachSyntax, forEachSyntax.Variable.Span, base.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl));
            // </Metalama>
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
            var span = forEachSyntax.AwaitKeyword != default
                ? TextSpan.FromBounds(forEachSyntax.AwaitKeyword.Span.Start, forEachSyntax.ForEachKeyword.Span.End)
                : forEachSyntax.ForEachKeyword.Span;

            // <Metalama> changed from constructor call to call to Create method
            var foreachKeywordSequencePoint = BoundSequencePoint.Create(forEachSyntax, span, null!);
            // </Metalama>
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
            TextSpan iterationVarDeclSpan;
            switch (original.Syntax.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    {
                        var forEachSyntax = (ForEachStatementSyntax)original.Syntax;
                        iterationVarDeclSpan = TextSpan.FromBounds(forEachSyntax.Type.SpanStart, forEachSyntax.Identifier.Span.End);
                        break;
                    }
                case SyntaxKind.ForEachVariableStatement:
                    {
                        var forEachSyntax = (ForEachVariableStatementSyntax)original.Syntax;
                        iterationVarDeclSpan = forEachSyntax.Variable.Span;
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(original.Syntax.Kind());
            }
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(original.Syntax, iterationVarDeclSpan,
                                                  base.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl));
            // </Metalama>
        }

        public override BoundStatement InstrumentForStatementConditionalGotoStartOrBreak(BoundForStatement original, BoundStatement branchBack)
        {
            // hidden sequence point if there is no condition
            return BoundSequencePoint.Create(original.Condition?.Syntax,
                                            base.InstrumentForStatementConditionalGotoStartOrBreak(original, branchBack));
        }

        public override BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        {
            var syntax = (CommonForEachStatementSyntax)original.Syntax;
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(syntax, syntax.InKeyword.Span,
                                                  base.InstrumentForEachStatementConditionalGotoStart(original, branchBack));
            // </Metalama>
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
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(
                syntax,
                TextSpan.FromBounds(
                    syntax.IfKeyword.SpanStart,
                    syntax.CloseParenToken.Span.End),
                base.InstrumentIfStatement(original, rewritten),
                original.HasErrors);
            // </Metalama>
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
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(labeledSyntax,
                                                  span,
                                                  base.InstrumentLabelStatement(original, rewritten));
            // </Metalama>
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
            LockStatementSyntax lockSyntax = (LockStatementSyntax)original.Syntax;
            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(lockSyntax,
                                                  TextSpan.FromBounds(lockSyntax.LockKeyword.SpanStart, lockSyntax.CloseParenToken.Span.End),
                                                  base.InstrumentLockTargetCapture(original, lockTargetCapture));
            // </Metalama>
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentReturnStatement(original, rewritten);

            if (original.WasCompilerGenerated && original.ExpressionOpt == null && original.Syntax.Kind() == SyntaxKind.Block)
            {
                // implicit return added by the compiler
                return new BoundSequencePointWithSpan(original.Syntax, rewritten, ((BlockSyntax)original.Syntax).CloseBraceToken.Span);
            }

            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(original.Syntax, rewritten);
            // </Metalama>
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            SwitchStatementSyntax switchSyntax = (SwitchStatementSyntax)original.Syntax;
            TextSpan switchSequencePointSpan = TextSpan.FromBounds(
                switchSyntax.SwitchKeyword.SpanStart,
                (switchSyntax.CloseParenToken != default) ? switchSyntax.CloseParenToken.Span.End : switchSyntax.Expression.Span.End);

            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(
                syntax: switchSyntax,
                statement: base.InstrumentSwitchStatement(original, rewritten),
                part: switchSequencePointSpan,
                hasErrors: false);
            // </Metalama>
        }

        public override BoundStatement InstrumentSwitchWhenClauseConditionalGotoBody(BoundExpression original, BoundStatement ifConditionGotoBody)
        {
            WhenClauseSyntax? whenClause = original.Syntax.FirstAncestorOrSelf<WhenClauseSyntax>();
            Debug.Assert(whenClause != null);

            // <Metalama> changed from constructor call to call to Create method
            return BoundSequencePoint.Create(
                syntax: whenClause,
                statement: base.InstrumentSwitchWhenClauseConditionalGotoBody(original, ifConditionGotoBody),
                part: whenClause.Span);
            // </Metalama>
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return AddSequencePoint((UsingStatementSyntax)original.Syntax,
                                    base.InstrumentUsingTargetCapture(original, usingTargetCapture));
        }

        public override BoundExpression InstrumentCatchClauseFilter(BoundCatchBlock original, BoundExpression rewrittenFilter, SyntheticBoundNodeFactory factory)
        {
            rewrittenFilter = base.InstrumentCatchClauseFilter(original, rewrittenFilter, factory);

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            CatchFilterClauseSyntax? filterClause = ((CatchClauseSyntax)original.Syntax).Filter;
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
            return new BoundSequencePointExpression(original.Syntax, base.InstrumentSwitchExpressionArmExpression(original, rewrittenExpression, factory), rewrittenExpression.Type);
        }

        public override BoundStatement InstrumentSwitchBindCasePatternVariables(BoundStatement bindings)
        {
            // Mark the code that binds pattern variables to their values as hidden.
            // We do it to tell that this is not a part of previous statement.
            return BoundSequencePoint.CreateHidden(base.InstrumentSwitchBindCasePatternVariables(bindings));
        }
    }
}
