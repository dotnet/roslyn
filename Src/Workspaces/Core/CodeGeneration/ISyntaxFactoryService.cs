// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal interface ISyntaxFactoryService : ILanguageService
    {
        SyntaxNode CreateReturnStatement(SyntaxNode expressionOpt = null);
        SyntaxNode CreateThrowStatement(SyntaxNode expressionOpt = null);
        SyntaxNode CreateIfStatement(SyntaxNode condition, IList<SyntaxNode> trueStatements, IList<SyntaxNode> falseStatementsOpt = null);
        SyntaxNode CreateExpressionStatement(SyntaxNode expression);
        SyntaxNode CreateUsingStatement(SyntaxNode variableDeclaratorOrExpression, params SyntaxNode[] statements);
        SyntaxNode CreateUsingStatement(SyntaxNode variableDeclaratorOrExpression, IList<SyntaxNode> statements);

        SyntaxNode CreateVariableDeclarator(string name, SyntaxNode expressionOpt = null);
        SyntaxNode CreateVariableDeclarator(ITypeSymbol type, string name, SyntaxNode expressionOpt = null);

        SyntaxNode CreateLocalDeclarationStatement(SyntaxNode variableDeclarator);
        SyntaxNode CreateLocalDeclarationStatement(ITypeSymbol type, SyntaxNode variableDeclarator);
        SyntaxNode CreateLocalDeclarationStatement(bool isConst, SyntaxNode variableDeclarator);
        SyntaxNode CreateLocalDeclarationStatement(bool isConst, ITypeSymbol type, SyntaxNode variableDeclarator);

        SyntaxNode CreateRawExpression(string text);
        SyntaxNode CreateMemberAccessExpression(SyntaxNode expression, SyntaxNode simpleName);
        SyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, params SyntaxNode[] arguments);
        SyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, IList<SyntaxNode> arguments);
        SyntaxNode CreateInvocationExpression(SyntaxNode expression, params SyntaxNode[] arguments);
        SyntaxNode CreateInvocationExpression(SyntaxNode expression, IList<SyntaxNode> arguments);
        SyntaxNode CreateElementAccessExpression(SyntaxNode expression, params SyntaxNode[] arguments);
        SyntaxNode CreateElementAccessExpression(SyntaxNode expression, IList<SyntaxNode> arguments);

        SyntaxNode CreateDefaultExpression(ITypeSymbol type);
        SyntaxNode CreateTypeReferenceExpression(INamedTypeSymbol typeSymbol);
        SyntaxNode CreateNegateExpression(SyntaxNode expression);
        SyntaxNode CreateLogicalNotExpression(SyntaxNode expression);
        SyntaxNode CreateIsExpression(SyntaxNode expression, ITypeSymbol type);
        SyntaxNode CreateAsExpression(SyntaxNode expression, ITypeSymbol type);

        SyntaxNode CreateCastExpression(ITypeSymbol type, SyntaxNode expression);
        SyntaxNode CreateConvertExpression(ITypeSymbol type, SyntaxNode expression);

        SyntaxNode CreateAssignExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateValueEqualsExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateReferenceEqualsExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateValueNotEqualsExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right);

        SyntaxNode CreateAddExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateMultiplyExpression(SyntaxNode left, SyntaxNode right);

        SyntaxNode CreateBinaryAndExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateBinaryOrExpression(SyntaxNode left, SyntaxNode right);

        SyntaxNode CreateLogicalAndExpression(SyntaxNode left, SyntaxNode right);
        SyntaxNode CreateLogicalOrExpression(SyntaxNode left, SyntaxNode right);

        SyntaxNode CreateConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse);

        SyntaxNode CreateFalseExpression();
        SyntaxNode CreateTrueExpression();
        SyntaxNode CreateNullExpression();
        SyntaxNode CreateThisExpression();
        SyntaxNode CreateBaseExpression();
        SyntaxNode CreateConstantExpression(object value);

        SyntaxNode CreateIdentifierName(string identifier);
        SyntaxNode CreateGenericName(string identifier, params ITypeSymbol[] typeArguments);
        SyntaxNode CreateGenericName(string identifier, IList<ITypeSymbol> typeArguments);
        SyntaxNode CreateQualifiedName(SyntaxNode left, SyntaxNode right);

        SyntaxNode CreateArgument(SyntaxNode expression);
        SyntaxNode CreateArgument(string nameOpt, RefKind refKind, SyntaxNode expression);

        SyntaxNode CreateSwitchLabel(SyntaxNode expressionOpt = null);
        SyntaxNode CreateSwitchSection(SyntaxNode switchLabel, params SyntaxNode[] statements);
        SyntaxNode CreateSwitchSection(SyntaxNode switchLabel, IList<SyntaxNode> statements);
        SyntaxNode CreateSwitchStatement(SyntaxNode expression, params SyntaxNode[] switchSections);
        SyntaxNode CreateSwitchStatement(SyntaxNode expression, IList<SyntaxNode> switchSections);
        SyntaxNode CreateExitSwitchStatement();

        SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, SyntaxNode body);
        SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, params SyntaxNode[] statements);
        SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, IList<SyntaxNode> statements);
    }
}