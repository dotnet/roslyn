// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    using static BinaryOperatorKind;
    using static AnalyzedPattern;

    internal static class CSharpUsePatternCombinatorsAnalyzer
    {
        public static bool Analyze(IOperation operation,
            [NotNullWhen(true)] out AnalyzedPattern? pattern,
            [NotNullWhen(true)] out ExpressionSyntax? target)
        {
            if ((pattern = ParsePattern(operation)) != null)
                return TryGetTargetExpression(pattern, out target);
            target = null;
            return false;
        }

        private enum ConstantResult
        {
            /// <summary>
            /// None of operands were constant.
            /// </summary>
            None,

            /// <summary>
            /// Signifies that the left operand is the constant.
            /// </summary>
            Left,

            /// <summary>
            /// Signifies that the right operand is the constant.
            /// </summary>
            Right,
        }

        private static AnalyzedPattern? ParsePattern(IOperation operation)
        {
            switch (operation)
            {
                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals } op:
                    return ParseConstantPattern(op);

                case IBinaryOperation { OperatorKind: NotEquals } op:
                    {
                        var pattern = ParseConstantPattern(op);
                        if (pattern == null)
                            break;
                        return Not.Create(pattern);
                    }

                case IBinaryOperation { OperatorKind: ConditionalOr, Syntax: BinaryExpressionSyntax syntax } op:
                    return ParseBinaryPattern(op, isDisjunctive: true, syntax.OperatorToken);

                case IBinaryOperation { OperatorKind: ConditionalAnd, Syntax: BinaryExpressionSyntax syntax } op:
                    return ParseBinaryPattern(op, isDisjunctive: false, syntax.OperatorToken);

                case IBinaryOperation op when IsRelationalOperator(op.OperatorKind):
                    return ParseRelationalPattern(op);

                case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } op:
                    {
                        var pattern = ParsePattern(op.Operand);
                        if (pattern == null)
                            break;
                        return Not.Create(pattern);
                    }

                case IIsTypeOperation { Syntax: BinaryExpressionSyntax { Right: TypeSyntax type } } op:
                    return new Type(type, op.ValueOperand);

                case IIsPatternOperation { Pattern: { Syntax: PatternSyntax pattern } } op:
                    return new Source(pattern, op.Value);

                case IParenthesizedOperation op:
                    return ParsePattern(op.Operand);
            }

            return null;
        }

        private static AnalyzedPattern? ParseBinaryPattern(IBinaryOperation op, bool isDisjunctive, SyntaxToken token)
        {
            var leftPattern = ParsePattern(op.LeftOperand);
            if (leftPattern == null)
                return null;

            var rightPattern = ParsePattern(op.RightOperand);
            if (rightPattern == null)
                return null;

            return Binary.Create(leftPattern, rightPattern, isDisjunctive, token);
        }

        private static ConstantResult DetermineConstant(IBinaryOperation op)
        {
            return (op.LeftOperand, op.RightOperand) switch
            {
                var (_, v) when IsConstant(v) => ConstantResult.Right,
                var (v, _) when IsConstant(v) => ConstantResult.Left,
                _ => ConstantResult.None,
            };
        }

        private static AnalyzedPattern? ParseRelationalPattern(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                ConstantResult.Left when op.LeftOperand.Syntax is ExpressionSyntax left
                    => new Relational(Flip(op.OperatorKind), left, op.RightOperand),
                ConstantResult.Right when op.RightOperand.Syntax is ExpressionSyntax right
                    => new Relational(op.OperatorKind, right, op.LeftOperand),
                _ => null
            };
        }

        private static AnalyzedPattern? ParseConstantPattern(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                ConstantResult.Left when op.LeftOperand.Syntax is ExpressionSyntax left
                    => new Constant(left, op.RightOperand),
                ConstantResult.Right when op.RightOperand.Syntax is ExpressionSyntax right
                    => new Constant(right, op.LeftOperand),
                _ => null
            };
        }

        private static bool IsRelationalOperator(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case LessThan:
                case LessThanOrEqual:
                case GreaterThanOrEqual:
                case GreaterThan:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Changes the direction the operator is pointing to.
        /// </summary>
        /// <remarks>
        /// Relational patterns only come in prefix form so we'll have to
        /// flip the operator if the original comparison has an LHS constant.
        /// </remarks>
        public static BinaryOperatorKind Flip(BinaryOperatorKind operatorKind)
        {
            return operatorKind switch
            {
                LessThan => GreaterThan,
                LessThanOrEqual => GreaterThanOrEqual,
                GreaterThanOrEqual => LessThanOrEqual,
                GreaterThan => LessThan,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };
        }

        private static bool IsConstant(IOperation operation)
        {
            // By-design, constants will not propagate to conversions.
            return operation is IConversionOperation op
                ? IsConstant(op.Operand)
                : operation.ConstantValue.HasValue;
        }

        private static bool TryGetTargetExpression(AnalyzedPattern pattern, out ExpressionSyntax? target)
        {
            target = null;
            return CheckTargetExpressions(pattern, ref target);

            static bool CheckTargetExpressions(AnalyzedPattern pattern, ref ExpressionSyntax? target)
            {
                return pattern switch
                {
                    Test p => CheckTargetExpression(p.TargetOperation, ref target),
                    Binary p => CheckTargetExpressions(p.Left, ref target) &&
                                CheckTargetExpressions(p.Right, ref target),
                    Not p => CheckTargetExpressions(p.Pattern, ref target),
                    var p => throw ExceptionUtilities.UnexpectedValue(p)
                };
            }

            static bool CheckTargetExpression(IOperation operation, ref ExpressionSyntax? target)
            {
                var expr = GetTargetExpression(operation);
                if (expr is null)
                    return false;

                if (target != null)
                    return SyntaxFactory.AreEquivalent(expr, target);

                target = expr;
                return true;
            }

            static ExpressionSyntax? GetTargetExpression(IOperation operation)
            {
                // Unwrap explicit casts because the pattern will emit those anyways
                if (operation is IConversionOperation { IsImplicit: false } op)
                    operation = op.Operand;

                return operation.Syntax as ExpressionSyntax;
            }
        }
    }
}

