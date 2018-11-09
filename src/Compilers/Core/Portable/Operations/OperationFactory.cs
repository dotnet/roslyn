// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal static class OperationFactory
    {
        public static IVariableInitializerOperation CreateVariableInitializer(SyntaxNode syntax, IOperation initializerValue, SemanticModel semanticModel, bool isImplicit)
        {
            return new VariableInitializer(initializerValue, semanticModel, syntax, type: null, constantValue: default, isImplicit: isImplicit);
        }

        public static IConditionalOperation CreateConditionalExpression(IOperation condition, IOperation whenTrue, IOperation whenFalse, bool isRef, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new ConditionalOperation(
                condition,
                whenTrue,
                whenFalse,
                isRef,
                semanticModel,
                syntax,
                resultType,
                default(Optional<object>),
                isImplicit);
        }

        public static IExpressionStatementOperation CreateSimpleAssignmentExpressionStatement(IOperation target, bool isRef, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var expression = new SimpleAssignmentExpression(target, isRef, value, semanticModel, syntax, target.Type, default(Optional<object>), isImplicit);
            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }

        public static ILiteralOperation CreateLiteralExpression(long value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new LiteralExpression(semanticModel, syntax, resultType, constantValue: new Optional<object>(value), isImplicit: isImplicit);
        }

        public static ILiteralOperation CreateLiteralExpression(ConstantValue value, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new LiteralExpression(semanticModel, syntax, resultType, new Optional<object>(value.Value), isImplicit);
        }

        public static IBinaryOperation CreateBinaryOperatorExpression(
            BinaryOperatorKind operatorKind, IOperation left, IOperation right, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isLifted, bool isChecked, bool isCompareText, bool isImplicit)
        {
            return new BinaryOperatorExpression(
                operatorKind, left, right,
                isLifted: isLifted, isChecked: isChecked,
                isCompareText: isCompareText, operatorMethod: null, unaryOperatorMethod: null,
                semanticModel: semanticModel, syntax: syntax, type: resultType, constantValue: default, isImplicit: isImplicit);
        }

        public static IInvalidOperation CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return CreateInvalidExpression(semanticModel, syntax, ImmutableArray<IOperation>.Empty, isImplicit);
        }

        public static IInvalidOperation CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children, bool isImplicit)
        {
            return new InvalidOperation(children, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }

        public static Lazy<IOperation> NullOperation { get; } = new Lazy<IOperation>(() => null);
        public static Lazy<IVariableInitializerOperation> NullInitializer { get; } = new Lazy<IVariableInitializerOperation>(() => null);
    }
}
