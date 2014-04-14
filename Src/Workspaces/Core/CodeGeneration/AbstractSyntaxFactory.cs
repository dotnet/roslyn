// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class AbstractSyntaxFactory : ISyntaxFactoryService
    {
        public SyntaxNode CreateUsingStatement(SyntaxNode variableDeclarationOrExpression, params SyntaxNode[] statements)
        {
            return CreateUsingStatement(variableDeclarationOrExpression, (IList<SyntaxNode>)statements);
        }

        public SyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, params SyntaxNode[] arguments)
        {
            return CreateObjectCreationExpression(typeName, (IList<SyntaxNode>)arguments);
        }

        public SyntaxNode CreateInvocationExpression(SyntaxNode expression, params SyntaxNode[] arguments)
        {
            return CreateInvocationExpression(expression, (IList<SyntaxNode>)arguments);
        }

        public SyntaxNode CreateElementAccessExpression(SyntaxNode expression, params SyntaxNode[] arguments)
        {
            return CreateElementAccessExpression(expression, (IList<SyntaxNode>)arguments);
        }

        public SyntaxNode CreateGenericName(string identifier, params ITypeSymbol[] typeArguments)
        {
            return CreateGenericName(identifier, (IList<ITypeSymbol>)typeArguments);
        }

        public SyntaxNode CreateArgument(SyntaxNode expression)
        {
            return CreateArgument(null, RefKind.None, expression);
        }

        public SyntaxNode CreateFalseExpression()
        {
            return CreateConstantExpression(false);
        }

        public SyntaxNode CreateTrueExpression()
        {
            return CreateConstantExpression(true);
        }

        public SyntaxNode CreateNullExpression()
        {
            return CreateConstantExpression(null);
        }

        public abstract SyntaxNode CreateReturnStatement(SyntaxNode expressionOpt = null);
        public abstract SyntaxNode CreateThrowStatement(SyntaxNode expressionOpt = null);
        public abstract SyntaxNode CreateIfStatement(SyntaxNode condition, IList<SyntaxNode> trueStatements, IList<SyntaxNode> falseStatementsOpt = null);
        public abstract SyntaxNode CreateExpressionStatement(SyntaxNode expression);
        public abstract SyntaxNode CreateUsingStatement(SyntaxNode variableDeclarationOrExpression, IList<SyntaxNode> statements);

        public abstract SyntaxNode CreateRawExpression(string text);
        public abstract SyntaxNode CreateMemberAccessExpression(SyntaxNode expression, SyntaxNode simpleName);
        public abstract SyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, IList<SyntaxNode> arguments);
        public abstract SyntaxNode CreateInvocationExpression(SyntaxNode expression, IList<SyntaxNode> arguments);
        public abstract SyntaxNode CreateElementAccessExpression(SyntaxNode expression, IList<SyntaxNode> arguments);
        public abstract SyntaxNode CreateDefaultExpression(ITypeSymbol type);
        public abstract SyntaxNode CreateTypeReferenceExpression(INamedTypeSymbol typeSymbol);
        public abstract SyntaxNode CreateNegateExpression(SyntaxNode expression);
        public abstract SyntaxNode CreateLogicalNotExpression(SyntaxNode expression);
        public abstract SyntaxNode CreateIsExpression(SyntaxNode expression, ITypeSymbol type);
        public abstract SyntaxNode CreateAsExpression(SyntaxNode expression, ITypeSymbol type);
        public abstract SyntaxNode CreateCastExpression(ITypeSymbol type, SyntaxNode expression);
        public abstract SyntaxNode CreateConvertExpression(ITypeSymbol type, SyntaxNode expression);
        public abstract SyntaxNode CreateAssignExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateValueEqualsExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateReferenceEqualsExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateValueNotEqualsExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateAddExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateMultiplyExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateBinaryAndExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateBinaryOrExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateLogicalAndExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateLogicalOrExpression(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse);
        public abstract SyntaxNode CreateThisExpression();
        public abstract SyntaxNode CreateBaseExpression();
        public abstract SyntaxNode CreateConstantExpression(object value);
        public abstract SyntaxNode CreateIdentifierName(string identifier);
        public abstract SyntaxNode CreateGenericName(string identifier, IList<ITypeSymbol> typeArguments);
        public abstract SyntaxNode CreateQualifiedName(SyntaxNode left, SyntaxNode right);
        public abstract SyntaxNode CreateArgument(string nameOpt, RefKind refKind, SyntaxNode expression);

        public abstract SyntaxNode CreateVariableDeclarator(ITypeSymbol type, string name, SyntaxNode expressionOpt = null);
        public abstract SyntaxNode CreateLocalDeclarationStatement(bool isConst, ITypeSymbol type, SyntaxNode variableDeclarator);

        public SyntaxNode CreateLocalDeclarationStatement(bool isConst, SyntaxNode variableDeclarator)
        {
            return CreateLocalDeclarationStatement(isConst, null, variableDeclarator);
        }

        public SyntaxNode CreateVariableDeclarator(string name, SyntaxNode expressionOpt = null)
        {
            return CreateVariableDeclarator(null, name, expressionOpt);
        }

        public SyntaxNode CreateLocalDeclarationStatement(SyntaxNode variableDeclarator)
        {
            return CreateLocalDeclarationStatement(null, variableDeclarator);
        }

        public SyntaxNode CreateLocalDeclarationStatement(ITypeSymbol type, SyntaxNode variableDeclarator)
        {
            return CreateLocalDeclarationStatement(false, type, variableDeclarator);
        }

        public abstract SyntaxNode CreateSwitchLabel(SyntaxNode expressionOpt = null);
        public abstract SyntaxNode CreateSwitchSection(SyntaxNode switchLabel, IList<SyntaxNode> statements);
        public abstract SyntaxNode CreateSwitchStatement(SyntaxNode expression, IList<SyntaxNode> switchSections);

        public SyntaxNode CreateSwitchSection(SyntaxNode switchLabel, params SyntaxNode[] statements)
        {
            return CreateSwitchSection(switchLabel, (IList<SyntaxNode>)statements);
        }

        public SyntaxNode CreateSwitchStatement(SyntaxNode expression, params SyntaxNode[] switchSections)
        {
            return CreateSwitchStatement(expression, (IList<SyntaxNode>)switchSections);
        }

        public abstract SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, SyntaxNode body);
        public abstract SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, IList<SyntaxNode> statements);

        public SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, params SyntaxNode[] statements)
        {
            return CreateLambdaExpression(parameters, (IList<SyntaxNode>)statements);
        }

        public abstract SyntaxNode CreateExitSwitchStatement();
    }
}