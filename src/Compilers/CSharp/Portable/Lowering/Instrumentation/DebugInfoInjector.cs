// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        private static readonly DebugInfoInjector s_singleton = new DebugInfoInjector(NoOp);

        private DebugInfoInjector(Instrumenter previous)
            : base(previous)
        {
        }

        public static DebugInfoInjector Create(Instrumenter previous)
            => (previous == NoOp) ? s_singleton : new DebugInfoInjector(previous);

        protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
            => Create(previous);

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
                switch (original.Syntax)
                {
                    case ConstructorDeclarationSyntax ctorDecl:
                        // Implicit constructor initializer:
                        Debug.Assert(ctorDecl.Initializer == null);

                        TextSpan span;
                        if (ctorDecl.Modifiers.Any(SyntaxKind.StaticKeyword))
                        {
                            Debug.Assert(ctorDecl.Body != null);

                            // [SomeAttribute] static MyCtorName(...) [|{|] ... }
                            var start = ctorDecl.Body.OpenBraceToken.SpanStart;
                            var end = ctorDecl.Body.OpenBraceToken.Span.End;
                            span = TextSpan.FromBounds(start, end);
                        }
                        else
                        {
                            //  [SomeAttribute] [|public MyCtorName(params int[] values)|] { ... }
                            span = CreateSpan(ctorDecl.Modifiers, ctorDecl.Identifier, ctorDecl.ParameterList.CloseParenToken);
                        }

                        return new BoundSequencePointWithSpan(ctorDecl, rewritten, span);

                    case ConstructorInitializerSyntax ctorInit:
                        // Explicit constructor initializer:

                        //  [SomeAttribute] public MyCtorName(params int[] values): [|base()|] { ... }
                        return new BoundSequencePointWithSpan(ctorInit, rewritten,
                            TextSpan.FromBounds(ctorInit.ThisOrBaseKeyword.SpanStart, ctorInit.ArgumentList.CloseParenToken.Span.End));

                    case TypeDeclarationSyntax typeDecl:
                        // Primary constructor with implicit base initializer:
                        //   class [|C<T>(int X, int Y)|] : B;
                        Debug.Assert(typeDecl.ParameterList != null);
                        Debug.Assert(typeDecl.BaseList?.Types.Any(t => t is PrimaryConstructorBaseTypeSyntax { ArgumentList: not null }) != true);

                        return new BoundSequencePointWithSpan(typeDecl, rewritten, TextSpan.FromBounds(typeDecl.Identifier.SpanStart, typeDecl.ParameterList.Span.End));

                    case PrimaryConstructorBaseTypeSyntax baseInit:
                        // Explicit base initializer:
                        //   class C<T>(int X, int Y) : [|B(...)|];
                        return new BoundSequencePointWithSpan(baseInit, rewritten, baseInit.Span);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(original.Syntax.Kind());
                }
            }
            else if (original.Syntax is ParameterSyntax parameterSyntax)
            {
                // Record property setter sequence point.
                return new BoundSequencePointWithSpan(parameterSyntax, rewritten, CreateSpan(parameterSyntax));
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
                return block.Update(block.Locals, block.LocalFunctions, block.HasUnsafeModifier, block.Instrumentation, ImmutableArray.Create(InstrumentFieldOrPropertyInitializer(block.Statements.Single(), syntax)));
            }

            return InstrumentFieldOrPropertyInitializer(rewritten, syntax);
        }

        private static BoundStatement InstrumentFieldOrPropertyInitializer(BoundStatement rewritten, SyntaxNode syntax)
        {
            if (syntax.IsKind(SyntaxKind.Parameter))
            {
                // This is an initialization of a generated property based on record parameter.
                // Do not add sequence point - the initialization is trivial and there is no value stepping over every backing field assignment.
                return rewritten;
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

        public override void InstrumentBlock(BoundBlock original, LocalRewriter rewriter, ref TemporaryArray<LocalSymbol> additionalLocals, out BoundStatement? prologue, out BoundStatement? epilogue, out BoundBlockInstrumentation? instrumentation)
        {
            base.InstrumentBlock(original, rewriter, ref additionalLocals, out var previousPrologue, out var previousEpilogue, out instrumentation);

            prologue = previousPrologue;
            epilogue = previousEpilogue;

            if (original.Syntax is BlockSyntax blockSyntax && !original.WasCompilerGenerated)
            {
                prologue = new BoundSequencePointWithSpan(original.Syntax, previousPrologue, blockSyntax.OpenBraceToken.Span);

                // no need to mark "}" on the outermost block
                // as it cannot leave it normally. The block will have "return" at the end.
                SyntaxNode? parent = original.Syntax.Parent;
                if (parent == null || !(parent.IsAnonymousFunction() || parent is BaseMethodDeclarationSyntax))
                {
                    epilogue = new BoundSequencePointWithSpan(original.Syntax, previousEpilogue, blockSyntax.CloseBraceToken.Span);
                }
            }
            else if (original == rewriter.CurrentMethodBody)
            {
                // Add hidden sequence point at the start of
                // 1) a prologue added by instrumentation.
                // 2) a synthesized main entry point for top-level code.
                // 3) synthesized body of primary and copy constructors of a record type that has at least one parameter.
                //    This hidden sequence point covers assignments of parameters/original fields to the current instance backing fields.
                //    These assignments do not have their own sequence points.
                if (previousPrologue != null ||
                    rewriter.Factory.TopLevelMethod is SynthesizedSimpleProgramEntryPointSymbol ||
                    original.Syntax is RecordDeclarationSyntax { ParameterList.Parameters.Count: > 0 })
                {
                    prologue = BoundSequencePoint.CreateHidden(previousPrologue);
                }

                if (previousEpilogue != null)
                {
                    epilogue = BoundSequencePoint.CreateHidden(previousEpilogue);
                }
            }
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

        public override BoundStatement InstrumentWhileStatementConditionalGotoStartOrBreak(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            WhileStatementSyntax whileSyntax = (WhileStatementSyntax)original.Syntax;
            TextSpan conditionSequencePointSpan = TextSpan.FromBounds(
                whileSyntax.WhileKeyword.SpanStart,
                whileSyntax.CloseParenToken.Span.End);

            return new BoundSequencePointWithSpan(whileSyntax, base.InstrumentWhileStatementConditionalGotoStartOrBreak(original, ifConditionGotoStart), conditionSequencePointSpan);
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
            return new BoundSequencePoint(forEachSyntax.Expression,
                                          base.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl));
        }

        public override BoundStatement InstrumentForEachStatementDeconstructionVariablesDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            var forEachSyntax = (ForEachVariableStatementSyntax)original.Syntax;
            return new BoundSequencePointWithSpan(forEachSyntax, base.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl), forEachSyntax.Variable.Span);
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

            var foreachKeywordSequencePoint = new BoundSequencePointWithSpan(forEachSyntax, null, span);
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
            return new BoundSequencePointWithSpan(original.Syntax,
                                                  base.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl),
                                                  iterationVarDeclSpan);
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

        public override BoundStatement InstrumentIfStatementConditionalGoto(BoundIfStatement original, BoundStatement rewritten)
        {
            var syntax = (IfStatementSyntax)original.Syntax;
            return new BoundSequencePointWithSpan(
                syntax,
                base.InstrumentIfStatementConditionalGoto(original, rewritten),
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

        public override BoundStatement InstrumentUserDefinedLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return AddSequencePoint(original.Syntax.Kind() == SyntaxKind.VariableDeclarator ?
                                        (VariableDeclaratorSyntax)original.Syntax :
                                        ((LocalDeclarationStatementSyntax)original.Syntax).Declaration.Variables.First(),
                                    base.InstrumentUserDefinedLocalInitialization(original, rewritten));
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
            rewritten = base.InstrumentReturnStatement(original, rewritten);

            if (original.WasCompilerGenerated && original.ExpressionOpt == null && original.Syntax.Kind() == SyntaxKind.Block)
            {
                // implicit return added by the compiler
                return new BoundSequencePointWithSpan(original.Syntax, rewritten, ((BlockSyntax)original.Syntax).CloseBraceToken.Span);
            }

            if (original.Syntax is ParameterSyntax parameterSyntax)
            {
                Debug.Assert(parameterSyntax is { Parent.Parent: RecordDeclarationSyntax });

                // Record property getter sequence point
                return new BoundSequencePointWithSpan(parameterSyntax, rewritten, CreateSpan(parameterSyntax));
            }

            return new BoundSequencePoint(original.Syntax, rewritten);
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            SwitchStatementSyntax switchSyntax = (SwitchStatementSyntax)original.Syntax;
            TextSpan switchSequencePointSpan = TextSpan.FromBounds(
                switchSyntax.SwitchKeyword.SpanStart,
                (switchSyntax.CloseParenToken != default) ? switchSyntax.CloseParenToken.Span.End : switchSyntax.Expression.Span.End);

            return new BoundSequencePointWithSpan(
                syntax: switchSyntax,
                statementOpt: base.InstrumentSwitchStatement(original, rewritten),
                span: switchSequencePointSpan,
                hasErrors: false);
        }

        public override BoundStatement InstrumentSwitchWhenClauseConditionalGotoBody(BoundExpression original, BoundStatement ifConditionGotoBody)
        {
            WhenClauseSyntax? whenClause = original.Syntax.FirstAncestorOrSelf<WhenClauseSyntax>();
            Debug.Assert(whenClause != null);

            return new BoundSequencePointWithSpan(
                syntax: whenClause,
                statementOpt: base.InstrumentSwitchWhenClauseConditionalGotoBody(original, ifConditionGotoBody),
                span: whenClause.Span);
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return AddSequencePoint((UsingStatementSyntax)original.Syntax,
                                    base.InstrumentUsingTargetCapture(original, usingTargetCapture));
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
            base.InstrumentCatchBlock(
                original,
                ref rewrittenSource,
                ref rewrittenFilterPrologue,
                ref rewrittenFilter,
                ref rewrittenBody,
                ref rewrittenType,
                factory);

            if (original.WasCompilerGenerated || rewrittenFilter is null)
            {
                return;
            }

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.

            var filterClause = ((CatchClauseSyntax)original.Syntax).Filter;
            Debug.Assert(filterClause is not null);

            rewrittenFilter = AddConditionSequencePoint(new BoundSequencePointExpression(filterClause, rewrittenFilter, rewrittenFilter.Type), filterClause, factory);
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
