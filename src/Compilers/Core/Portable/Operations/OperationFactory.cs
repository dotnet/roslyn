// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal static class OperationFactory
    {
        public static IVariableDeclarationOperation CreateVariableDeclaration(ILocalSymbol variable, IVariableInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return CreateVariableDeclaration(ImmutableArray.Create(variable), initializer, semanticModel, syntax);
        }

        public static VariableDeclaration CreateVariableDeclaration(ImmutableArray<ILocalSymbol> variables, IVariableInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax)
        {
            return new VariableDeclaration(
                variables,
                initializer,
                semanticModel,
                syntax,
                type: null,
                constantValue: default(Optional<object>),
                isImplicit: false); // variable declaration is always explicit
        }

        public static IVariableInitializerOperation CreateVariableInitializer(SyntaxNode syntax, IOperation initializerValue, SemanticModel semanticModel, bool isImplicit)
        {
            return new VariableInitializer(initializerValue, semanticModel, syntax, type: null, constantValue: default, isImplicit: isImplicit);
        }

        public static IConditionalOperation CreateConditionalExpression(IOperation condition, IOperation whenTrue, IOperation whenFalse, ITypeSymbol resultType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var isStatement = false;
            return new ConditionalOperation(
                condition,
                whenTrue,
                whenFalse,
                isStatement,
                semanticModel,
                syntax,
                resultType,
                default(Optional<object>),
                isImplicit);
        }

        public static IExpressionStatementOperation CreateSimpleAssignmentExpressionStatement(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var expression = new SimpleAssignmentExpression(target, value, semanticModel, syntax, target.Type, default(Optional<object>), isImplicit);
            return new ExpressionStatement(expression, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }

        public static IExpressionStatementOperation CreateCompoundAssignmentExpressionStatement(
            IOperation target, IOperation value, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            var expression = new CompoundAssignmentExpression(
                     operatorKind,
                     isLifted,
                     isChecked,
                     target,
                     value,
                     operatorMethod,
                     semanticModel,
                     syntax,
                     target.Type,
                     default(Optional<object>),
                     isImplicit);

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
                isCompareText: isCompareText, operatorMethod: null,
                semanticModel: semanticModel, syntax: syntax, type: resultType, constantValue: default, isImplicit: isImplicit);
        }

        public static IInvalidOperation CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit)
        {
            return CreateInvalidExpression(semanticModel, syntax, ImmutableArray<IOperation>.Empty, isImplicit);
        }

        public static IInvalidOperation CreateInvalidExpression(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children, bool isImplicit)
        {
            var isStatement = false;
            return new InvalidOperation(children, isStatement, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }
    }
}
