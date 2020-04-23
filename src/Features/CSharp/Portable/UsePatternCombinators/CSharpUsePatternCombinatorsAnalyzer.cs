// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    using static BinaryOperatorKind;
    using static AnalyzedPattern;

    internal sealed class CSharpUsePatternCombinatorsAnalyzer
    {
        private ExpressionSyntax _targetExpression = null!;

        public static AnalyzedPattern? Analyze(IOperation operation, out ExpressionSyntax targetExpression)
        {
            var patternAnalyzer = new CSharpUsePatternCombinatorsAnalyzer();
            var analyzedPattern = patternAnalyzer.ParsePattern(operation);
            targetExpression = patternAnalyzer._targetExpression;
            return analyzedPattern;
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

        private AnalyzedPattern? ParsePattern(IOperation operation)
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

                case IIsTypeOperation op when CheckTargetExpression(op.ValueOperand) &&
                                              op.Syntax is BinaryExpressionSyntax { Right: TypeSyntax type }:
                    return new Type(type);

                case IIsPatternOperation op when CheckTargetExpression(op.Value) &&
                                                 op.Pattern.Syntax is PatternSyntax pattern:
                    return new Source(pattern);

                case IParenthesizedOperation op:
                    return ParsePattern(op.Operand);
            }

            return null;
        }

        private AnalyzedPattern? ParseBinaryPattern(IBinaryOperation op, bool isDisjunctive, SyntaxToken token)
        {
            var leftPattern = ParsePattern(op.LeftOperand);
            if (leftPattern == null)
                return null;

            var rightPattern = ParsePattern(op.RightOperand);
            if (rightPattern == null)
                return null;

            return Binary.Create(leftPattern, rightPattern, isDisjunctive, token);

        }

        private ConstantResult DetermineConstant(IBinaryOperation op)
        {
            return (op.LeftOperand, op.RightOperand) switch
            {
                var (e, v) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Right,
                var (v, e) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Left,
                _ => ConstantResult.None,
            };
        }

        private AnalyzedPattern? ParseRelationalPattern(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                ConstantResult.Left when op.LeftOperand.Syntax is ExpressionSyntax left
                    => new Relational(Flip(op.OperatorKind), left),
                ConstantResult.Right when op.RightOperand.Syntax is ExpressionSyntax right
                    => new Relational(op.OperatorKind, right),
                _ => null
            };
        }

        private AnalyzedPattern? ParseConstantPattern(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                ConstantResult.Left when op.LeftOperand.Syntax is ExpressionSyntax left
                    => new Constant(left),
                ConstantResult.Right when op.RightOperand.Syntax is ExpressionSyntax right
                    => new Constant(right),
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
        /// Changes the direction the operator is pointing
        /// </summary>
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
            // Constants do not propagate to conversions
            return operation is IConversionOperation op
                ? IsConstant(op.Operand)
                : operation.ConstantValue.HasValue;
        }

        private bool CheckTargetExpression(IOperation operation)
        {
            if (operation is IConversionOperation { IsImplicit: false } op)
            {
                // Unwrap explicit casts because the pattern will emit those anyways
                operation = op.Operand;
            }

            if (!(operation.Syntax is ExpressionSyntax expression))
            {
                return false;
            }

            // If we have not figured the target expression yet,
            // we will assume that the first expression is the one.
            if (_targetExpression is null)
            {
                _targetExpression = expression;
                return true;
            }

            return SyntaxFactory.AreEquivalent(expression, _targetExpression);
        }
    }
}

