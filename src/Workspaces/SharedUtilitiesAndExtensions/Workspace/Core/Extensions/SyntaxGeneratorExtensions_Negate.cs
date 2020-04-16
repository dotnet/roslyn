// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Shared.Extensions;
#endif

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxGeneratorExtensions
    {
        private const string LongLength = "LongLength";

        private static readonly Dictionary<BinaryOperatorKind, BinaryOperatorKind> s_negatedBinaryMap =
            new Dictionary<BinaryOperatorKind, BinaryOperatorKind>
        {
            { BinaryOperatorKind.Equals, BinaryOperatorKind.NotEquals },
            { BinaryOperatorKind.NotEquals, BinaryOperatorKind.Equals },
            { BinaryOperatorKind.LessThan, BinaryOperatorKind.GreaterThanOrEqual },
            { BinaryOperatorKind.GreaterThan, BinaryOperatorKind.LessThanOrEqual },
            { BinaryOperatorKind.LessThanOrEqual, BinaryOperatorKind.GreaterThan },
            { BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThan },
            { BinaryOperatorKind.Or, BinaryOperatorKind.And },
            { BinaryOperatorKind.And, BinaryOperatorKind.Or },
            { BinaryOperatorKind.ConditionalOr, BinaryOperatorKind.ConditionalAnd },
            { BinaryOperatorKind.ConditionalAnd, BinaryOperatorKind.ConditionalOr },
        };

        public static SyntaxNode Negate(
            this SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            SyntaxNode expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            return Negate(generator, generatorInternal, expression, semanticModel, negateBinary: true, cancellationToken);
        }

        public static SyntaxNode Negate(
            this SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            SyntaxNode expressionOrPattern,
            SemanticModel semanticModel,
            bool negateBinary,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = generatorInternal.SyntaxFacts;
            if (syntaxFacts.IsParenthesizedExpression(expressionOrPattern))
            {
                return generatorInternal.AddParentheses(
                    generator.Negate(
                        generatorInternal,
                        syntaxFacts.GetExpressionOfParenthesizedExpression(expressionOrPattern),
                        semanticModel,
                        negateBinary,
                        cancellationToken))
                    .WithTriviaFrom(expressionOrPattern);
            }

            if (negateBinary && syntaxFacts.IsBinaryExpression(expressionOrPattern))
                return GetNegationOfBinaryExpression(expressionOrPattern, generator, generatorInternal, semanticModel, cancellationToken);

            if (negateBinary && syntaxFacts.IsIsPatternExpression(expressionOrPattern))
                return GetNegationOfIsPatternExpression(expressionOrPattern, generator, generatorInternal, semanticModel, cancellationToken);

            if (syntaxFacts.IsLiteralExpression(expressionOrPattern))
                return GetNegationOfLiteralExpression(expressionOrPattern, generator, semanticModel);

            if (syntaxFacts.IsLogicalNotExpression(expressionOrPattern))
                return GetNegationOfLogicalNotExpression(expressionOrPattern, syntaxFacts);

#if CODE_STYLE
            return generator.LogicalNotExpression(expressionOrPattern);
#else

            if (syntaxFacts.IsParenthesizedPattern(expressionOrPattern))
            {
                return generatorInternal.AddParentheses(
                    generator.Negate(
                        generatorInternal,
                        syntaxFacts.GetPatternOfParenthesizedPattern(expressionOrPattern),
                        semanticModel,
                        negateBinary,
                        cancellationToken))
                    .WithTriviaFrom(expressionOrPattern);
            }

            if (negateBinary && syntaxFacts.IsBinaryPattern(expressionOrPattern))
                return GetNegationOfBinaryPattern(expressionOrPattern, generator, generatorInternal, semanticModel, cancellationToken);

            if (syntaxFacts.IsConstantPattern(expressionOrPattern))
                return GetNegationOfConstantPattern(expressionOrPattern, generator, generatorInternal, semanticModel, cancellationToken);

            if (syntaxFacts.IsUnaryPattern(expressionOrPattern))
                return GetNegationOfUnaryPattern(expressionOrPattern, generator, syntaxFacts);

            return syntaxFacts.IsAnyPattern(expressionOrPattern)
                ? generator.NotPattern(expressionOrPattern)
                : generator.LogicalNotExpression(expressionOrPattern);

#endif

        }

        private static SyntaxNode GetNegationOfBinaryExpression(
            SyntaxNode expressionNode,
            SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = generatorInternal.SyntaxFacts;
            syntaxFacts.GetPartsOfBinaryExpression(expressionNode, out var leftOperand, out var operatorToken, out var rightOperand);

            var binaryOperation = semanticModel.GetOperation(expressionNode, cancellationToken) as IBinaryOperation;
            if (binaryOperation == null)
            {
                // Apply the logical not operator if it is not a binary operation.
                return generator.LogicalNotExpression(expressionNode);
            }

            if (!s_negatedBinaryMap.TryGetValue(binaryOperation.OperatorKind, out var negatedKind))
            {
                return generator.LogicalNotExpression(expressionNode);
            }
            else
            {
                var negateOperands = false;
                switch (binaryOperation.OperatorKind)
                {
                    case BinaryOperatorKind.Or:
                    case BinaryOperatorKind.And:
                    case BinaryOperatorKind.ConditionalAnd:
                    case BinaryOperatorKind.ConditionalOr:
                        negateOperands = true;
                        break;
                }

                //Workaround for https://github.com/dotnet/roslyn/issues/23956
                //Issue to remove this when above is merged
                if (binaryOperation.OperatorKind == BinaryOperatorKind.Or && syntaxFacts.IsLogicalOrExpression(expressionNode))
                {
                    negatedKind = BinaryOperatorKind.ConditionalAnd;
                }
                else if (binaryOperation.OperatorKind == BinaryOperatorKind.And && syntaxFacts.IsLogicalAndExpression(expressionNode))
                {
                    negatedKind = BinaryOperatorKind.ConditionalOr;
                }

                var newLeftOperand = leftOperand;
                var newRightOperand = rightOperand;
                if (negateOperands)
                {
                    newLeftOperand = generator.Negate(generatorInternal, leftOperand, semanticModel, cancellationToken);
                    newRightOperand = generator.Negate(generatorInternal, rightOperand, semanticModel, cancellationToken);
                }

                var newBinaryExpressionSyntax = NewBinaryOperation(binaryOperation, newLeftOperand, negatedKind, newRightOperand, generator, cancellationToken)
                    .WithTriviaFrom(expressionNode);

                var newToken = syntaxFacts.GetOperatorTokenOfBinaryExpression(newBinaryExpressionSyntax);
                var newTokenWithTrivia = newToken.WithTriviaFrom(operatorToken);
                return newBinaryExpressionSyntax.ReplaceToken(newToken, newTokenWithTrivia);
            }
        }

        private static SyntaxNode GetNegationOfBinaryPattern(
            SyntaxNode pattern,
            SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = generatorInternal.SyntaxFacts;
            syntaxFacts.GetPartsOfBinaryPattern(pattern, out var left, out var operatorToken, out var right);

            var newLeft = generator.Negate(generatorInternal, left, semanticModel, cancellationToken);
            var newRight = generator.Negate(generatorInternal, right, semanticModel, cancellationToken);

            var newPattern =
                syntaxFacts.IsAndPattern(pattern) ? generator.OrPattern(newLeft, newRight) :
                syntaxFacts.IsOrPattern(pattern) ? generator.AndPattern(newLeft, newRight) :
                throw ExceptionUtilities.UnexpectedValue(pattern.RawKind);

            newPattern = newPattern.WithTriviaFrom(pattern);

            syntaxFacts.GetPartsOfBinaryPattern(newPattern, out _, out var newToken, out _);
            var newTokenWithTrivia = newToken.WithTriviaFrom(operatorToken);
            return newPattern.ReplaceToken(newToken, newTokenWithTrivia);
        }

        private static SyntaxNode GetNegationOfIsPatternExpression(SyntaxNode isExpression, SyntaxGenerator generator, SyntaxGeneratorInternal generatorInternal, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Don't recurse into patterns if the language doesn't support negated patterns.
            // Just wrap with a normal '!' expression.
            var syntaxFacts = generatorInternal.SyntaxFacts;
            if (!syntaxFacts.SupportsNotPattern(semanticModel.SyntaxTree.Options))
                return generator.LogicalNotExpression(isExpression);

            syntaxFacts.GetPartsOfIsPatternExpression(isExpression, out var left, out var isToken, out var pattern);
            return generator.IsPatternExpression(
                left, isToken,
                generator.Negate(generatorInternal, pattern, semanticModel, cancellationToken));
        }

        private static SyntaxNode NewBinaryOperation(
            IBinaryOperation binaryOperation,
            SyntaxNode leftOperand,
            BinaryOperatorKind operationKind,
            SyntaxNode rightOperand,
            SyntaxGenerator generator,
            CancellationToken cancellationToken)
        {
            switch (operationKind)
            {
                case BinaryOperatorKind.Equals:
                    return binaryOperation.LeftOperand.Type?.IsValueType == true && binaryOperation.RightOperand.Type?.IsValueType == true
                        ? generator.ValueEqualsExpression(leftOperand, rightOperand)
                        : generator.ReferenceEqualsExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.NotEquals:
                    return binaryOperation.LeftOperand.Type?.IsValueType == true && binaryOperation.RightOperand.Type?.IsValueType == true
                        ? generator.ValueNotEqualsExpression(leftOperand, rightOperand)
                        : generator.ReferenceNotEqualsExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.LessThanOrEqual:
                    return IsSpecialCaseBinaryExpression(binaryOperation, operationKind, cancellationToken)
                        ? generator.ValueEqualsExpression(leftOperand, rightOperand)
                        : generator.LessThanOrEqualExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return IsSpecialCaseBinaryExpression(binaryOperation, operationKind, cancellationToken)
                        ? generator.ValueEqualsExpression(leftOperand, rightOperand)
                        : generator.GreaterThanOrEqualExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.LessThan:
                    return generator.LessThanExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.GreaterThan:
                    return generator.GreaterThanExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.Or:
                    return generator.BitwiseOrExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.And:
                    return generator.BitwiseAndExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.ConditionalOr:
                    return generator.LogicalOrExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.ConditionalAnd:
                    return generator.LogicalAndExpression(leftOperand, rightOperand);
            }

            return null;
        }

        /// <summary>
        /// Returns true if the binaryExpression consists of an expression that can never be negative, 
        /// such as length or unsigned numeric types, being compared to zero with greater than, 
        /// less than, or equals relational operator.
        /// </summary>
        public static bool IsSpecialCaseBinaryExpression(
            IBinaryOperation binaryOperation,
            BinaryOperatorKind operationKind,
            CancellationToken cancellationToken)
        {
            if (binaryOperation == null)
            {
                return false;
            }

            var rightOperand = RemoveImplicitConversion(binaryOperation.RightOperand);
            var leftOperand = RemoveImplicitConversion(binaryOperation.LeftOperand);

            switch (operationKind)
            {
                case BinaryOperatorKind.LessThanOrEqual when rightOperand.IsNumericLiteral():
                    return CanSimplifyToLengthEqualsZeroExpression(
                        leftOperand, (ILiteralOperation)rightOperand);
                case BinaryOperatorKind.GreaterThanOrEqual when leftOperand.IsNumericLiteral():
                    return CanSimplifyToLengthEqualsZeroExpression(
                        rightOperand, (ILiteralOperation)leftOperand);
            }

            return false;
        }

        private static IOperation RemoveImplicitConversion(IOperation operation)
        {
            return operation is IConversionOperation conversion && conversion.IsImplicit
                ? RemoveImplicitConversion(conversion.Operand)
                : operation;
        }

        private static bool CanSimplifyToLengthEqualsZeroExpression(
            IOperation variableExpression, ILiteralOperation numericLiteralExpression)
        {
            var numericValue = numericLiteralExpression.ConstantValue;
            if (numericValue.HasValue && numericValue.Value is 0)
            {
                if (variableExpression is IPropertyReferenceOperation propertyOperation)
                {
                    var property = propertyOperation.Property;
                    if ((property.Name == nameof(Array.Length) || property.Name == LongLength))
                    {
                        var containingType = property.ContainingType;
                        if (containingType?.SpecialType == SpecialType.System_Array ||
                            containingType.SpecialType == SpecialType.System_String)
                        {
                            return true;
                        }
                    }
                }

                var type = variableExpression.Type;
                if (type != null)
                {
                    switch (type.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            return true;
                    }
                }
            }

            return false;
        }

        private static SyntaxNode GetNegationOfLiteralExpression(
            SyntaxNode expression,
            SyntaxGenerator generator,
            SemanticModel semanticModel)
        {
            var operation = semanticModel.GetOperation(expression);
            SyntaxNode newLiteralExpression;

            if (operation?.Kind == OperationKind.Literal && operation.ConstantValue.HasValue && operation.ConstantValue.Value is true)
            {
                newLiteralExpression = generator.FalseLiteralExpression();
            }
            else if (operation?.Kind == OperationKind.Literal && operation.ConstantValue.HasValue && operation.ConstantValue.Value is false)
            {
                newLiteralExpression = generator.TrueLiteralExpression();
            }
            else
            {
                newLiteralExpression = generator.LogicalNotExpression(expression.WithoutTrivia());
            }

            return newLiteralExpression.WithTriviaFrom(expression);
        }

#if !CODE_STYLE

        private static SyntaxNode GetNegationOfConstantPattern(
            SyntaxNode pattern,
            SyntaxGenerator generator,
            SyntaxGeneratorInternal generatorInternal,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = generatorInternal.SyntaxFacts;

            var expression = syntaxFacts.GetExpressionOfConstantPattern(pattern);
            if (syntaxFacts.IsTrueLiteralExpression(expression))
                return generator.ConstantPattern(generator.FalseLiteralExpression());

            if (syntaxFacts.IsFalseLiteralExpression(expression))
                return generator.ConstantPattern(generator.TrueLiteralExpression());

            return generator.NotPattern(pattern);
        }

#endif

        private static SyntaxNode GetNegationOfLogicalNotExpression(
            SyntaxNode expression,
            ISyntaxFacts syntaxFacts)
        {
            var operatorToken = syntaxFacts.GetOperatorTokenOfPrefixUnaryExpression(expression);
            var operand = syntaxFacts.GetOperandOfPrefixUnaryExpression(expression);

            return operand.WithPrependedLeadingTrivia(operatorToken.LeadingTrivia)
                          .WithAdditionalAnnotations(Simplifier.Annotation);
        }

#if !CODE_STYLE

        private static SyntaxNode GetNegationOfUnaryPattern(
            SyntaxNode pattern,
            SyntaxGenerator generator,
            ISyntaxFacts syntaxFacts)
        {
            syntaxFacts.GetPartsOfUnaryPattern(pattern, out var opToken, out var subPattern);

            if (syntaxFacts.IsNotPattern(pattern))
            {
                return subPattern.WithPrependedLeadingTrivia(opToken.LeadingTrivia)
                                 .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            // TODO: add support for more unary patterns.  for example, `< 0` can be negated to `>= 0`

            return generator.NotPattern(pattern);
        }

#endif
    }
}
