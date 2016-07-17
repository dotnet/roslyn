// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal sealed class CSharpSyntaxContext : SyntaxContext
    {
        public readonly TypeDeclarationSyntax ContainingTypeDeclaration;
        public readonly BaseTypeDeclarationSyntax ContainingTypeOrEnumDeclaration;

        public readonly bool IsInNonUserCode;

        public readonly bool IsPreProcessorKeywordContext;
        public readonly bool IsPreProcessorExpressionContext;

        public readonly bool IsGlobalStatementContext;

        public readonly bool IsNonAttributeExpressionContext;
        public readonly bool IsConstantExpressionContext;

        public readonly bool IsLabelContext;
        public readonly bool IsTypeArgumentOfConstraintContext;

        public readonly bool IsIsOrAsContext;
        public readonly bool IsObjectCreationTypeContext;
        public readonly bool IsDefiniteCastTypeContext;
        public readonly bool IsGenericTypeArgumentContext;
        public readonly bool IsEnumBaseListContext;
        public readonly bool IsIsOrAsTypeContext;
        public readonly bool IsLocalVariableDeclarationContext;
        public readonly bool IsDeclarationExpressionContext;
        public readonly bool IsFixedVariableDeclarationContext;
        public readonly bool IsParameterTypeContext;
        public readonly bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext;
        public readonly bool IsImplicitOrExplicitOperatorTypeContext;
        public readonly bool IsPrimaryFunctionExpressionContext;
        public readonly bool IsDelegateReturnTypeContext;
        public readonly bool IsTypeOfExpressionContext;
        public readonly ISet<SyntaxKind> PrecedingModifiers;
        public readonly bool IsInstanceContext;
        public readonly bool IsCrefContext;
        public readonly bool IsCatchFilterContext;
        public readonly bool IsDestructorTypeContext;

        private CSharpSyntaxContext(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            SyntaxToken leftToken,
            SyntaxToken targetToken,
            TypeDeclarationSyntax containingTypeDeclaration,
            BaseTypeDeclarationSyntax containingTypeOrEnumDeclaration,
            bool isInNonUserCode,
            bool isPreProcessorDirectiveContext,
            bool isPreProcessorKeywordContext,
            bool isPreProcessorExpressionContext,
            bool isTypeContext,
            bool isNamespaceContext,
            bool isNamespaceDeclarationNameContext,
            bool isStatementContext,
            bool isGlobalStatementContext,
            bool isAnyExpressionContext,
            bool isNonAttributeExpressionContext,
            bool isConstantExpressionContext,
            bool isAttributeNameContext,
            bool isEnumTypeMemberAccessContext,
            bool isNameOfContext,
            bool isInQuery,
            bool isInImportsDirective,
            bool isLabelContext,
            bool isTypeArgumentOfConstraintContext,
            bool isRightOfDotOrArrowOrColonColon,
            bool isIsOrAsContext,
            bool isObjectCreationTypeContext,
            bool isDefiniteCastTypeContext,
            bool isGenericTypeArgumentContext,
            bool isEnumBaseListContext,
            bool isIsOrAsTypeContext,
            bool isLocalVariableDeclarationContext,
            bool isDeclarationExpressionContext,
            bool isFixedVariableDeclarationContext,
            bool isParameterTypeContext,
            bool isPossibleLambdaOrAnonymousMethodParameterTypeContext,
            bool isImplicitOrExplicitOperatorTypeContext,
            bool isPrimaryFunctionExpressionContext,
            bool isDelegateReturnTypeContext,
            bool isTypeOfExpressionContext,
            ISet<SyntaxKind> precedingModifiers,
            bool isInstanceContext,
            bool isCrefContext,
            bool isCatchFilterContext,
            bool isDestructorTypeContext,
            CancellationToken cancellationToken)
            : base(workspace, semanticModel, position, leftToken, targetToken,
                   isTypeContext, isNamespaceContext, isNamespaceDeclarationNameContext,
                   isPreProcessorDirectiveContext,
                   isRightOfDotOrArrowOrColonColon, isStatementContext, isAnyExpressionContext,
                   isAttributeNameContext, isEnumTypeMemberAccessContext, isNameOfContext,
                   isInQuery, isInImportsDirective, IsWithinAsyncMethod(), cancellationToken)
        {
            this.ContainingTypeDeclaration = containingTypeDeclaration;
            this.ContainingTypeOrEnumDeclaration = containingTypeOrEnumDeclaration;
            this.IsInNonUserCode = isInNonUserCode;
            this.IsPreProcessorKeywordContext = isPreProcessorKeywordContext;
            this.IsPreProcessorExpressionContext = isPreProcessorExpressionContext;
            this.IsGlobalStatementContext = isGlobalStatementContext;
            this.IsNonAttributeExpressionContext = isNonAttributeExpressionContext;
            this.IsConstantExpressionContext = isConstantExpressionContext;
            this.IsLabelContext = isLabelContext;
            this.IsTypeArgumentOfConstraintContext = isTypeArgumentOfConstraintContext;
            this.IsIsOrAsContext = isIsOrAsContext;
            this.IsObjectCreationTypeContext = isObjectCreationTypeContext;
            this.IsDefiniteCastTypeContext = isDefiniteCastTypeContext;
            this.IsGenericTypeArgumentContext = isGenericTypeArgumentContext;
            this.IsEnumBaseListContext = isEnumBaseListContext;
            this.IsIsOrAsTypeContext = isIsOrAsTypeContext;
            this.IsLocalVariableDeclarationContext = isLocalVariableDeclarationContext;
            this.IsDeclarationExpressionContext = isDeclarationExpressionContext;
            this.IsFixedVariableDeclarationContext = isFixedVariableDeclarationContext;
            this.IsParameterTypeContext = isParameterTypeContext;
            this.IsPossibleLambdaOrAnonymousMethodParameterTypeContext = isPossibleLambdaOrAnonymousMethodParameterTypeContext;
            this.IsImplicitOrExplicitOperatorTypeContext = isImplicitOrExplicitOperatorTypeContext;
            this.IsPrimaryFunctionExpressionContext = isPrimaryFunctionExpressionContext;
            this.IsDelegateReturnTypeContext = isDelegateReturnTypeContext;
            this.IsTypeOfExpressionContext = isTypeOfExpressionContext;
            this.PrecedingModifiers = precedingModifiers;
            this.IsInstanceContext = isInstanceContext;
            this.IsCrefContext = isCrefContext;
            this.IsCatchFilterContext = isCatchFilterContext;
            this.IsDestructorTypeContext = isDestructorTypeContext;
        }

        public static CSharpSyntaxContext CreateContext(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return CreateContextWorker(workspace, semanticModel, position, cancellationToken);
        }

        private static CSharpSyntaxContext CreateContextWorker(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = semanticModel.SyntaxTree;

            var isInNonUserCode = syntaxTree.IsInNonUserCode(position, cancellationToken);

            var preProcessorTokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);
            var isPreProcessorDirectiveContext = syntaxTree.IsPreProcessorDirectiveContext(position, preProcessorTokenOnLeftOfPosition, cancellationToken);

            var leftToken = isPreProcessorDirectiveContext
                ? preProcessorTokenOnLeftOfPosition
                : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);

            var isPreProcessorKeywordContext = isPreProcessorDirectiveContext
                ? syntaxTree.IsPreProcessorKeywordContext(position, leftToken, cancellationToken)
                : false;

            var isPreProcessorExpressionContext = isPreProcessorDirectiveContext
                ? targetToken.IsPreProcessorExpressionContext()
                : false;

            var isStatementContext = !isPreProcessorDirectiveContext
                ? targetToken.IsBeginningOfStatementContext()
                : false;

            var isGlobalStatementContext = !isPreProcessorDirectiveContext
                ? syntaxTree.IsGlobalStatementContext(position, cancellationToken)
                : false;

            var isAnyExpressionContext = !isPreProcessorDirectiveContext
                ? syntaxTree.IsExpressionContext(position, leftToken, attributes: true, cancellationToken: cancellationToken, semanticModelOpt: semanticModel)
                : false;

            var isNonAttributeExpressionContext = !isPreProcessorDirectiveContext
                ? syntaxTree.IsExpressionContext(position, leftToken, attributes: false, cancellationToken: cancellationToken, semanticModelOpt: semanticModel)
                : false;

            var isConstantExpressionContext = !isPreProcessorDirectiveContext
                ? syntaxTree.IsConstantExpressionContext(position, leftToken, cancellationToken)
                : false;

            var containingTypeDeclaration = syntaxTree.GetContainingTypeDeclaration(position, cancellationToken);
            var containingTypeOrEnumDeclaration = syntaxTree.GetContainingTypeOrEnumDeclaration(position, cancellationToken);

            var isDestructorTypeContext = targetToken.IsKind(SyntaxKind.TildeToken) &&
                                            targetToken.Parent.IsKind(SyntaxKind.DestructorDeclaration) &&
                                            targetToken.Parent.Parent.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

            return new CSharpSyntaxContext(
                workspace,
                semanticModel,
                position,
                leftToken,
                targetToken,
                containingTypeDeclaration,
                containingTypeOrEnumDeclaration,
                isInNonUserCode,
                isPreProcessorDirectiveContext,
                isPreProcessorKeywordContext,
                isPreProcessorExpressionContext,
                syntaxTree.IsTypeContext(position, cancellationToken, semanticModelOpt: semanticModel),
                syntaxTree.IsNamespaceContext(position, cancellationToken, semanticModelOpt: semanticModel),
                syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken),
                isStatementContext,
                isGlobalStatementContext,
                isAnyExpressionContext,
                isNonAttributeExpressionContext,
                isConstantExpressionContext,
                syntaxTree.IsAttributeNameContext(position, cancellationToken),
                syntaxTree.IsEnumTypeMemberAccessContext(semanticModel, position, cancellationToken),
                syntaxTree.IsNameOfContext(position, semanticModel, cancellationToken),
                leftToken.GetAncestor<QueryExpressionSyntax>() != null,
                IsLeftSideOfUsingAliasDirective(leftToken, cancellationToken),
                syntaxTree.IsLabelContext(position, cancellationToken),
                syntaxTree.IsTypeArgumentOfConstraintClause(position, cancellationToken),
                syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken),
                syntaxTree.IsIsOrAsContext(position, leftToken, cancellationToken),
                syntaxTree.IsObjectCreationTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsDefiniteCastTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken),
                syntaxTree.IsEnumBaseListContext(position, leftToken, cancellationToken),
                syntaxTree.IsIsOrAsTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsLocalVariableDeclarationContext(position, leftToken, cancellationToken),
                syntaxTree.IsDeclarationExpressionContext(position, leftToken, cancellationToken),
                syntaxTree.IsFixedVariableDeclarationContext(position, leftToken, cancellationToken),
                syntaxTree.IsParameterTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsPrimaryFunctionExpressionContext(position, leftToken, cancellationToken),
                syntaxTree.IsDelegateReturnTypeContext(position, leftToken, cancellationToken),
                syntaxTree.IsTypeOfExpressionContext(position, leftToken, cancellationToken),
                syntaxTree.GetPrecedingModifiers(position, leftToken, cancellationToken),
                syntaxTree.IsInstanceContext(position, leftToken, cancellationToken),
                syntaxTree.IsCrefContext(position, cancellationToken) && !leftToken.IsKind(SyntaxKind.DotToken),
                syntaxTree.IsCatchFilterContext(position, leftToken),
                isDestructorTypeContext,
                cancellationToken);
        }

        public static CSharpSyntaxContext CreateContext_Test(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var inferenceService = new CSharpTypeInferenceService();
            var types = inferenceService.InferTypes(semanticModel, position, cancellationToken);
            return CreateContextWorker(workspace: null, semanticModel: semanticModel, position: position, cancellationToken: cancellationToken);
        }

        private new static bool IsWithinAsyncMethod()
        {
            // TODO: Implement this if any C# completion code needs to know if it is in an async 
            // method or not.
            return false;
        }

        public bool IsTypeAttributeContext(CancellationToken cancellationToken)
        {
            // cases:
            //    [ |
            //    class C { [ |
            var token = this.TargetToken;

            // Note that we pass the token.SpanStart to IsTypeDeclarationContext below. This is a bit subtle,
            // but we want to be sure that the attribute itself (i.e. the open square bracket, '[') is in a
            // type declaration context.
            if (token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent.Kind() == SyntaxKind.AttributeList &&
                this.SyntaxTree.IsTypeDeclarationContext(
                    token.SpanStart, contextOpt: null, validModifiers: null, validTypeDeclarations: SyntaxKindSet.ClassStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return true;
            }

            return false;
        }

        public bool IsTypeDeclarationContext(
            ISet<SyntaxKind> validModifiers = null,
            ISet<SyntaxKind> validTypeDeclarations = null,
            bool canBePartial = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SyntaxTree.IsTypeDeclarationContext(this.Position, this, validModifiers, validTypeDeclarations, canBePartial, cancellationToken);
        }

        public bool IsMemberAttributeContext(ISet<SyntaxKind> validTypeDeclarations, CancellationToken cancellationToken)
        {
            // cases:
            //   class C { [ |
            var token = this.TargetToken;

            if (token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent.Kind() == SyntaxKind.AttributeList &&
                this.SyntaxTree.IsMemberDeclarationContext(
                    token.SpanStart, contextOpt: null, validModifiers: null, validTypeDeclarations: validTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return true;
            }

            return false;
        }

        public bool IsMemberDeclarationContext(
            ISet<SyntaxKind> validModifiers = null,
            ISet<SyntaxKind> validTypeDeclarations = null,
            bool canBePartial = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.SyntaxTree.IsMemberDeclarationContext(this.Position, this, validModifiers, validTypeDeclarations, canBePartial, cancellationToken);
        }

        private static bool IsLeftSideOfUsingAliasDirective(SyntaxToken leftToken, CancellationToken cancellationToken)
        {
            var usingDirective = leftToken.GetAncestor<UsingDirectiveSyntax>();

            if (usingDirective != null)
            {
                // No = token: 
                if (usingDirective.Alias == null || usingDirective.Alias.EqualsToken.IsMissing)
                {
                    return true;
                }

                return leftToken.SpanStart < usingDirective.Alias.EqualsToken.SpanStart;
            }

            return false;
        }

        internal override ITypeInferenceService GetTypeInferenceServiceWithoutWorkspace()
        {
            return new CSharpTypeInferenceService();
        }
    }
}
