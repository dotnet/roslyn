// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                variables.IsDefaultOrEmpty || (initialValue != null && initialValue.IsInvalid),
                syntax,
                type: null,
                constantValue: default(Optional<object>));
        }

        public static IConditionalChoiceExpression CreateConditionalChoiceExpression(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SyntaxNode syntax)
        {
            var isInvalid = (condition == null || condition.IsInvalid || ifTrue == null || ifTrue.IsInvalid || ifFalse == null || ifFalse.IsInvalid || resultType == null);

            return new ConditionalChoiceExpression(
                condition,
                ifTrue,
                ifFalse,
                isInvalid,
                syntax,
                resultType,
                default(Optional<object>));
        }

        public static IExpressionStatement CreateAssignmentExpressionStatement(IOperation target, IOperation value, SyntaxNode syntax)
        {
            var isInvalid = target == null || target.IsInvalid || value == null || value.IsInvalid;
            var expression = new AssignmentExpression(target, value, isInvalid, syntax, target.Type, default(Optional<object>));
            return new ExpressionStatement(expression, expression.IsInvalid, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static IExpressionStatement CreateCompoundAssignmentExpressionStatement(
            IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            var isInvalid = target == null || target.IsInvalid || value == null || value.IsInvalid;
            var expression = new CompoundAssignmentExpression(
                     binaryOperationKind,
                     target,
                     value,
                     operatorMethod != null,
                     operatorMethod,
                     isInvalid,
                     syntax,
                     target.Type,
                     default(Optional<object>));

            return new ExpressionStatement(expression, expression.IsInvalid, syntax, type: null, constantValue: default(Optional<object>));
        }

        public static ILiteralExpression CreateLiteralExpression(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            return new LiteralExpression(value.ToString(), isInvalid: false, syntax: syntax, type: resultType, constantValue: new Optional<object>(value));
        }

        public static ILiteralExpression CreateLiteralExpression(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            return new LiteralExpression(value.GetValueToDisplay(), value.IsBad, syntax, resultType, new Optional<object>(value.Value));
        }

        public static IBinaryOperatorExpression CreateBinaryOperatorExpression(
            BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SyntaxNode syntax)
        {
            var isInvalid = left == null || left.IsInvalid || right == null || right.IsInvalid || binaryOperationKind == BinaryOperationKind.Invalid || resultType == null;
            return new BinaryOperatorExpression(
                binaryOperationKind, left, right,
                usesOperatorMethod: false, operatorMethod: null,
                isInvalid: isInvalid, syntax: syntax, type: resultType, constantValue: default(Optional<object>));
        }

        public static IArrayCreationExpression CreateArrayCreationExpression(
            IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SyntaxNode syntax)
        {
            var initializer = new ArrayInitializer(elementValues, elementValues.Any(v => v.IsInvalid), syntax, arrayType, default(Optional<object>));
            return new ArrayCreationExpression(
                arrayType.ElementType,
                ImmutableArray.Create<IOperation>(CreateLiteralExpression(elementValues.Count(), resultType: null, syntax: syntax)),
                initializer,
                initializer.IsInvalid,
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
            return new InvalidExpression(children: children, isInvalid: true, syntax: syntax, type: null, constantValue: default(Optional<object>));
        }
    }
}