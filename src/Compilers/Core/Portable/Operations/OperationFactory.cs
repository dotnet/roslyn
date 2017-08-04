// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal static class OperationFactory
    {
        public static IVariableDeclaration CreateVariableDeclaration(ILocalSymbol variable, IOperation initialValue, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return CreateVariableDeclaration(ImmutableArray.Create(variable), initialValue, semanticModel, syntax);
        }

        public static VariableDeclaration CreateVariableDeclaration(ImmutableArray<ILocalSymbol> variables, IOperation initialValue, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return new VariableDeclaration(
                variables,
                initialValue,
                semanticModel,
                syntax,
                type: null,
                constantValue: default(Optional<object>));
        }

        public static IConditionalChoiceExpression CreateConditionalChoiceExpression(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return new ConditionalChoiceExpression(
                condition,
                ifTrue,
                ifFalse,
                semanticModel,
                syntax,
                resultType,
                default(Optional<object>));
        }

        public static IExpressionStatement CreateSimpleAssignmentExpressionStatement(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax)
        {
            var expression = new SimpleAssignmentExpression(target, value, semanticModel, syntax, target.Type, default(Optional<object>));
            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static IExpressionStatement CreateCompoundAssignmentExpressionStatement(
            IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, bool isLifted, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax)
        {
            var expression = new CompoundAssignmentExpression(
                     binaryOperationKind,
                     isLifted,
                     target,
                     value,
                     operatorMethod != null,
                     operatorMethod,
                     semanticModel,
                     syntax,
                     target.Type,
                     default(Optional<object>));

            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static ILiteralExpression CreateLiteralExpression(long value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return new LiteralExpression(value.ToString(), semanticModel, syntax, resultType, constantValue: new Optional<object>(value));
        }

        public static ILiteralExpression CreateLiteralExpression(ConstantValue value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return new LiteralExpression(value.GetValueToDisplay(), semanticModel, syntax, resultType, new Optional<object>(value.Value));
        }

        public static IBinaryOperatorExpression CreateBinaryOperatorExpression(
            BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isLifted)
        {
            return new BinaryOperatorExpression(
                binaryOperationKind, left, right,
                isLifted: isLifted, usesOperatorMethod: false, operatorMethod: null,
                semanticModel: semanticModel, syntax: syntax, type: resultType, constantValue: default);
        }

        public static IArrayCreationExpression CreateArrayCreationExpression(
            IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax)
        {
            var initializer = new ArrayInitializer(elementValues, semanticModel, syntax, arrayType, default(Optional<object>));
            return new ArrayCreationExpression(
                arrayType.ElementType,
                ImmutableArray.Create<IOperation>(CreateLiteralExpression(elementValues.Count(), resultType: null, semanticModel: semanticModel, syntax: syntax)),
                initializer,
                semanticModel,
                syntax,
                arrayType,
                default(Optional<object>));
        }

        public static IInvalidExpression CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax)
        {
            return CreateInvalidExpression(semanticModel, syntax, ImmutableArray<IOperation>.Empty);
        }

        public static IInvalidExpression CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children)
        {
            return new InvalidExpression(children, semanticModel, syntax, type: null, constantValue: default(Optional<object>));
        }
    }
}
