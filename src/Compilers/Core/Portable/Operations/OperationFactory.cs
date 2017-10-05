﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public static IConditionalExpression CreateConditionalExpression(IOperation condition, IOperation whenTrue, IOperation whenFalse, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return new ConditionalExpression(
                condition,
                whenTrue,
                whenFalse,
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
            IOperation target, IOperation value, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var expression = new CompoundAssignmentExpression(
                     operatorKind,
                     isLifted,
                     isChecked,
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
            BinaryOperatorKind operatorKind, IOperation left, IOperation right, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isLifted, bool isChecked, bool isCompareText, bool isImplicit)
        {
            return new BinaryOperatorExpression(
                operatorKind, left, right,
                isLifted: isLifted, isChecked: isChecked,
                isCompareText: isCompareText, usesOperatorMethod: false, operatorMethod: null,
                semanticModel: semanticModel, syntax: syntax, type: resultType, constantValue: default, isImplicit: isImplicit);
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
