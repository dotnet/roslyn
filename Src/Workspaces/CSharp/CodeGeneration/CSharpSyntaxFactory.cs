// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(ISyntaxFactoryService), LanguageNames.CSharp)]
    internal class CSharpSyntaxFactory : AbstractSyntaxFactory
    {
        public override SyntaxNode CreateReturnStatement(SyntaxNode expressionOpt = null)
        {
            return SyntaxFactory.ReturnStatement((ExpressionSyntax)expressionOpt);
        }

        public override SyntaxNode CreateThrowStatement(SyntaxNode expressionOpt = null)
        {
            return SyntaxFactory.ThrowStatement((ExpressionSyntax)expressionOpt);
        }

        public override SyntaxNode CreateIfStatement(SyntaxNode condition, IList<SyntaxNode> trueStatements, IList<SyntaxNode> falseStatementsOpt = null)
        {
            return SyntaxFactory.IfStatement(
                (ExpressionSyntax)condition,
                CreateBlock(trueStatements),
                falseStatementsOpt == null
                    ? null
                    : SyntaxFactory.ElseClause(CreateBlock(falseStatementsOpt)));
        }

        private BlockSyntax CreateBlock(IList<SyntaxNode> statements)
        {
            return SyntaxFactory.Block(SyntaxFactory.List(statements.Cast<StatementSyntax>()));
        }

        public override SyntaxNode CreateExpressionStatement(SyntaxNode expression)
        {
            return SyntaxFactory.ExpressionStatement((ExpressionSyntax)expression);
        }

        public override SyntaxNode CreateUsingStatement(SyntaxNode variableDeclarationOrExpression, IList<SyntaxNode> statements)
        {
            return SyntaxFactory.UsingStatement(
                variableDeclarationOrExpression as VariableDeclarationSyntax,
                variableDeclarationOrExpression as ExpressionSyntax,
                CreateBlock(statements));
        }

        public override SyntaxNode CreateRawExpression(string text)
        {
            throw new NotImplementedException();
        }

        public override SyntaxNode CreateMemberAccessExpression(SyntaxNode expression, SyntaxNode simpleName)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                (ExpressionSyntax)expression, (SimpleNameSyntax)simpleName);
        }

        public override SyntaxNode CreateObjectCreationExpression(ITypeSymbol typeName, IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.ObjectCreationExpression(typeName.GenerateTypeSyntax(), CreateArgumentList(arguments), null);
        }

        private ArgumentListSyntax CreateArgumentList(IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.ArgumentList(CreateArguments(arguments));
        }

        private SeparatedSyntaxList<ArgumentSyntax> CreateArguments(IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.SeparatedList(arguments.Select(CreateArgument).Cast<ArgumentSyntax>());
        }

        public override SyntaxNode CreateInvocationExpression(SyntaxNode expression, IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.InvocationExpression((ExpressionSyntax)expression, CreateArgumentList(arguments));
        }

        public override SyntaxNode CreateElementAccessExpression(SyntaxNode expression, IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.ElementAccessExpression(
                (ExpressionSyntax)expression, SyntaxFactory.BracketedArgumentList(CreateArguments(arguments)));
        }

        public override SyntaxNode CreateDefaultExpression(ITypeSymbol type)
        {
            // If it's just a reference type, then "null" is the default expression for it.  Note:
            // this counts for actual reference type, or a type parameter with a 'class' constraint.
            // Also, if it's a nullable type, then we can use "null".
            if (type.IsReferenceType ||
                type.IsPointerType() ||
                type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0", 0));
            }

            // Default to a "default(<typename>)" expression.
            return SyntaxFactory.DefaultExpression(type.GenerateTypeSyntax());
        }

        public override SyntaxNode CreateTypeReferenceExpression(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GenerateExpressionSyntax();
        }

        public override SyntaxNode CreateNegateExpression(SyntaxNode expression)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, Parenthesize(expression));
        }

        private ExpressionSyntax Parenthesize(SyntaxNode expression)
        {
            return ((ExpressionSyntax)expression).Parenthesize();
        }

        public override SyntaxNode CreateLogicalNotExpression(SyntaxNode expression)
        {
            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Parenthesize(expression));
        }

        public override SyntaxNode CreateIsExpression(SyntaxNode expression, ITypeSymbol type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, Parenthesize(expression), type.GenerateTypeSyntax());
        }

        public override SyntaxNode CreateAsExpression(SyntaxNode expression, ITypeSymbol type)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, Parenthesize(expression), type.GenerateTypeSyntax());
        }

        public override SyntaxNode CreateCastExpression(ITypeSymbol type, SyntaxNode expression)
        {
            return SyntaxFactory.CastExpression(type.GenerateTypeSyntax(), Parenthesize(expression));
        }

        public override SyntaxNode CreateConvertExpression(ITypeSymbol type, SyntaxNode expression)
        {
            return SyntaxFactory.CastExpression(type.GenerateTypeSyntax(), Parenthesize(expression));
        }

        public override SyntaxNode CreateAssignExpression(SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)left, Parenthesize(right));
        }

        private SyntaxNode CreateBinaryExpression(SyntaxKind syntaxKind, SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.BinaryExpression(syntaxKind, Parenthesize(left), Parenthesize(right));
        }

        public override SyntaxNode CreateValueEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);
        }

        public override SyntaxNode CreateReferenceEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.EqualsExpression, left, right);
        }

        public override SyntaxNode CreateValueNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);
        }

        public override SyntaxNode CreateReferenceNotEqualsExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.NotEqualsExpression, left, right);
        }

        public override SyntaxNode CreateAddExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.AddExpression, left, right);
        }

        public override SyntaxNode CreateMultiplyExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.MultiplyExpression, left, right);
        }

        public override SyntaxNode CreateBinaryAndExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.BitwiseAndExpression, left, right);
        }

        public override SyntaxNode CreateBinaryOrExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);
        }

        public override SyntaxNode CreateLogicalAndExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LogicalAndExpression, left, right);
        }

        public override SyntaxNode CreateLogicalOrExpression(SyntaxNode left, SyntaxNode right)
        {
            return CreateBinaryExpression(SyntaxKind.LogicalOrExpression, left, right);
        }

        public override SyntaxNode CreateConditionalExpression(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse)
        {
            return SyntaxFactory.ConditionalExpression(Parenthesize(condition), Parenthesize(whenTrue), Parenthesize(whenFalse));
        }

        public override SyntaxNode CreateThisExpression()
        {
            return SyntaxFactory.ThisExpression();
        }

        public override SyntaxNode CreateBaseExpression()
        {
            return SyntaxFactory.BaseExpression();
        }

        public override SyntaxNode CreateConstantExpression(object value)
        {
            return ExpressionGenerator.GenerateNonEnumValueExpression(null, value, canUseFieldReference: true);
        }

        public override SyntaxNode CreateIdentifierName(string identifier)
        {
            return identifier.ToIdentifierName();
        }

        public override SyntaxNode CreateGenericName(string identifier, IList<ITypeSymbol> typeArguments)
        {
            return SyntaxFactory.GenericName(identifier.ToIdentifierToken(),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Select(t => t.GenerateTypeSyntax()))));
        }

        public override SyntaxNode CreateQualifiedName(SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.QualifiedName((NameSyntax)left, (SimpleNameSyntax)right);
        }

        public override SyntaxNode CreateArgument(string nameOpt, RefKind refKind, SyntaxNode expression)
        {
            if (expression is ArgumentSyntax)
            {
                return expression;
            }

            return SyntaxFactory.Argument(
                nameOpt == null ? null : SyntaxFactory.NameColon(nameOpt),
                refKind == RefKind.Ref ? SyntaxFactory.Token(SyntaxKind.RefKeyword) :
                refKind == RefKind.Out ? SyntaxFactory.Token(SyntaxKind.OutKeyword) : default(SyntaxToken),
                (ExpressionSyntax)expression);
        }

        public override SyntaxNode CreateVariableDeclarator(ITypeSymbol type, string name, SyntaxNode expressionOpt = null)
        {
            return SyntaxFactory.VariableDeclarator(name.ToIdentifierToken(), null,
                expressionOpt == null ? null : SyntaxFactory.EqualsValueClause((ExpressionSyntax)expressionOpt));
        }

        public override SyntaxNode CreateLocalDeclarationStatement(bool isConst, ITypeSymbol type, SyntaxNode variableDeclarator)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                isConst ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)) : default(SyntaxTokenList),
                SyntaxFactory.VariableDeclaration(
                    type == null ? SyntaxFactory.IdentifierName("var") : type.GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList((VariableDeclaratorSyntax)variableDeclarator)));
        }

        public override SyntaxNode CreateSwitchLabel(SyntaxNode expressionOpt = null)
        {
            return expressionOpt == null
                ? SyntaxFactory.SwitchLabel(SyntaxKind.DefaultSwitchLabel)
                : SyntaxFactory.SwitchLabel(SyntaxKind.CaseSwitchLabel, (ExpressionSyntax)expressionOpt);
        }

        public override SyntaxNode CreateSwitchSection(SyntaxNode switchLabel, IList<SyntaxNode> statements)
        {
            return SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList((SwitchLabelSyntax)switchLabel),
                statements.Cast<StatementSyntax>().ToSyntaxList());
        }

        public override SyntaxNode CreateSwitchStatement(SyntaxNode expression, IList<SyntaxNode> switchSections)
        {
            return SyntaxFactory.SwitchStatement(
                (ExpressionSyntax)expression,
                switchSections.Cast<SwitchSectionSyntax>().ToSyntaxList());
        }

        public override SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, SyntaxNode body)
        {
            if (parameters.Count == 1 && parameters[0].Type == null)
            {
                return SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(parameters[0].Name.ToIdentifierToken()),
                    (CSharpSyntaxNode)body);
            }
            else
            {
                return SyntaxFactory.ParenthesizedLambdaExpression(
                    ParameterGenerator.GenerateParameterList(parameters, isExplicit: false, options: CodeGenerationOptions.Default),
                    (CSharpSyntaxNode)body);
            }
        }

        public override SyntaxNode CreateLambdaExpression(IList<IParameterSymbol> parameters, IList<SyntaxNode> statements)
        {
            if (statements.Count == 1)
            {
                return CreateLambdaExpression(parameters, statements[0]);
            }
            else
            {
                return CreateLambdaExpression(parameters, SyntaxFactory.Block(statements.Cast<StatementSyntax>().ToSyntaxList()));
            }
        }

        public override SyntaxNode CreateExitSwitchStatement()
        {
            return SyntaxFactory.BreakStatement();
        }
    }
}