// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string LongLength = "LongLength";

        private static readonly Dictionary<BinaryOperatorKind, BinaryOperatorKind> s_negatedBinaryMap =
            new Dictionary<BinaryOperatorKind, BinaryOperatorKind>
        {
            {BinaryOperatorKind.Equals, BinaryOperatorKind.NotEquals},
            {BinaryOperatorKind.NotEquals, BinaryOperatorKind.Equals},
            {BinaryOperatorKind.LessThan, BinaryOperatorKind.GreaterThanOrEqual},
            {BinaryOperatorKind.GreaterThan, BinaryOperatorKind.LessThanOrEqual},
            {BinaryOperatorKind.LessThanOrEqual, BinaryOperatorKind.GreaterThan},
            {BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThan},
            {BinaryOperatorKind.Or, BinaryOperatorKind.And},
            {BinaryOperatorKind.And, BinaryOperatorKind.Or},
            {BinaryOperatorKind.ConditionalOr, BinaryOperatorKind.ConditionalAnd},
            {BinaryOperatorKind.ConditionalAnd, BinaryOperatorKind.ConditionalOr},
        };

        protected abstract SyntaxNode GetIfStatement(TextSpan textSpan, SyntaxToken token, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetRootWithInvertIfStatement(Document document, SemanticModel model, SyntaxNode ifStatement, CancellationToken cancellationToken);
        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            var ifStatement = GetIfStatement(textSpan, token, cancellationToken);
            if (ifStatement == null)
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetInvertIfText(),
                    c => InvertIfAsync(document, ifStatement, c)));
        }

        internal abstract string GetInvertIfText();

        private async Task<Document> InvertIfAsync(
            Document document, 
            SyntaxNode ifStatement, 
            CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(
                // this returns the root because VB requires changing context around if statement
                GetRootWithInvertIfStatement(
                    document, model, ifStatement, cancellationToken));
        }

        private SyntaxNode GetNegationOfBinaryExpression(
            SyntaxNode expressionNode,
            SyntaxGenerator generator,
            ISyntaxFactsService syntaxFacts,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            syntaxFacts.GetPartsOfBinaryExpression(expressionNode, out var leftOperand, out var rightOperand);
            var operatorToken = syntaxFacts.GetOperatorTokenOfBinaryExpression(expressionNode);

            var operation = GetBinaryOperation(expressionNode, semanticModel);
            var binaryOperation = (IBinaryOperation)operation;

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

                //Workaround for issue
                if (binaryOperation.OperatorKind == BinaryOperatorKind.Or && IsConditionalOr(binaryOperation))
                {
                    negatedKind = BinaryOperatorKind.ConditionalAnd;
                }
                else if (binaryOperation.OperatorKind == BinaryOperatorKind.And && IsConditionalAnd(binaryOperation))
                {
                    negatedKind = BinaryOperatorKind.ConditionalOr;
                }

                SyntaxNode newLeftOperand = null;
                SyntaxNode newRightOperand = null;
                if (negateOperands)
                {
                    newLeftOperand = Negate(leftOperand, generator, syntaxFacts, semanticModel, cancellationToken);
                    newRightOperand = Negate(rightOperand, generator, syntaxFacts, semanticModel, cancellationToken);
                }
                else
                {
                    newLeftOperand = leftOperand;
                    newRightOperand = rightOperand;
                }

                var newBinaryOperation = NewBinaryOperation(binaryOperation, newLeftOperand, negatedKind, newRightOperand, generator, syntaxFacts, cancellationToken);
                return newBinaryOperation;
            }
        }

        internal abstract bool IsConditionalAnd(IBinaryOperation binaryOperation);
        internal abstract bool IsConditionalOr(IBinaryOperation binaryOperation);

        private SyntaxNode NewBinaryOperation(IBinaryOperation binaryOperation, SyntaxNode leftOperand, BinaryOperatorKind operationKind, SyntaxNode rightOperand, SyntaxGenerator generator, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            switch (operationKind)
            {
                case BinaryOperatorKind.Equals:
                    return generator.ValueEqualsExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.NotEquals:
                    return generator.ValueNotEqualsExpression(leftOperand, rightOperand);
                case BinaryOperatorKind.LessThanOrEqual:
                    if (IsSpecialCaseBinaryExpression(binaryOperation, operationKind, cancellationToken))
                    {
                        return generator.ValueEqualsExpression(leftOperand, rightOperand);
                    }
                    else
                    {
                        return generator.LessThanOrEqualExpression(leftOperand, rightOperand);
                    }
                case BinaryOperatorKind.GreaterThanOrEqual:
                    if (IsSpecialCaseBinaryExpression(binaryOperation, operationKind, cancellationToken))
                    {
                        return generator.ValueEqualsExpression(leftOperand, rightOperand);
                    }
                    else
                    {
                        return generator.GreaterThanOrEqualExpression(leftOperand, rightOperand);
                    }
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

        internal abstract IOperation GetBinaryOperation(SyntaxNode expressionNode, SemanticModel semanticModel);

        protected SyntaxNode Negate(SyntaxNode expression, SyntaxGenerator generator, ISyntaxFactsService syntaxFacts, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (syntaxFacts.IsParenthesizedExpression(expression))
            {
                //TO DO:  This is not returning a Parenthesized Expression.  How to get parenthesized expression from generator?
                return syntaxFacts.Parenthesize(Negate(syntaxFacts.GetExpressionOfParenthesizedExpression(expression), generator, syntaxFacts, semanticModel, cancellationToken));
            }
            if (syntaxFacts.IsBinaryExpression(expression))
            {
                return GetNegationOfBinaryExpression(expression, generator, syntaxFacts, semanticModel, cancellationToken);
            }
            else if (syntaxFacts.IsLiteralExpression(expression))
            {
                return GetNegationOfLiteralExpression(expression, generator, semanticModel);
            }
            else if (syntaxFacts.IsLogicalNotExpression(expression))
            {
                return (GetNegationOfLogicalNotExpression(expression, generator, syntaxFacts, semanticModel));
            }
            return generator.LogicalNotExpression(expression);
        }

        private SyntaxNode GetNegationOfLogicalNotExpression(SyntaxNode expression, SyntaxGenerator generator, ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
        {
            var operatorToken = syntaxFacts.GetOperatorTokenOfPrefixUnaryExpression(expression);
            var operand = syntaxFacts.GetOperandOfPrefixUnaryExpression(expression);

            return operand.WithPrependedLeadingTrivia(operatorToken.LeadingTrivia).WithPrependedLeadingTrivia(operatorToken.TrailingTrivia);
        }

        private SyntaxNode GetNegationOfLiteralExpression(SyntaxNode expression, SyntaxGenerator generator, SemanticModel semanticModel)
        {
            var operation = semanticModel.GetOperation(expression);
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is bool value)
            {
                if (value == true)
                {
                    return generator.FalseLiteralExpression();
                }
                else
                {
                    return generator.TrueLiteralExpression();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the binaryExpression consists of an expression that can never be negative, 
        /// such as length or unsigned numeric types, being compared to zero with greater than, 
        /// less than, or equals relational operator.
        /// </summary>
        public bool IsSpecialCaseBinaryExpression(
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
                case BinaryOperatorKind.LessThanOrEqual when IsNumericLiteral(rightOperand):
                    return CanSimplifyToLengthEqualsZeroExpression(
                        leftOperand,
                        (ILiteralOperation)rightOperand,
                        cancellationToken);
                case BinaryOperatorKind.GreaterThanOrEqual when IsNumericLiteral(leftOperand):
                    return CanSimplifyToLengthEqualsZeroExpression(
                        rightOperand,
                        (ILiteralOperation)leftOperand,
                        cancellationToken);
            }

            return false;
        }

        private bool IsNumericLiteral(IOperation operation)
            => operation.Kind == OperationKind.Literal && operation.Type.IsNumericType();

        private IOperation RemoveImplicitConversion(IOperation operation)
        {
            if (operation is IConversionOperation conversion && conversion.IsImplicit)
            {
                return RemoveImplicitConversion(conversion.Operand);
            }

            return operation;
        }

        private bool CanSimplifyToLengthEqualsZeroExpression(
            IOperation variableExpression,
            ILiteralOperation numericLiteralExpression,
            CancellationToken cancellationToken)
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
                        if (containingType != null &&
                            (containingType.SpecialType == SpecialType.System_Array ||
                            containingType.SpecialType == SpecialType.System_String))
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

        internal SyntaxNode GetNegationOfParenthesizedExpression(SyntaxNode expression, SyntaxGenerator generator, ISyntaxFactsService syntaxFacts, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            //var parenthesizedExpression = (ParenthesizedExpressionSyntax)expression;
            //return parenthesizedExpression
            //    .WithExpression(Negate(parenthesizedExpression.Expression, semanticModel, cancellationToken))
            //    .WithAdditionalAnnotations(Simplifier.Annotation);
            return generator.ExpressionStatement(Negate(syntaxFacts.GetExpressionOfParenthesizedExpression(expression), generator, syntaxFacts, semanticModel, cancellationToken));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
}
}
