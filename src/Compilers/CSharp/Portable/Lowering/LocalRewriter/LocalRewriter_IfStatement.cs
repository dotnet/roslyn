// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            Debug.Assert(node != null);
            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenConsequence = VisitStatement(node.Consequence);
            var rewrittenAlternative = VisitStatement(node.AlternativeOpt);
            var syntax = (IfStatementSyntax)node.Syntax;

            if (TryEliminateIfStatement(syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, out var eliminateConditionResult))
            {
                return VisitStatement(eliminateConditionResult);
            }

            // EnC: We need to insert a hidden sequence point to handle function remapping in case
            // the containing method is edited while methods invoked in the condition are being executed.
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                rewrittenCondition = _instrumenter.InstrumentIfStatementCondition(node, rewrittenCondition, _factory);
            }

            var result = RewriteIfStatement(syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.HasErrors);

            // add sequence point before the whole statement
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = _instrumenter.InstrumentIfStatement(node, result);
            }

            return result;
        }

        private static BoundStatement RewriteIfStatement(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement rewrittenAlternativeOpt,
            bool hasErrors)
        {
            var afterif = new GeneratedLabelSymbol("afterif");
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            if (rewrittenAlternativeOpt == null)
            {
                // if (condition) 
                //   consequence;  
                //
                // becomes
                //
                // GotoIfFalse condition afterif;
                // consequence;
                // afterif:

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, afterif));
                builder.Add(rewrittenConsequence);
                builder.Add(new BoundSequencePoint(null, null));
                builder.Add(new BoundLabelStatement(syntax, afterif));
                var statements = builder.ToImmutableAndFree();
                return new BoundStatementList(syntax, statements, hasErrors);
            }
            else
            {
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

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, alt));
                builder.Add(rewrittenConsequence);
                builder.Add(new BoundGotoStatement(syntax, afterif));
                builder.Add(new BoundLabelStatement(syntax, alt));
                builder.Add(rewrittenAlternativeOpt);
                builder.Add(new BoundSequencePoint(null, null));
                builder.Add(new BoundLabelStatement(syntax, afterif));
                return new BoundStatementList(syntax, builder.ToImmutableAndFree(), hasErrors);
            }

        }

        /// <summary>
        /// Tries to replace if statement with conditional operator to reuse conditional operator optimizations.
        /// In particular:
        ///
        /// if (condition)
        ///     return X;
        /// else
        ///     return Y;
        ///
        /// becomes
        ///
        /// return condition ? X : Y;
        /// </summary>
        private static bool TryEliminateIfStatement(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement rewrittenAlternativeOpt,
            out BoundStatement result)
        {
            bool TryGetReturnStatement(BoundStatement statement, out BoundReturnStatement returnStatementResult)
            {
                if (statement is BoundReturnStatement returnStatement)
                {
                    returnStatementResult = returnStatement;
                    return true;
                }

                if (statement is BoundSequencePoint sequencePoint)
                {
                    return TryGetReturnStatement(sequencePoint.StatementOpt, out returnStatementResult);
                }

                returnStatementResult = default;
                return false;
            }

            if (syntax.HasErrors)
            {
                result = default;
                return false;
            }

            if (rewrittenAlternativeOpt == null)
            {
                result = default;
                return false;
            }

            if (TryGetReturnStatement(rewrittenConsequence, out var consequenceReturn) &&
                TryGetReturnStatement(rewrittenAlternativeOpt, out var alternativeReturn))
            {
                var consequenceExpression = consequenceReturn.ExpressionOpt;
                if (rewrittenConsequence is BoundSequencePoint)
                {
                    consequenceExpression = new BoundSequencePointExpression(
                        consequenceExpression.Syntax,
                        consequenceExpression,
                        consequenceExpression.Type,
                        consequenceExpression.HasErrors);
                }

                var alternativeExpression = alternativeReturn.ExpressionOpt;
                if (rewrittenAlternativeOpt is BoundSequencePoint)
                {
                    alternativeExpression= new BoundSequencePointExpression(
                        alternativeExpression.Syntax,
                        alternativeExpression,
                        alternativeExpression.Type,
                        alternativeExpression.HasErrors);
                }

                Debug.Assert(TypeSymbol.Equals(consequenceExpression.Type, alternativeExpression.Type, TypeCompareKind.ConsiderEverything));
                var resultType = consequenceExpression.Type;

                var conditionalOperator = new BoundConditionalOperator(
                    syntax,
                    false,
                    rewrittenCondition,
                    consequenceExpression,
                    alternativeExpression,
                    null,
                    resultType);

                result = new BoundReturnStatement(syntax, RefKind.None, conditionalOperator);
                return true;
            }

            result = default;
            return false;
        }
    }
}
