// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Compilers;
using Microsoft.CodeAnalysis.Compilers.Common;
using Microsoft.CodeAnalysis.Compilers.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Services.CSharp.Extensions;
using Microsoft.CodeAnalysis.Services.Editor.Implementation.GenerateMember.GenerateFieldOrProperty;
using Microsoft.CodeAnalysis.Services.Shared.CodeGeneration;
using Microsoft.CodeAnalysis.Services.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Services.Editor.CSharp.GenerateMember.GenerateFieldOrProperty
{
    [ExportLanguageService(typeof(IGenerateFieldOrPropertyService), LanguageNames.CSharp)]
    internal partial class CSharpGenerateFieldOrPropertyService :
        AbstractGenerateFieldOrPropertyService<CSharpGenerateFieldOrPropertyService, SimpleNameSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpGenerateFieldOrPropertyService(
            ILanguageServiceProviderFactory languageServiceProviderFactory,
            ICodeDefinitionFactory codeDefinitionFactory)
            : base(languageServiceProviderFactory, codeDefinitionFactory)
        {
        }

        protected override bool IsExplicitInterfaceGeneration(CommonSyntaxNode node)
        {
            return node is PropertyDeclarationSyntax;
        }

        protected override bool IsIdentifierNameGeneration(CommonSyntaxNode node)
        {
            return node is IdentifierNameSyntax;
        }

        protected override bool TryInitializeExplicitInterfaceState(
            IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken,
            out CommonSyntaxToken identifierToken, out IPropertySymbol propertySymbol, out INamedTypeSymbol typeToGenerateIn)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)node;
            identifierToken = propertyDeclaration.Identifier;

            if (propertyDeclaration.ExplicitInterfaceSpecifier != null)
            {
                var semanticModel = document.GetSemanticModel(cancellationToken);
                propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken) as IPropertySymbol;
                if (propertySymbol != null && !propertySymbol.ExplicitInterfaceImplementations.Any())
                {
                    var info = semanticModel.GetTypeInfo(propertyDeclaration.ExplicitInterfaceSpecifier.Name, cancellationToken);
                    typeToGenerateIn = info.Type as INamedTypeSymbol;
                    return typeToGenerateIn != null;
                }
            }

            identifierToken = default(CommonSyntaxToken);
            propertySymbol = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeIdentifierNameState(
            IDocument document, SimpleNameSyntax identifierName, CancellationToken cancellationToken,
            out CommonSyntaxToken identifierToken, out ExpressionSyntax simpleNameOrMemberAccessExpression)
        {
            identifierToken = identifierName.Identifier;
            if (identifierToken.ValueText != string.Empty &&
                !identifierName.IsVar)
            {
                var memberAccess = identifierName.Parent as MemberAccessExpressionSyntax;
                simpleNameOrMemberAccessExpression = memberAccess != null && memberAccess.Name == identifierName
                    ? (ExpressionSyntax)memberAccess
                    : identifierName;

                // If we're being invoked, then don't offer this, offer generate method instead.
                // Note: we could offer to generate a field with a delegate type.  However, that's
                // very esoteric and probably not what most users want.
                if (!IsLegal(simpleNameOrMemberAccessExpression))
                {
                    return false;
                }
#if false
                if (simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.ObjectCreationExpression) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.GotoStatement) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.AliasQualifiedName) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.NameColon) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.QualifiedName) ||
                    simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.IncompleteMember) ||
                    identifierName.getan.IsParentKind(SyntaxKind.UsingDirective))
                {
                    return false;
                }
#endif

                return true;
            }

            identifierToken = default(CommonSyntaxToken);
            simpleNameOrMemberAccessExpression = null;
            return false;
        }

        private bool IsLegal(ExpressionSyntax expression)
        {
            if (expression.IsParentKind(SyntaxKind.ExpressionStatement) ||
                (expression.IsParentKind(SyntaxKind.NameEquals) && expression.Parent.IsParentKind(SyntaxKind.AttributeArgument)) ||
                expression.IsLeftSideOfAnyAssignExpression() ||
                expression.IsParentKind(SyntaxKind.EqualsValueClause) ||
                expression.IsParentKind(SyntaxKind.Argument) ||
                expression.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator) ||
                expression.Parent is PrefixUnaryExpressionSyntax ||
                expression.Parent is PostfixUnaryExpressionSyntax ||
                expression.Parent is BinaryExpressionSyntax ||
                expression.Parent is AssignmentExpressionSyntax ||
                expression.CheckParent<ForEachStatementSyntax>(f => f.Expression == expression) ||
                expression.CheckParent<MemberAccessExpressionSyntax>(m => m.Expression == expression) ||
                expression.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                expression.IsParentKind(SyntaxKind.SimpleLambdaExpression))
            {
                return true;
            }

            return false;
        }
    }
}
