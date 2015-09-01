// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateVariable
{
    [ExportLanguageService(typeof(IGenerateVariableService), LanguageNames.CSharp), Shared]
    internal partial class CSharpGenerateVariableService :
        AbstractGenerateVariableService<CSharpGenerateVariableService, SimpleNameSyntax, ExpressionSyntax>
    {
        protected override bool IsExplicitInterfaceGeneration(SyntaxNode node)
        {
            return node is PropertyDeclarationSyntax;
        }

        protected override bool IsIdentifierNameGeneration(SyntaxNode node)
        {
            return node is IdentifierNameSyntax;
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        {
            return containingType.ContainingTypesOrSelfHasUnsafeKeyword();
        }

        protected override bool TryInitializeExplicitInterfaceState(
            SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken,
            out SyntaxToken identifierToken, out IPropertySymbol propertySymbol, out INamedTypeSymbol typeToGenerateIn)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)node;
            identifierToken = propertyDeclaration.Identifier;

            if (propertyDeclaration.ExplicitInterfaceSpecifier != null)
            {
                var semanticModel = document.SemanticModel;
                propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken) as IPropertySymbol;
                if (propertySymbol != null && !propertySymbol.ExplicitInterfaceImplementations.Any())
                {
                    var info = semanticModel.GetTypeInfo(propertyDeclaration.ExplicitInterfaceSpecifier.Name, cancellationToken);
                    typeToGenerateIn = info.Type as INamedTypeSymbol;
                    return typeToGenerateIn != null;
                }
            }

            identifierToken = default(SyntaxToken);
            propertySymbol = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeIdentifierNameState(
            SemanticDocument document, SimpleNameSyntax identifierName, CancellationToken cancellationToken,
            out SyntaxToken identifierToken, out ExpressionSyntax simpleNameOrMemberAccessExpression, out bool isInExecutableBlock, out bool isConditionalAccessExpression)
        {
            identifierToken = identifierName.Identifier;
            if (identifierToken.ValueText != string.Empty &&
                !identifierName.IsVar)
            {
                var memberAccess = identifierName.Parent as MemberAccessExpressionSyntax;
                var conditionalMemberAccess = identifierName.Parent.Parent as ConditionalAccessExpressionSyntax;
                if (memberAccess?.Name == identifierName)
                {
                    simpleNameOrMemberAccessExpression = memberAccess;
                }
                else if ((conditionalMemberAccess?.WhenNotNull as MemberBindingExpressionSyntax)?.Name == identifierName)
                {
                    simpleNameOrMemberAccessExpression = conditionalMemberAccess;
                }
                else
                {
                    simpleNameOrMemberAccessExpression = identifierName;
                }

                // If we're being invoked, then don't offer this, offer generate method instead.
                // Note: we could offer to generate a field with a delegate type.  However, that's
                // very esoteric and probably not what most users want.
                if (!IsLegal(document, simpleNameOrMemberAccessExpression, cancellationToken))
                {
                    isInExecutableBlock = false;
                    isConditionalAccessExpression = false;
                    return false;
                }

                var block = identifierName.GetAncestor<BlockSyntax>();
                isInExecutableBlock = block != null && !block.OverlapsHiddenPosition(cancellationToken);
                isConditionalAccessExpression = conditionalMemberAccess != null;
                return true;
            }

            identifierToken = default(SyntaxToken);
            simpleNameOrMemberAccessExpression = null;
            isInExecutableBlock = false;
            isConditionalAccessExpression = false;
            return false;
        }

        private bool IsLegal(
            SemanticDocument document,
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            // TODO(cyrusn): Consider supporting this at some point.  It is difficult because we'd
            // need to replace the identifier typed with the fully qualified name of the field we
            // were generating.
            if (expression.IsParentKind(SyntaxKind.AttributeArgument))
            {
                return false;
            }

            if (expression.IsParentKind(SyntaxKind.ConditionalAccessExpression))
            {
                return true;
            }

            return expression.CanReplaceWithLValue(document.SemanticModel, cancellationToken);
        }

        protected override bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, OptionSet options, out SyntaxNode newRoot)
        {
            var token = identifierToken;
            var node = identifierToken.Parent as IdentifierNameSyntax;
            if (node.IsLeftSideOfAssignExpression() && node.Parent.IsParentKind(SyntaxKind.ExpressionStatement))
            {
                var assignExpression = (AssignmentExpressionSyntax)node.Parent;
                var expressionStatement = (StatementSyntax)assignExpression.Parent;

                var declarationStatement = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        GenerateTypeSyntax(type, options),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(token, null, SyntaxFactory.EqualsValueClause(
                                assignExpression.OperatorToken, assignExpression.Right)))));
                declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation);

                var root = token.GetAncestor<CompilationUnitSyntax>();
                newRoot = root.ReplaceNode(expressionStatement, declarationStatement);

                return true;
            }

            newRoot = null;
            return false;
        }

        private static TypeSyntax GenerateTypeSyntax(ITypeSymbol type, OptionSet options)
        {
            return type.ContainsAnonymousType() || (options.GetOption(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals) && type.TypeKind != TypeKind.Delegate)
                ? SyntaxFactory.IdentifierName("var")
                : type.GenerateTypeSyntax();
        }
    }
}
