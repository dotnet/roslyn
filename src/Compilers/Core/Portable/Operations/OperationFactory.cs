﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal static class OperationFactory
    {
        public static IVariableDeclaration CreateVariableDeclaration(ILocalSymbol variable, IOperation initialValue, SyntaxNode syntax)
        {
            return CreateVariableDeclaration(ImmutableArray.Create(variable), initialValue, syntax);
        }

        public static VariableDeclaration CreateVariableDeclaration(ImmutableArray<ILocalSymbol> variables, IOperation initialValue, SyntaxNode syntax)
        {
            return new VariableDeclaration(
                variables,
                initialValue,
                syntax,
                type: null,
                constantValue: default(Optional<object>));
        }

        public static IConditionalChoiceExpression CreateConditionalChoiceExpression(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SyntaxNode syntax)
        {
            return new ConditionalChoiceExpression(
                condition,
                ifTrue,
                ifFalse,
                syntax,
                resultType,
                default(Optional<object>));
        }

        public static IExpressionStatement CreateSimpleAssignmentExpressionStatement(IOperation target, IOperation value, SyntaxNode syntax)
        {
            var expression = new SimpleAssignmentExpression(target, value, syntax, target.Type, default(Optional<object>));
            return new ExpressionStatement(expression, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static IExpressionStatement CreateCompoundAssignmentExpressionStatement(
            IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, bool isLifted, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            var expression = new CompoundAssignmentExpression(
                     binaryOperationKind,
                     isLifted,
                     target,
                     value,
                     operatorMethod != null,
                     operatorMethod,
                     syntax,
                     target.Type,
                     default(Optional<object>));

            return new ExpressionStatement(expression, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static ILiteralExpression CreateLiteralExpression(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            return new LiteralExpression(value.ToString(), syntax: syntax, type: resultType, constantValue: new Optional<object>(value));
        }

        public static ILiteralExpression CreateLiteralExpression(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            return new LiteralExpression(value.GetValueToDisplay(), syntax, resultType, new Optional<object>(value.Value));
        }

        public static IBinaryOperatorExpression CreateBinaryOperatorExpression(
            BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SyntaxNode syntax, bool isLifted)
        {
            return new BinaryOperatorExpression(
                binaryOperationKind, left, right,
                usesOperatorMethod: false, operatorMethod: null,
                syntax: syntax, type: resultType, constantValue: default, isLifted: isLifted);
        }

        public static IArrayCreationExpression CreateArrayCreationExpression(
            IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SyntaxNode syntax)
        {
            var initializer = new ArrayInitializer(elementValues, syntax, arrayType, default(Optional<object>));
            return new ArrayCreationExpression(
                arrayType.ElementType,
                ImmutableArray.Create<IOperation>(CreateLiteralExpression(elementValues.Count(), resultType: null, syntax: syntax)),
                initializer,
                syntax,
                arrayType,
                default(Optional<object>));
        }

        public static IInvalidExpression CreateInvalidExpression(SyntaxNode syntax)
        {
            return CreateInvalidExpression(syntax, ImmutableArray<IOperation>.Empty);
        }

        public static IInvalidExpression CreateInvalidExpression(SyntaxNode syntax, ImmutableArray<IOperation> children)
        {
            return new InvalidExpression(children: children, syntax: syntax, type: null, constantValue: default(Optional<object>));
        }
    }
}
