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

        public readonly bool IsIsOrAsOrSwitchExpressionContext;
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
        public readonly bool IsLeftSideOfImportAliasDirective;

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
            bool isLeftSideOfImportAliasDirective,
            bool isLabelContext,
            bool isTypeArgumentOfConstraintContext,
            bool isRightOfDotOrArrowOrColonColon,
            bool isIsOrAsOrSwitchExpressionContext,
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
            bool isPossibleTupleContext,
            bool isPatternContext,
            bool isRightSideOfNumericType,
            CancellationToken cancellationToken)
            : base(workspace, semanticModel, position, leftToken, targetToken,
                   isTypeContext, isNamespaceContext, isNamespaceDeclarationNameContext,
                   isPreProcessorDirectiveContext,
                   isRightOfDotOrArrowOrColonColon, isStatementContext, isAnyExpressionContext,
                   isAttributeNameContext, isEnumTypeMemberAccessContext, isNameOfContext,
                   isInQuery, isInImportsDirective, IsWithinAsyncMethod(), isPossibleTupleContext,
                   isPatternContext, isRightSideOfNumericType, cancellationToken)
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
            this.IsIsOrAsOrSwitchExpressionContext = isIsOrAsOrSwitchExpressionContext;
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
            this.IsLeftSideOfImportAliasDirective = isLeftSideOfImportAliasDirective;
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

            // Typing a dot after a numeric expression (numericExpression.) 
            // - maybe a start of MemberAccessExpression like numericExpression.Member.
            // - or it maybe a start of a range expression like numericExpression..anotherNumericExpression (starting C# 8.0) 
            // Therefore, in the scenario, we want the completion to be __soft selected__ until user types the next character after the dot.
            // If the second dot was typed, we just insert two dots.
            var isRightSideOfNumericType = leftToken.IsNumericTypeContext(semanticModel, cancellationToken);

            return new CSharpSyntaxContext(
                workspace: workspace,
                semanticModel: semanticModel,
                position: position,
                leftToken: leftToken,
                targetToken: targetToken,
                containingTypeDeclaration: containingTypeDeclaration,
                containingTypeOrEnumDeclaration: containingTypeOrEnumDeclaration,
                isInNonUserCode: isInNonUserCode,
                isPreProcessorDirectiveContext: isPreProcessorDirectiveContext,
                isPreProcessorKeywordContext: isPreProcessorKeywordContext,
                isPreProcessorExpressionContext: isPreProcessorExpressionContext,
                isTypeContext: syntaxTree.IsTypeContext(position, cancellationToken, semanticModelOpt: semanticModel),
                isNamespaceContext: syntaxTree.IsNamespaceContext(position, cancellationToken, semanticModelOpt: semanticModel),
                isNamespaceDeclarationNameContext: syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken),
                isStatementContext: isStatementContext,
                isGlobalStatementContext: isGlobalStatementContext,
                isAnyExpressionContext: isAnyExpressionContext,
                isNonAttributeExpressionContext: isNonAttributeExpressionContext,
                isConstantExpressionContext: isConstantExpressionContext,
                isAttributeNameContext: syntaxTree.IsAttributeNameContext(position, cancellationToken),
                isEnumTypeMemberAccessContext: syntaxTree.IsEnumTypeMemberAccessContext(semanticModel, position, cancellationToken),
                isNameOfContext: syntaxTree.IsNameOfContext(position, semanticModel, cancellationToken),
                isInQuery: leftToken.GetAncestor<QueryExpressionSyntax>() != null,
                isInImportsDirective: leftToken.GetAncestor<UsingDirectiveSyntax>() != null,
                isLeftSideOfImportAliasDirective: IsLeftSideOfUsingAliasDirective(leftToken, cancellationToken),
                isLabelContext: syntaxTree.IsLabelContext(position, cancellationToken),
                isTypeArgumentOfConstraintContext: syntaxTree.IsTypeArgumentOfConstraintClause(position, cancellationToken),
                isRightOfDotOrArrowOrColonColon: syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken),
                isIsOrAsOrSwitchExpressionContext: syntaxTree.IsIsOrAsOrSwitchExpressionContext(semanticModel, position, leftToken, cancellationToken),
                isObjectCreationTypeContext: syntaxTree.IsObjectCreationTypeContext(position, leftToken, cancellationToken),
                isDefiniteCastTypeContext: syntaxTree.IsDefiniteCastTypeContext(position, leftToken, cancellationToken),
                isGenericTypeArgumentContext: syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken),
                isEnumBaseListContext: syntaxTree.IsEnumBaseListContext(position, leftToken, cancellationToken),
                isIsOrAsTypeContext: syntaxTree.IsIsOrAsTypeContext(position, leftToken, cancellationToken),
                isLocalVariableDeclarationContext: syntaxTree.IsLocalVariableDeclarationContext(position, leftToken, cancellationToken),
                isDeclarationExpressionContext: syntaxTree.IsDeclarationExpressionContext(position, leftToken, cancellationToken),
                isFixedVariableDeclarationContext: syntaxTree.IsFixedVariableDeclarationContext(position, leftToken, cancellationToken),
                isParameterTypeContext: syntaxTree.IsParameterTypeContext(position, leftToken, cancellationToken),
                isPossibleLambdaOrAnonymousMethodParameterTypeContext: syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, leftToken, cancellationToken),
                isImplicitOrExplicitOperatorTypeContext: syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, leftToken, cancellationToken),
                isPrimaryFunctionExpressionContext: syntaxTree.IsPrimaryFunctionExpressionContext(position, leftToken, cancellationToken),
                isDelegateReturnTypeContext: syntaxTree.IsDelegateReturnTypeContext(position, leftToken, cancellationToken),
                isTypeOfExpressionContext: syntaxTree.IsTypeOfExpressionContext(position, leftToken, cancellationToken),
                precedingModifiers: syntaxTree.GetPrecedingModifiers(position, leftToken, cancellationToken),
                isInstanceContext: syntaxTree.IsInstanceContext(targetToken, semanticModel, cancellationToken),
                isCrefContext: syntaxTree.IsCrefContext(position, cancellationToken) && !leftToken.IsKind(SyntaxKind.DotToken),
                isCatchFilterContext: syntaxTree.IsCatchFilterContext(position, leftToken),
                isDestructorTypeContext: isDestructorTypeContext,
                isPossibleTupleContext: syntaxTree.IsPossibleTupleContext(leftToken, position),
                isPatternContext: syntaxTree.IsPatternContext(leftToken, position),
                isRightSideOfNumericType: isRightSideOfNumericType,
                cancellationToken: cancellationToken);
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
                    token.SpanStart, contextOpt: null, validModifiers: null, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return true;
            }

            return false;
        }

        public bool IsTypeDeclarationContext(
            ISet<SyntaxKind> validModifiers = null,
            ISet<SyntaxKind> validTypeDeclarations = null,
            bool canBePartial = false,
            CancellationToken cancellationToken = default)
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
            CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Is this a possible position for an await statement (`await using` or `await foreach`)?
        /// </summary>
        internal bool IsAwaitStatementContext(int position, CancellationToken cancellationToken)
        {
            var leftToken = this.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);
            return targetToken.Kind() == SyntaxKind.AwaitKeyword && targetToken.GetPreviousToken().IsBeginningOfStatementContext();
        }
    }
}
