#if false
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    [Export(typeof(ICodeDefinitionFactory))]
    internal partial class CodeDefinitionFactory : ICodeDefinitionFactory
    {
        [ImportingConstructor]
        public CodeDefinitionFactory()
        {
        }

        public RawStatementDefinition CreateRawStatement(string text)
        {
            return new RawStatementDefinition(text);
        }

        public CommonSyntaxNode CreateThrowStatement(CommonSyntaxNode expression)
        {
            return new ThrowStatementDefinition(expression);
        }

        public CommonSyntaxNode CreateRawExpression(string text)
        {
            return new RawExpressionDefinition(text);
        }

        public CommonSyntaxNode CreateObjectCreationExpression(ITypeSymbol type, params CommonSyntaxNode[] arguments)
        {
            return CreateObjectCreationExpression(type, (IList<CommonSyntaxNode>)arguments);
        }

        public CommonSyntaxNode CreateObjectCreationExpression(ITypeSymbol type, IList<CommonSyntaxNode> arguments)
        {
            return new ObjectCreationExpressionDefinition(type, arguments);
        }

        public CommonSyntaxNode CreateIdentifierName(string identifier)
        {
            return new IdentifierNameDefinition(identifier);
        }

        public CommonSyntaxNode CreateArgument(string nameOpt, RefKind refKind, CommonSyntaxNode expression)
        {
            return new ArgumentDefinition(nameOpt, refKind, expression);
        }

        public CommonSyntaxNode CreateReturnStatement(CommonSyntaxNode expressionOpt)
        {
            return new ReturnStatementDefinition(expressionOpt);
        }

        public CommonSyntaxNode CreateGenericName(string identifier, IList<ITypeSymbol> typeArguments)
        {
            return new GenericNameDefinition(identifier, typeArguments);
        }

        public CommonSyntaxNode CreateQualifiedName(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new QualifiedNameDefinition(left, right);
        }

        public CommonSyntaxNode CreateThisExpression()
        {
            return new ThisExpressionDefinition();
        }

        public CommonSyntaxNode CreateMemberAccessExpression(CommonSyntaxNode expression, CommonSyntaxNode simpleName)
        {
            return new MemberAccessExpressionDefinition(expression, simpleName);
        }

        public CommonSyntaxNode CreateInvocationExpression(CommonSyntaxNode expression, params CommonSyntaxNode[] arguments)
        {
            return CreateInvocationExpression(expression, (IList<CommonSyntaxNode>)arguments);
        }

        public CommonSyntaxNode CreateInvocationExpression(CommonSyntaxNode expression, IList<CommonSyntaxNode> arguments)
        {
            return new InvocationExpressionDefinition(expression, arguments);
        }

        public CommonSyntaxNode CreateElementAccessExpression(CommonSyntaxNode expression, params CommonSyntaxNode[] arguments)
        {
            return CreateElementAccessExpression(expression, (IList<CommonSyntaxNode>)arguments);
        }

        public CommonSyntaxNode CreateElementAccessExpression(CommonSyntaxNode expression, IList<CommonSyntaxNode> arguments)
        {
            return new ElementAccessExpressionDefinition(expression, arguments);
        }

        public CommonSyntaxNode CreateExpressionStatement(CommonSyntaxNode expression)
        {
            return new ExpressionStatementDefinition(expression);
        }

        public CommonSyntaxNode CreateAssignExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new AssignExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateDefaultExpression(ITypeSymbol type)
        {
            return new DefaultExpressionDefinition(type);
        }

        public CommonSyntaxNode CreateIfStatement(CommonSyntaxNode condition, IList<CommonSyntaxNode> trueStatements, IList<CommonSyntaxNode> falseStatementsOpt = null)
        {
            return new IfStatementDefinition(condition, trueStatements, falseStatementsOpt);
        }

        public CommonSyntaxNode CreateNegateExpression(CommonSyntaxNode expression)
        {
            return new NegateExpressionDefinition(expression);
        }

        public CommonSyntaxNode CreateIsExpression(CommonSyntaxNode expression, ITypeSymbol type)
        {
            return new IsExpressionDefinition(expression, type);
        }

        public CommonSyntaxNode CreateAsExpression(CommonSyntaxNode expression, ITypeSymbol type)
        {
            return new AsExpressionDefinition(expression, type);
        }

        public CommonSyntaxNode CreateFalseExpression()
        {
            return new ConstantExpressionDefinition(false);
        }

        public CommonSyntaxNode CreateTrueExpression()
        {
            return new ConstantExpressionDefinition(true);
        }

        public CommonSyntaxNode CreateCastExpression(ITypeSymbol type, CommonSyntaxNode expression)
        {
            return new CastExpressionDefinition(type, expression);
        }

        public CommonSyntaxNode CreateTypeReferenceExpression(INamedTypeSymbol type)
        {
            return new TypeReferenceExpressionDefinition(type);
        }

        public CommonSyntaxNode CreateValueEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new ValueEqualsExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateReferenceEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new ReferenceEqualsExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateValueNotEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new ValueNotEqualsExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateReferenceNotEqualsExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new ReferenceNotEqualsExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateLogicalAndExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new LogicalAndExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateLogicalOrExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new LogicalOrExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateNullExpression()
        {
            return new ConstantExpressionDefinition(null);
        }

        public CommonSyntaxNode CreateBaseExpression()
        {
            return new BaseExpressionDefinition();
        }

        public CommonSyntaxNode CreateGenericName(string identifier, params ITypeSymbol[] typeArguments)
        {
            return CreateGenericName(identifier, (IList<ITypeSymbol>)typeArguments);
        }

        public CommonSyntaxNode CreateArgument(CommonSyntaxNode expression)
        {
            return CreateArgument(nameOpt: null, refKind: RefKind.None, expression: expression);
        }

        public CommonSyntaxNode CreateAddExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new AddExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateMultiplyExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new MultiplyExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateConditionalExpression(CommonSyntaxNode condition, CommonSyntaxNode whenTrue, CommonSyntaxNode whenFalse)
        {
            return new ConditionalExpressionDefinition(condition, whenTrue, whenFalse);
        }

        public CommonSyntaxNode CreateConstantExpression(object value)
        {
            return new ConstantExpressionDefinition(value);
        }

        public CommonSyntaxNode CreateLogicalNotExpression(CommonSyntaxNode expression)
        {
            return new LogicalNotExpressionDefinition(expression);
        }

        public CommonSyntaxNode CreateVariableDeclarator(string name, CommonSyntaxNode expressionOpt = null)
        {
            return new VariableDeclaratorDefinition(name, expressionOpt);
        }

        public CommonSyntaxNode CreateVariableDeclaration(string name, CommonSyntaxNode expressionOpt = null)
        {
            return CreateVariableDeclaration(CreateVariableDeclarator(name, expressionOpt));
        }

        public CommonSyntaxNode CreateVariableDeclaration(params CommonSyntaxNode[] variableDeclarators)
        {
            return CreateVariableDeclaration((IList<CommonSyntaxNode>)variableDeclarators);
        }

        public CommonSyntaxNode CreateVariableDeclaration(IList<CommonSyntaxNode> variableDeclarators)
        {
            return CreateVariableDeclaration(null, variableDeclarators);
        }

        public CommonSyntaxNode CreateVariableDeclaration(ITypeSymbol typeOpt, params CommonSyntaxNode[] variableDeclarators)
        {
            return CreateVariableDeclaration(typeOpt, (IList<CommonSyntaxNode>)variableDeclarators);
        }

        public CommonSyntaxNode CreateVariableDeclaration(ITypeSymbol typeOpt, IList<CommonSyntaxNode> variableDeclarators)
        {
            return new VariableDeclarationDefinition(typeOpt, variableDeclarators);
        }

        public CommonSyntaxNode CreateLocalDeclarationStatement(string name, CommonSyntaxNode expressionOpt = null)
        {
            return CreateLocalDeclarationStatement(CreateVariableDeclaration(name, expressionOpt));
        }

        public CommonSyntaxNode CreateLocalDeclarationStatement(CommonSyntaxNode variableDeclaration)
        {
            return CreateLocalDeclarationStatement(false, variableDeclaration);
        }

        public CommonSyntaxNode CreateLocalDeclarationStatement(bool isConst, CommonSyntaxNode variableDeclaration)
        {
            return new LocalDeclarationStatementDefinition(isConst, variableDeclaration);
        }

        public CommonSyntaxNode CreateUsingStatement(CommonSyntaxNode variableDeclarationOrExpression, params CommonSyntaxNode[] statements)
        {
            return CreateUsingStatement(variableDeclarationOrExpression, (IList<CommonSyntaxNode>)statements);
        }

        public CommonSyntaxNode CreateUsingStatement(CommonSyntaxNode variableDeclarationOrExpression, IList<CommonSyntaxNode> statements)
        {
            return new UsingStatementDefinition(variableDeclarationOrExpression, statements);
        }

        public CommonSyntaxNode CreateBinaryOrExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new BinaryOrExpressionDefinition(left, right);
        }

        public CommonSyntaxNode CreateBinaryAndExpression(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            return new BinaryAndExpressionDefinition(left, right);
        }
    }
}
#endif