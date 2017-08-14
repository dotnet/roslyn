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
                constantValue: default(Optional<object>),
                isImplicit: false); // variable declaration is always explicit
        }

        public static IConditionalChoiceExpression CreateConditionalChoiceExpression(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new ConditionalChoiceExpression(
                condition,
                ifTrue,
                ifFalse,
                semanticModel,
                syntax,
                resultType,
                default(Optional<object>),
                isImplicit);
        }

        public static IExpressionStatement CreateSimpleAssignmentExpressionStatement(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var expression = new SimpleAssignmentExpression(target, value, semanticModel, syntax, target.Type, default(Optional<object>), isImplicit);
            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }

        public static IExpressionStatement CreateCompoundAssignmentExpressionStatement(
            IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, bool isLifted, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
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
                     default(Optional<object>),
                     isImplicit);

            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }

        public static ILiteralExpression CreateLiteralExpression(long value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new LiteralExpression(semanticModel, syntax, resultType, constantValue: new Optional<object>(value), isImplicit: isImplicit);
        }

        public static ILiteralExpression CreateLiteralExpression(ConstantValue value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new LiteralExpression(semanticModel, syntax, resultType, new Optional<object>(value.Value), isImplicit);
        }

        public static IBinaryOperatorExpression CreateBinaryOperatorExpression(
            BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isLifted, bool isImplicit)
        {
            return new BinaryOperatorExpression(
                binaryOperationKind, left, right,
                isLifted: isLifted, usesOperatorMethod: false, operatorMethod: null,
                semanticModel: semanticModel, syntax: syntax, type: resultType, constantValue: default, isImplicit: isImplicit);
        }

        public static IArrayCreationExpression CreateArrayCreationExpression(
            IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var initializer = new ArrayInitializer(elementValues, semanticModel, syntax, arrayType, default(Optional<object>), isImplicit);
            return new ArrayCreationExpression(
                arrayType.ElementType,
                ImmutableArray.Create<IOperation>(CreateLiteralExpression(elementValues.Count(), resultType: null, semanticModel: semanticModel, syntax: syntax, isImplicit: isImplicit)),
                initializer,
                semanticModel,
                syntax,
                arrayType,
                default(Optional<object>),
                isImplicit);
        }

        public static IInvalidExpression CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return CreateInvalidExpression(semanticModel, syntax, ImmutableArray<IOperation>.Empty, isImplicit);
        }

        public static IInvalidExpression CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children, bool isImplicit)
        {
            return new InvalidExpression(children, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }
    }
}
