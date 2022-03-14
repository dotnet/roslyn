// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public readonly TypeDeclarationSyntax? ContainingTypeDeclaration;
        public readonly BaseTypeDeclarationSyntax? ContainingTypeOrEnumDeclaration;

        public readonly bool IsInNonUserCode;

        public readonly bool IsPreProcessorKeywordContext;

        public readonly bool IsNonAttributeExpressionContext;
        public readonly bool IsConstantExpressionContext;

        public readonly bool IsLabelContext;
        public readonly bool IsTypeArgumentOfConstraintContext;

        public readonly bool IsIsOrAsOrSwitchOrWithExpressionContext;
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
        public readonly bool IsFunctionPointerTypeArgumentContext;
        public readonly bool IsLocalFunctionDeclarationContext;

        private CSharpSyntaxContext(
            Document document,
            SemanticModel semanticModel,
            int position,
            SyntaxToken leftToken,
            SyntaxToken targetToken,
            TypeDeclarationSyntax? containingTypeDeclaration,
            BaseTypeDeclarationSyntax? containingTypeOrEnumDeclaration,
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
            bool isIsOrAsOrSwitchOrWithExpressionContext,
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
            bool isStartPatternContext,
            bool isAfterPatternContext,
            bool isRightSideOfNumericType,
            bool isInArgumentList,
            bool isFunctionPointerTypeArgumentContext,
            bool isLocalFunctionDeclarationContext,
            CancellationToken cancellationToken)
            : base(document, semanticModel, position, leftToken, targetToken,
                   isTypeContext, isNamespaceContext, isNamespaceDeclarationNameContext,
                   isPreProcessorDirectiveContext, isPreProcessorExpressionContext,
                   isRightOfDotOrArrowOrColonColon, isStatementContext, isGlobalStatementContext,
                   isAnyExpressionContext, isAttributeNameContext, isEnumTypeMemberAccessContext,
                   isNameOfContext, isInQuery, isInImportsDirective, IsWithinAsyncMethod(), isPossibleTupleContext,
                   isStartPatternContext, isAfterPatternContext, isRightSideOfNumericType, isInArgumentList,
                   cancellationToken)
        {
            this.ContainingTypeDeclaration = containingTypeDeclaration;
            this.ContainingTypeOrEnumDeclaration = containingTypeOrEnumDeclaration;
            this.IsInNonUserCode = isInNonUserCode;
            this.IsPreProcessorKeywordContext = isPreProcessorKeywordContext;
            this.IsNonAttributeExpressionContext = isNonAttributeExpressionContext;
            this.IsConstantExpressionContext = isConstantExpressionContext;
            this.IsLabelContext = isLabelContext;
            this.IsTypeArgumentOfConstraintContext = isTypeArgumentOfConstraintContext;
            this.IsIsOrAsOrSwitchOrWithExpressionContext = isIsOrAsOrSwitchOrWithExpressionContext;
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
            this.IsFunctionPointerTypeArgumentContext = isFunctionPointerTypeArgumentContext;
            this.IsLocalFunctionDeclarationContext = isLocalFunctionDeclarationContext;
        }

        public static CSharpSyntaxContext CreateContext(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => CreateContextWorker(document, semanticModel, position, cancellationToken);

        private static CSharpSyntaxContext CreateContextWorker(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
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
                ? syntaxTree.IsPreProcessorKeywordContext(position, leftToken)
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
                ? syntaxTree.IsConstantExpressionContext(position, leftToken)
                : false;

            var containingTypeDeclaration = syntaxTree.GetContainingTypeDeclaration(position, cancellationToken);
            var containingTypeOrEnumDeclaration = syntaxTree.GetContainingTypeOrEnumDeclaration(position, cancellationToken);

            var isDestructorTypeContext = targetToken.IsKind(SyntaxKind.TildeToken) &&
                                            targetToken.Parent.IsKind(SyntaxKind.DestructorDeclaration) &&
                                            targetToken.Parent.Parent.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);

            // Typing a dot after a numeric expression (numericExpression.) 
            // - maybe a start of MemberAccessExpression like numericExpression.Member.
            // - or it maybe a start of a range expression like numericExpression..anotherNumericExpression (starting C# 8.0) 
            // Therefore, in the scenario, we want the completion to be __soft selected__ until user types the next character after the dot.
            // If the second dot was typed, we just insert two dots.
            var isRightSideOfNumericType = leftToken.IsNumericTypeContext(semanticModel, cancellationToken);

            var isArgumentListToken = targetToken.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.AttributeArgumentList, SyntaxKind.ArrayRankSpecifier);

            var isLocalFunctionDeclarationContext = syntaxTree.IsLocalFunctionDeclarationContext(position, cancellationToken);

            return new CSharpSyntaxContext(
                document: document,
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
                isLeftSideOfImportAliasDirective: IsLeftSideOfUsingAliasDirective(leftToken),
                isLabelContext: syntaxTree.IsLabelContext(position, cancellationToken),
                isTypeArgumentOfConstraintContext: syntaxTree.IsTypeArgumentOfConstraintClause(position, cancellationToken),
                isRightOfDotOrArrowOrColonColon: syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken),
                isIsOrAsOrSwitchOrWithExpressionContext: syntaxTree.IsIsOrAsOrSwitchOrWithExpressionContext(semanticModel, position, leftToken, cancellationToken),
                isObjectCreationTypeContext: syntaxTree.IsObjectCreationTypeContext(position, leftToken, cancellationToken),
                isDefiniteCastTypeContext: syntaxTree.IsDefiniteCastTypeContext(position, leftToken),
                isGenericTypeArgumentContext: syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken),
                isEnumBaseListContext: syntaxTree.IsEnumBaseListContext(position, leftToken),
                isIsOrAsTypeContext: syntaxTree.IsIsOrAsTypeContext(position, leftToken),
                isLocalVariableDeclarationContext: syntaxTree.IsLocalVariableDeclarationContext(position, leftToken, cancellationToken),
                isDeclarationExpressionContext: syntaxTree.IsDeclarationExpressionContext(position, leftToken),
                isFixedVariableDeclarationContext: syntaxTree.IsFixedVariableDeclarationContext(position, leftToken),
                isParameterTypeContext: syntaxTree.IsParameterTypeContext(position, leftToken),
                isPossibleLambdaOrAnonymousMethodParameterTypeContext: syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, leftToken, cancellationToken),
                isImplicitOrExplicitOperatorTypeContext: syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, leftToken),
                isPrimaryFunctionExpressionContext: syntaxTree.IsPrimaryFunctionExpressionContext(position, leftToken),
                isDelegateReturnTypeContext: syntaxTree.IsDelegateReturnTypeContext(position, leftToken),
                isTypeOfExpressionContext: syntaxTree.IsTypeOfExpressionContext(position, leftToken),
                precedingModifiers: syntaxTree.GetPrecedingModifiers(position, cancellationToken),
                isInstanceContext: syntaxTree.IsInstanceContext(targetToken, semanticModel, cancellationToken),
                isCrefContext: syntaxTree.IsCrefContext(position, cancellationToken) && !leftToken.IsKind(SyntaxKind.DotToken),
                isCatchFilterContext: syntaxTree.IsCatchFilterContext(position, leftToken),
                isDestructorTypeContext: isDestructorTypeContext,
                isPossibleTupleContext: syntaxTree.IsPossibleTupleContext(leftToken, position),
                isStartPatternContext: syntaxTree.IsAtStartOfPattern(leftToken, position),
                isAfterPatternContext: syntaxTree.IsAtEndOfPattern(leftToken, position),
                isRightSideOfNumericType: isRightSideOfNumericType,
                isInArgumentList: isArgumentListToken,
                isFunctionPointerTypeArgumentContext: syntaxTree.IsFunctionPointerTypeArgumentContext(position, leftToken, cancellationToken),
                isLocalFunctionDeclarationContext: isLocalFunctionDeclarationContext,
                cancellationToken: cancellationToken);
        }

        private static new bool IsWithinAsyncMethod()
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
                token.Parent.IsKind(SyntaxKind.AttributeList) &&
                this.SyntaxTree.IsTypeDeclarationContext(
                    token.SpanStart, contextOpt: null, validModifiers: null, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return true;
            }

            return false;
        }

        public bool IsTypeDeclarationContext(
            ISet<SyntaxKind>? validModifiers = null,
            ISet<SyntaxKind>? validTypeDeclarations = null,
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
                token.Parent.IsKind(SyntaxKind.AttributeList))
            {
                if (token.Parent.Parent is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } })
                {
                    return true;
                }

                if (SyntaxTree.IsMemberDeclarationContext(
                    token.SpanStart, contextOpt: null, validModifiers: null, validTypeDeclarations: validTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsStatementAttributeContext()
        {
            var token = TargetToken;

            if (token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent.IsKind(SyntaxKind.AttributeList) &&
                token.Parent.Parent is StatementSyntax)
            {
                return true;
            }

            return false;
        }

        public bool IsMemberDeclarationContext(
            ISet<SyntaxKind>? validModifiers = null,
            ISet<SyntaxKind>? validTypeDeclarations = null,
            bool canBePartial = false,
            CancellationToken cancellationToken = default)
        {
            return this.SyntaxTree.IsMemberDeclarationContext(this.Position, this, validModifiers, validTypeDeclarations, canBePartial, cancellationToken);
        }

        private static bool IsLeftSideOfUsingAliasDirective(SyntaxToken leftToken)
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

        /// <summary>
        /// Is this a possible position for an await statement (`await using` or `await foreach`)?
        /// </summary>
        internal bool IsAwaitStatementContext(int position, CancellationToken cancellationToken)
        {
            var leftToken = this.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);
            if (targetToken.IsKind(SyntaxKind.AwaitKeyword))
            {
                var previousToken = targetToken.GetPreviousToken();
                if (previousToken.IsBeginningOfStatementContext())
                {
                    return true;
                }

                return SyntaxTree.IsGlobalStatementContext(targetToken.SpanStart, cancellationToken);
            }
            else if (SyntaxTree.IsScript()
                && targetToken.IsKind(SyntaxKind.IdentifierToken)
                && targetToken.HasMatchingText(SyntaxKind.AwaitKeyword))
            {
                // The 'await' keyword is parsed as an identifier in C# script
                return SyntaxTree.IsGlobalStatementContext(targetToken.SpanStart, cancellationToken);
            }

            return false;
        }

        /// <summary>
        /// Determines whether await should be suggested in a given position.
        /// </summary>
        internal override bool IsAwaitKeywordContext()
        {
            if (IsGlobalStatementContext)
            {
                return true;
            }

            if (IsAnyExpressionContext || IsStatementContext)
            {
                foreach (var node in LeftToken.GetAncestors<SyntaxNode>())
                {
                    if (node.IsAnyLambdaOrAnonymousMethod())
                    {
                        return true;
                    }

                    if (node.IsKind(SyntaxKind.QueryExpression))
                    {
                        // There are some cases where "await" is allowed in a query context. See error CS1995 for details:
                        // error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                        if (TargetToken.IsKind(SyntaxKind.InKeyword))
                        {
                            return TargetToken.Parent switch
                            {
                                FromClauseSyntax { Parent: QueryExpressionSyntax queryExpression } fromClause => queryExpression.FromClause == fromClause,
                                JoinClauseSyntax => true,
                                _ => false,
                            };
                        }

                        return false;
                    }

                    if (node.IsKind(SyntaxKind.LockStatement, out LockStatementSyntax? lockStatement))
                    {
                        if (lockStatement.Statement != null &&
                            !lockStatement.Statement.IsMissing &&
                            lockStatement.Statement.Span.Contains(Position))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}
