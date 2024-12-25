// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;

using static BinaryOperatorKind;
using static AnalyzedPattern;

internal static class CSharpUsePatternCombinatorsAnalyzer
{
    public static AnalyzedPattern? Analyze(IOperation operation)
    {
        var pattern = ParsePattern(operation);
        return pattern?.Target.Syntax is ExpressionSyntax ? pattern : null;
    }

    private enum ConstantResult
    {
        /// <summary>
        /// None of operands were constant.
        /// </summary>
        None,

        /// <summary>
        /// The left operand is the constant.
        /// </summary>
        Left,

        /// <summary>
        /// The right operand is the constant.
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
                return Not.TryCreate(ParseConstantPattern(op));

            case IBinaryOperation { OperatorKind: ConditionalOr, Syntax: BinaryExpressionSyntax syntax } op:
                return ParseBinaryPattern(op, isDisjunctive: true, syntax.OperatorToken);

            case IBinaryOperation { OperatorKind: ConditionalAnd, Syntax: BinaryExpressionSyntax syntax } op:
                return ParseBinaryPattern(op, isDisjunctive: false, syntax.OperatorToken);

            case IBinaryOperation op when IsRelationalOperator(op.OperatorKind):
                return ParseRelationalPattern(op);

            case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } op:
                return Not.TryCreate(ParsePattern(op.Operand));

            case IIsTypeOperation { Syntax: BinaryExpressionSyntax binaryExpression } op:
                return Type.TryCreate(binaryExpression, op);

            case IIsPatternOperation { Pattern.Syntax: PatternSyntax pattern } op:
                return new Source(pattern, op.Value);

            case IParenthesizedOperation op:
                return ParsePattern(op.Operand);
        }

        return null;
    }

    private static AnalyzedPattern? ParseBinaryPattern(IBinaryOperation op, bool isDisjunctive, SyntaxToken token)
    {
        var leftPattern = ParsePattern(op.LeftOperand);
        if (leftPattern is null)
            return null;

        var rightPattern = ParsePattern(op.RightOperand);
        if (rightPattern is null)
            return null;

        return Binary.TryCreate(leftPattern, rightPattern, isDisjunctive, token);
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
                // We need to flip the operator if the constant is on the left-hand-side.
                // This is because relational patterns only come in the prefix form.
                // For instance: `123 > x` would be rewritten as `x is < 123`.
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
    /// Changes the direction the operator is pointing at.
    /// </summary>
    /// <remarks>
    /// Relational patterns only come in the prefix form so we'll have to
    /// flip the operator if the constant happens to be on the left-hand-side.
    /// For instance: `123 &gt; x` would be rewritten as `x is &lt; 123`.
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
        return operation is IConversionOperation { Conversion.IsUserDefined: false } op
            ? IsConstant(op.Operand)
            : operation.ConstantValue.HasValue;
    }
}
