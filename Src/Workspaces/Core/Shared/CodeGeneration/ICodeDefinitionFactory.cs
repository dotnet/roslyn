#if false
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal interface ICodeDefinitionFactory
    {
        RawStatementDefinition CreateRawStatement(string text);

        CommonSyntaxNode CreateReturnStatement(CommonSyntaxNode expressionOpt = null);
        CommonSyntaxNode CreateThrowStatement(CommonSyntaxNode expressionOpt = null);
        CommonSyntaxNode CreateIfStatement(CommonSyntaxNode condition, IList<CommonSyntaxNode> trueStatements, IList<CommonSyntaxNode> falseStatementsOpt = null);
        CommonSyntaxNode CreateExpressionStatement(CommonSyntaxNode expression);
        CommonSyntaxNode CreateUsingStatement(CommonSyntaxNode variableDeclarationOrExpression, params CommonSyntaxNode[] statements);
        CommonSyntaxNode CreateUsingStatement(CommonSyntaxNode variableDeclarationOrExpression, IList<CommonSyntaxNode> statements);

        CommonSyntaxNode CreateVariableDeclarator(string name, CommonSyntaxNode expressionOpt = null);

        CommonSyntaxNode CreateVariableDeclaration(string name, CommonSyntaxNode expressionOpt = null);
        CommonSyntaxNode CreateVariableDeclaration(params CommonSyntaxNode[] variableDeclarators);
        CommonSyntaxNode CreateVariableDeclaration(IList<CommonSyntaxNode> variableDeclarators);
        CommonSyntaxNode CreateVariableDeclaration(ITypeSymbol typeOpt, params CommonSyntaxNode[] variableDeclarators);
        CommonSyntaxNode CreateVariableDeclaration(ITypeSymbol typeOpt, IList<CommonSyntaxNode> variableDeclarators);

        CommonSyntaxNode CreateLocalDeclarationStatement(string name, CommonSyntaxNode expressionOpt = null);
        CommonSyntaxNode CreateLocalDeclarationStatement(CommonSyntaxNode variableDeclaration);
        CommonSyntaxNode CreateLocalDeclarationStatement(bool isConst, CommonSyntaxNode variableDeclaration);

        CommonSyntaxNode CreateRawExpression(string text);
        CommonSyntaxNode CreateMemberAccessExpression(CommonSyntaxNode expression, CommonSyntaxNode simpleName);
        CommonSyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, params CommonSyntaxNode[] arguments);
        CommonSyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, IList<CommonSyntaxNode> arguments);
        CommonSyntaxNode CreateInvocationExpression(CommonSyntaxNode expression, params CommonSyntaxNode[] arguments);
        CommonSyntaxNode CreateInvocationExpression(CommonSyntaxNode expression, IList<CommonSyntaxNode> arguments);
        CommonSyntaxNode CreateElementAccessExpression(CommonSyntaxNode expression, params CommonSyntaxNode[] arguments);
        CommonSyntaxNode CreateElementAccessExpression(CommonSyntaxNode expression, IList<CommonSyntaxNode> arguments);

        CommonSyntaxNode CreateDefaultExpression(ITypeSymbol type);
        CommonSyntaxNode CreateTypeReferenceExpression(INamedTypeSymbol typeSymbol);
        CommonSyntaxNode CreateNegateExpression(CommonSyntaxNode expression);
        CommonSyntaxNode CreateLogicalNotExpression(CommonSyntaxNode expression);
        CommonSyntaxNode CreateIsExpression(CommonSyntaxNode expression, ITypeSymbol type);
        CommonSyntaxNode CreateAsExpression(CommonSyntaxNode expression, ITypeSymbol type);
        CommonSyntaxNode CreateCastExpression(ITypeSymbol type, CommonSyntaxNode expression);

        CommonSyntaxNode CreateAssignExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateValueEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateReferenceEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateValueNotEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateReferenceNotEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right);

        CommonSyntaxNode CreateAddExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateMultiplyExpression(CommonSyntaxNode left, CommonSyntaxNode right);

        CommonSyntaxNode CreateBinaryAndExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateBinaryOrExpression(CommonSyntaxNode left, CommonSyntaxNode right);

        CommonSyntaxNode CreateLogicalAndExpression(CommonSyntaxNode left, CommonSyntaxNode right);
        CommonSyntaxNode CreateLogicalOrExpression(CommonSyntaxNode left, CommonSyntaxNode right);

        CommonSyntaxNode CreateConditionalExpression(CommonSyntaxNode condition, CommonSyntaxNode whenTrue, CommonSyntaxNode whenFalse);

        CommonSyntaxNode CreateFalseExpression();
        CommonSyntaxNode CreateTrueExpression();
        CommonSyntaxNode CreateNullExpression();
        CommonSyntaxNode CreateThisExpression();
        CommonSyntaxNode CreateBaseExpression();
        CommonSyntaxNode CreateConstantExpression(object value);

        CommonSyntaxNode CreateIdentifierName(string identifier);
        CommonSyntaxNode CreateGenericName(string identifier, params ITypeSymbol[] typeArguments);
        CommonSyntaxNode CreateGenericName(string identifier, IList<ITypeSymbol> typeArguments);
        CommonSyntaxNode CreateQualifiedName(CommonSyntaxNode left, CommonSyntaxNode right);

        CommonSyntaxNode CreateArgument(CommonSyntaxNode expression);
        CommonSyntaxNode CreateArgument(string nameOpt, RefKind refKind, CommonSyntaxNode expression);
    }
}
#endif