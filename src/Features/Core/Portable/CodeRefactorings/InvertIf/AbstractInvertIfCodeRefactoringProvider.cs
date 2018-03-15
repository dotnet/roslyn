// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string LongLength = "LongLength";

        protected abstract SyntaxNode GetIfStatement(TextSpan textSpan, SyntaxToken token, CancellationToken cancellationToken);
        protected abstract Task<SyntaxNode> InvertIfStatementAsync(Document document, SemanticModel model, SyntaxNode ifStatement, CancellationToken cancellation);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            var ifStatement = GetIfStatement(textSpan, token, cancellationToken);
            if (ifStatement == null)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    FeaturesResources.Invert_if_statement,
                    c => InvertIfAsync(document, ifStatement, c)));
        }


        private async Task<Document> InvertIfAsync(Document document, SyntaxNode ifStatement, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newIfStatement = await InvertIfStatementAsync(document, model, ifStatement, cancellationToken).ConfigureAwait(false);

            var result = ifStatement.SyntaxTree.GetRoot().ReplaceNode(ifStatement, newIfStatement);
            return document.WithSyntaxRoot(result);
        }

        /// <summary>
        /// Returns true if the binaryExpression consists of an expression that can never be negative, 
        /// such as length or unsigned numeric types, being compared to zero with greater than, 
        /// less than, or equals relational operator.
        /// </summary>
        protected bool IsSpecialCaseBinaryExpression(IBinaryOperation binaryOperation, CancellationToken cancellationToken)
        {
            if (binaryOperation == null)
            {
                return false;
            }

            switch (binaryOperation.OperatorKind)
            {
                case BinaryOperatorKind.GreaterThan when IsNumericLiteral(binaryOperation.RightOperand):
                case BinaryOperatorKind.Equals when IsNumericLiteral(binaryOperation.RightOperand):
                    return CanSimplifyToLengthEqualsZeroExpression(
                        binaryOperation.LeftOperand,
                        (ILiteralOperation)binaryOperation.RightOperand,
                        cancellationToken);
                case BinaryOperatorKind.LessThan when IsNumericLiteral(binaryOperation.LeftOperand):
                case BinaryOperatorKind.Equals when IsNumericLiteral(binaryOperation.LeftOperand):
                    return CanSimplifyToLengthEqualsZeroExpression(
                        binaryOperation.RightOperand,
                        (ILiteralOperation)binaryOperation.LeftOperand,
                        cancellationToken);
            }

            return false;
        }

        private bool IsNumericLiteral(IOperation operation)
        {
            return operation.Kind == OperationKind.Literal && operation.Type.IsNumericType();
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
