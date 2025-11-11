// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

internal sealed class CSharpSyntaxContext : SyntaxContext
{
    public readonly TypeDeclarationSyntax? ContainingTypeDeclaration;
    public readonly BaseTypeDeclarationSyntax? ContainingTypeOrEnumDeclaration;

    public readonly bool IsCatchFilterContext;
    public readonly bool IsConstantExpressionContext;
    public readonly bool IsCrefContext;
    public readonly bool IsDefiniteCastTypeContext;
    public readonly bool IsDelegateReturnTypeContext;
    public readonly bool IsDestructorTypeContext;
    public readonly bool IsFixedVariableDeclarationContext;
    public readonly bool IsFunctionPointerTypeArgumentContext;
    public readonly bool IsGenericTypeArgumentContext;
    public readonly bool IsImplicitOrExplicitOperatorTypeContext;
    public readonly bool IsInNonUserCode;
    public readonly bool IsInstanceContext;
    public readonly bool IsIsOrAsOrSwitchOrWithExpressionContext;
    public readonly bool IsIsOrAsTypeContext;
    public readonly bool IsLabelContext;
    public readonly bool IsLeftSideOfImportAliasDirective;
    public readonly bool IsLocalFunctionDeclarationContext;
    public readonly bool IsLocalVariableDeclarationContext;
    public readonly bool IsNonAttributeExpressionContext;
    public readonly bool IsParameterTypeContext;
    public readonly bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext;
    public readonly bool IsPreProcessorKeywordContext;
    public readonly bool IsPrimaryFunctionExpressionContext;
    public readonly bool IsTypeArgumentOfConstraintContext;
    public readonly bool IsTypeOfExpressionContext;
    public readonly bool IsUsingAliasTypeContext;

    public readonly ISet<SyntaxKind> PrecedingModifiers;

    private AttributeTargets? _lazyValidAttributeTargets;

    private CSharpSyntaxContext(
        Document document,
        SemanticModel semanticModel,
        int position,
        SyntaxToken leftToken,
        SyntaxToken targetToken,
        TypeDeclarationSyntax? containingTypeDeclaration,
        BaseTypeDeclarationSyntax? containingTypeOrEnumDeclaration,
        bool isAnyExpressionContext,
        bool isAtEndOfPattern,
        bool isAtStartOfPattern,
        bool isAttributeNameContext,
        bool isAwaitKeywordContext,
        bool isBaseListContext,
        bool isCatchFilterContext,
        bool isConstantExpressionContext,
        bool isCrefContext,
        bool isDefiniteCastTypeContext,
        bool isDelegateReturnTypeContext,
        bool isDestructorTypeContext,
        bool isEnumBaseListContext,
        bool isEnumTypeMemberAccessContext,
        bool isFixedVariableDeclarationContext,
        bool isFunctionPointerTypeArgumentContext,
        bool isGenericConstraintContext,
        bool isGenericTypeArgumentContext,
        bool isGlobalStatementContext,
        bool isImplicitOrExplicitOperatorTypeContext,
        bool isInImportsDirective,
        bool isInNonUserCode,
        bool isInQuery,
        bool isInstanceContext,
        bool isTaskLikeTypeContext,
        bool isIsOrAsOrSwitchOrWithExpressionContext,
        bool isIsOrAsTypeContext,
        bool isLabelContext,
        bool isLeftSideOfImportAliasDirective,
        bool isLocalFunctionDeclarationContext,
        bool isLocalVariableDeclarationContext,
        bool isNameOfContext,
        bool isNamespaceContext,
        bool isNamespaceDeclarationNameContext,
        bool isNonAttributeExpressionContext,
        bool isObjectCreationTypeContext,
        bool isOnArgumentListBracketOrComma,
        bool isParameterTypeContext,
        bool isPossibleLambdaOrAnonymousMethodParameterTypeContext,
        bool isPossibleTupleContext,
        bool isPreProcessorDirectiveContext,
        bool isPreProcessorExpressionContext,
        bool isPreProcessorKeywordContext,
        bool isPrimaryFunctionExpressionContext,
        bool isRightAfterUsingOrImportDirective,
        bool isRightOfNameSeparator,
        bool isRightSideOfNumericType,
        bool isStatementContext,
        bool isTypeArgumentOfConstraintContext,
        bool isTypeContext,
        bool isTypeOfExpressionContext,
        bool isUsingAliasTypeContext,
        bool isWithinAsyncMethod,
        ISet<SyntaxKind> precedingModifiers,
        CancellationToken cancellationToken)
        : base(
              document,
              semanticModel,
              position,
              leftToken,
              targetToken,
              isAnyExpressionContext: isAnyExpressionContext,
              isAtEndOfPattern: isAtEndOfPattern,
              isAtStartOfPattern: isAtStartOfPattern,
              isAttributeNameContext: isAttributeNameContext,
              isAwaitKeywordContext: isAwaitKeywordContext,
              isBaseListContext: isBaseListContext,
              isEnumBaseListContext: isEnumBaseListContext,
              isEnumTypeMemberAccessContext: isEnumTypeMemberAccessContext,
              isGenericConstraintContext: isGenericConstraintContext,
              isGlobalStatementContext: isGlobalStatementContext,
              isInImportsDirective: isInImportsDirective,
              isInQuery: isInQuery,
              isTaskLikeTypeContext: isTaskLikeTypeContext,
              isNameOfContext: isNameOfContext,
              isNamespaceContext: isNamespaceContext,
              isNamespaceDeclarationNameContext: isNamespaceDeclarationNameContext,
              isObjectCreationTypeContext: isObjectCreationTypeContext,
              isOnArgumentListBracketOrComma: isOnArgumentListBracketOrComma,
              isPossibleTupleContext: isPossibleTupleContext,
              isPreProcessorDirectiveContext: isPreProcessorDirectiveContext,
              isPreProcessorExpressionContext: isPreProcessorExpressionContext,
              isRightAfterUsingOrImportDirective: isRightAfterUsingOrImportDirective,
              isRightOfNameSeparator: isRightOfNameSeparator,
              isRightSideOfNumericType: isRightSideOfNumericType,
              isStatementContext: isStatementContext,
              isTypeContext: isTypeContext,
              isWithinAsyncMethod: isWithinAsyncMethod,
              cancellationToken)
    {
        this.ContainingTypeDeclaration = containingTypeDeclaration;
        this.ContainingTypeOrEnumDeclaration = containingTypeOrEnumDeclaration;

        this.IsCatchFilterContext = isCatchFilterContext;
        this.IsConstantExpressionContext = isConstantExpressionContext;
        this.IsCrefContext = isCrefContext;
        this.IsDefiniteCastTypeContext = isDefiniteCastTypeContext;
        this.IsDelegateReturnTypeContext = isDelegateReturnTypeContext;
        this.IsDestructorTypeContext = isDestructorTypeContext;
        this.IsFixedVariableDeclarationContext = isFixedVariableDeclarationContext;
        this.IsFunctionPointerTypeArgumentContext = isFunctionPointerTypeArgumentContext;
        this.IsGenericTypeArgumentContext = isGenericTypeArgumentContext;
        this.IsImplicitOrExplicitOperatorTypeContext = isImplicitOrExplicitOperatorTypeContext;
        this.IsInNonUserCode = isInNonUserCode;
        this.IsInstanceContext = isInstanceContext;
        this.IsIsOrAsOrSwitchOrWithExpressionContext = isIsOrAsOrSwitchOrWithExpressionContext;
        this.IsIsOrAsTypeContext = isIsOrAsTypeContext;
        this.IsLabelContext = isLabelContext;
        this.IsLeftSideOfImportAliasDirective = isLeftSideOfImportAliasDirective;
        this.IsLocalFunctionDeclarationContext = isLocalFunctionDeclarationContext;
        this.IsLocalVariableDeclarationContext = isLocalVariableDeclarationContext;
        this.IsNonAttributeExpressionContext = isNonAttributeExpressionContext;
        this.IsParameterTypeContext = isParameterTypeContext;
        this.IsPossibleLambdaOrAnonymousMethodParameterTypeContext = isPossibleLambdaOrAnonymousMethodParameterTypeContext;
        this.IsPreProcessorKeywordContext = isPreProcessorKeywordContext;
        this.IsPrimaryFunctionExpressionContext = isPrimaryFunctionExpressionContext;
        this.IsTypeArgumentOfConstraintContext = isTypeArgumentOfConstraintContext;
        this.IsTypeOfExpressionContext = isTypeOfExpressionContext;
        this.IsUsingAliasTypeContext = isUsingAliasTypeContext;

        this.PrecedingModifiers = precedingModifiers;
    }

    public override AttributeTargets ValidAttributeTargets
    {
        get
        {
            if (!_lazyValidAttributeTargets.HasValue)
            {
                _lazyValidAttributeTargets = ComputeValidAttributeTargets();
            }

            return _lazyValidAttributeTargets.Value;
        }
    }

    public static CSharpSyntaxContext CreateContext(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        => CreateContextWorker(document, semanticModel, position, cancellationToken);

    private static CSharpSyntaxContext CreateContextWorker(
        Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        var syntaxTree = semanticModel.SyntaxTree;

        var isInNonUserCode = syntaxTree.IsInNonUserCode(position, cancellationToken);

        var preProcessorTokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);
        var isPreProcessorDirectiveContext = syntaxTree.IsPreProcessorDirectiveContext(position, preProcessorTokenOnLeftOfPosition, cancellationToken);

        var leftToken = isPreProcessorDirectiveContext
            ? preProcessorTokenOnLeftOfPosition
            : syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

        var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        var isPreProcessorKeywordContext = isPreProcessorDirectiveContext && syntaxTree.IsPreProcessorKeywordContext(position, leftToken);
        var isPreProcessorExpressionContext = isPreProcessorDirectiveContext && targetToken.IsPreProcessorExpressionContext();

        var isStatementContext = !isPreProcessorDirectiveContext && targetToken.IsBeginningOfStatementContext();
        var isGlobalStatementContext = !isPreProcessorDirectiveContext && syntaxTree.IsGlobalStatementContext(position, cancellationToken);
        var isAnyExpressionContext = !isPreProcessorDirectiveContext && syntaxTree.IsExpressionContext(position, leftToken, attributes: true, cancellationToken: cancellationToken, semanticModel: semanticModel);
        var isNonAttributeExpressionContext = !isPreProcessorDirectiveContext && syntaxTree.IsExpressionContext(position, leftToken, attributes: false, cancellationToken: cancellationToken, semanticModel: semanticModel);
        var isConstantExpressionContext = !isPreProcessorDirectiveContext && syntaxTree.IsConstantExpressionContext(position, leftToken);

        var containingTypeDeclaration = syntaxTree.GetContainingTypeDeclaration(position, cancellationToken);
        var containingTypeOrEnumDeclaration = syntaxTree.GetContainingTypeOrEnumDeclaration(position, cancellationToken);

        var isDestructorTypeContext =
            targetToken.IsKind(SyntaxKind.TildeToken) &&
            targetToken.Parent.IsKind(SyntaxKind.DestructorDeclaration) &&
            targetToken.Parent.Parent is (kind: SyntaxKind.ClassDeclaration or SyntaxKind.RecordDeclaration);

        // Typing a dot after a numeric expression (numericExpression.) 
        // - maybe a start of MemberAccessExpression like numericExpression.Member.
        // - or it maybe a start of a range expression like numericExpression..anotherNumericExpression (starting C# 8.0) 
        // Therefore, in the scenario, we want the completion to be __soft selected__ until user types the next character after the dot.
        // If the second dot was typed, we just insert two dots.
        var isRightSideOfNumericType = leftToken.IsNumericTypeContext(semanticModel, cancellationToken);

        var isOnArgumentListBracketOrComma = targetToken.Parent is (kind: SyntaxKind.ArgumentList or SyntaxKind.AttributeArgumentList or SyntaxKind.ArrayRankSpecifier);

        var isLocalFunctionDeclarationContext = syntaxTree.IsLocalFunctionDeclarationContext(position, cancellationToken);

        var precedingModifiers = syntaxTree.GetPrecedingModifiers(position, cancellationToken);

        return new CSharpSyntaxContext(
            document: document,
            semanticModel: semanticModel,
            position: position,
            leftToken: leftToken,
            targetToken: targetToken,
            containingTypeDeclaration: containingTypeDeclaration,
            containingTypeOrEnumDeclaration: containingTypeOrEnumDeclaration,
            isAnyExpressionContext: isAnyExpressionContext,
            isAtEndOfPattern: syntaxTree.IsAtEndOfPattern(leftToken, position),
            isAtStartOfPattern: syntaxTree.IsAtStartOfPattern(leftToken, position),
            isAttributeNameContext: syntaxTree.IsAttributeNameContext(position, cancellationToken),
            isAwaitKeywordContext: ComputeIsAwaitKeywordContext(position, leftToken, targetToken, isGlobalStatementContext, isAnyExpressionContext, isStatementContext),
            isBaseListContext: syntaxTree.IsBaseListContext(targetToken),
            isCatchFilterContext: syntaxTree.IsCatchFilterContext(position, leftToken),
            isConstantExpressionContext: isConstantExpressionContext,
            isCrefContext: syntaxTree.IsCrefContext(position, cancellationToken) && !leftToken.IsKind(SyntaxKind.DotToken),
            isDefiniteCastTypeContext: syntaxTree.IsDefiniteCastTypeContext(position, leftToken),
            isDelegateReturnTypeContext: syntaxTree.IsDelegateReturnTypeContext(position, leftToken),
            isDestructorTypeContext: isDestructorTypeContext,
            isEnumBaseListContext: syntaxTree.IsEnumBaseListContext(targetToken),
            isEnumTypeMemberAccessContext: syntaxTree.IsEnumTypeMemberAccessContext(semanticModel, position, cancellationToken),
            isFixedVariableDeclarationContext: syntaxTree.IsFixedVariableDeclarationContext(position, leftToken),
            isFunctionPointerTypeArgumentContext: syntaxTree.IsFunctionPointerTypeArgumentContext(position, leftToken, cancellationToken),
            isGenericConstraintContext: syntaxTree.IsGenericConstraintContext(targetToken),
            isGenericTypeArgumentContext: syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken),
            isGlobalStatementContext: isGlobalStatementContext,
            isImplicitOrExplicitOperatorTypeContext: syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, leftToken),
            isOnArgumentListBracketOrComma: isOnArgumentListBracketOrComma,
            isInImportsDirective: leftToken.GetAncestor<UsingDirectiveSyntax>() != null,
            isInNonUserCode: isInNonUserCode,
            isInQuery: leftToken.GetAncestor<QueryExpressionSyntax>() != null,
            isInstanceContext: syntaxTree.IsInstanceContext(targetToken, semanticModel, cancellationToken),
            isTaskLikeTypeContext: precedingModifiers.Contains(SyntaxKind.AsyncKeyword),
            isIsOrAsOrSwitchOrWithExpressionContext: syntaxTree.IsIsOrAsOrSwitchOrWithExpressionContext(semanticModel, position, leftToken, cancellationToken),
            isIsOrAsTypeContext: syntaxTree.IsIsOrAsTypeContext(position, leftToken),
            isLabelContext: syntaxTree.IsLabelContext(position, cancellationToken),
            isLeftSideOfImportAliasDirective: IsLeftSideOfUsingAliasDirective(leftToken),
            isLocalFunctionDeclarationContext: isLocalFunctionDeclarationContext,
            isLocalVariableDeclarationContext: syntaxTree.IsLocalVariableDeclarationContext(position, leftToken, cancellationToken),
            isNameOfContext: syntaxTree.IsNameOfContext(position, semanticModel, cancellationToken),
            isNamespaceContext: syntaxTree.IsNamespaceContext(position, cancellationToken, semanticModel),
            isNamespaceDeclarationNameContext: syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken),
            isNonAttributeExpressionContext: isNonAttributeExpressionContext,
            isObjectCreationTypeContext: syntaxTree.IsObjectCreationTypeContext(position, leftToken, cancellationToken),
            isParameterTypeContext: syntaxTree.IsParameterTypeContext(position, leftToken),
            isPossibleLambdaOrAnonymousMethodParameterTypeContext: syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, leftToken, cancellationToken),
            isPossibleTupleContext: syntaxTree.IsPossibleTupleContext(leftToken, position),
            isPreProcessorDirectiveContext: isPreProcessorDirectiveContext,
            isPreProcessorExpressionContext: isPreProcessorExpressionContext,
            isPreProcessorKeywordContext: isPreProcessorKeywordContext,
            isPrimaryFunctionExpressionContext: syntaxTree.IsPrimaryFunctionExpressionContext(position, leftToken),
            isRightAfterUsingOrImportDirective: targetToken.Parent is UsingDirectiveSyntax usingDirective && usingDirective?.GetLastToken() == targetToken,
            isRightOfNameSeparator: syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken),
            isRightSideOfNumericType: isRightSideOfNumericType,
            isStatementContext: isStatementContext,
            isTypeArgumentOfConstraintContext: syntaxTree.IsTypeArgumentOfConstraintClause(position, cancellationToken),
            isTypeContext: syntaxTree.IsTypeContext(position, cancellationToken, semanticModel),
            isTypeOfExpressionContext: syntaxTree.IsTypeOfExpressionContext(position, leftToken),
            isUsingAliasTypeContext: syntaxTree.IsUsingAliasTypeContext(position, cancellationToken),
            isWithinAsyncMethod: ComputeIsWithinAsyncMethod(),
            precedingModifiers: precedingModifiers,
            cancellationToken: cancellationToken);
    }

    private static bool ComputeIsWithinAsyncMethod()
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
                token.SpanStart, context: null, validModifiers: null, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
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

    public bool IsRecordDeclarationContext(ISet<SyntaxKind> validModifiers, CancellationToken cancellationToken)
    {
        var previousToken = LeftToken.GetPreviousTokenIfTouchingWord(Position);

        if (!previousToken.IsKind(SyntaxKind.RecordKeyword))
            return false;

        var positionBeforeRecordKeyword = previousToken.SpanStart;
        var modifiers = SyntaxTree.GetPrecedingModifiers(positionBeforeRecordKeyword, cancellationToken);

        return modifiers.IsProperSubsetOf(validModifiers);
    }

    public bool IsMemberAttributeContext(
        ISet<SyntaxKind> validTypeDeclarations, bool includingRecordParameters, CancellationToken cancellationToken)
    {
        // cases:
        //   class C { [ |
        var token = this.TargetToken;

        if (token.Kind() == SyntaxKind.OpenBracketToken &&
            token.Parent.IsKind(SyntaxKind.AttributeList))
        {
            if (includingRecordParameters &&
                IsRecordParameterAttributeContext(out var record) &&
                validTypeDeclarations.Contains(record.Kind()))
            {
                return true;
            }

            if (SyntaxTree.IsMemberDeclarationContext(
                    token.SpanStart, context: null, validModifiers: null, validTypeDeclarations: validTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsRecordParameterAttributeContext([NotNullWhen(true)] out RecordDeclarationSyntax? recordDeclaration)
    {
        var token = this.TargetToken;

        if (token.Kind() == SyntaxKind.OpenBracketToken &&
            token.Parent.IsKind(SyntaxKind.AttributeList) &&
            token.Parent.Parent is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax record } })
        {
            recordDeclaration = record;
            return true;
        }

        recordDeclaration = null;
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

    public bool IsRegularTopLevelStatementsContext()
        => IsGlobalStatementContext && SyntaxTree.Options.Kind is SourceCodeKind.Regular;

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
    private static bool ComputeIsAwaitKeywordContext(
        int position,
        SyntaxToken leftToken,
        SyntaxToken targetToken,
        bool isGlobalStatementContext,
        bool isAnyExpressionContext,
        bool isStatementContext)
    {
        if (isGlobalStatementContext)
        {
            return true;
        }

        if (isAnyExpressionContext || isStatementContext)
        {
            foreach (var node in leftToken.GetAncestors<SyntaxNode>())
            {
                if (node is AnonymousFunctionExpressionSyntax)
                    return true;

                if (node.IsKind(SyntaxKind.QueryExpression))
                {
                    // There are some cases where "await" is allowed in a query context. See error CS1995 for details:
                    // error CS1995: The 'await' operator may only be used in a query expression within the first collection expression of the initial 'from' clause or within the collection expression of a 'join' clause
                    if (targetToken.IsKind(SyntaxKind.InKeyword))
                    {
                        return targetToken.Parent switch
                        {
                            FromClauseSyntax { Parent: QueryExpressionSyntax queryExpression } fromClause => queryExpression.FromClause == fromClause,
                            JoinClauseSyntax => true,
                            _ => false,
                        };
                    }

                    return false;
                }

                if (node is LockStatementSyntax lockStatement)
                {
                    if (lockStatement.Statement != null &&
                        !lockStatement.Statement.IsMissing &&
                        lockStatement.Statement.Span.Contains(position))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    private AttributeTargets ComputeValidAttributeTargets()
    {
        // If we're not in an attribute context, return All to allow all attributes
        if (!IsAttributeNameContext)
            return AttributeTargets.All;

        // Find the attribute list that contains the current position
        var token = TargetToken;
        var attributeList = token.Parent?.FirstAncestorOrSelf<AttributeListSyntax>();
        if (attributeList == null)
            return AttributeTargets.All;

        // Check if there's an explicit target specifier (e.g., "assembly:", "return:", etc.)
        if (attributeList.Target != null)
        {
            var targetIdentifier = attributeList.Target.Identifier.ValueText;
            return targetIdentifier switch
            {
                "assembly" or "module" => AttributeTargets.Assembly | AttributeTargets.Module,
                "type" => AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate,
                "method" => AttributeTargets.Method,
                "field" => AttributeTargets.Field,
                "property" => AttributeTargets.Property,
                "event" => AttributeTargets.Event,
                "param" => AttributeTargets.Parameter,
                "return" => AttributeTargets.ReturnValue,
                "typevar" => AttributeTargets.GenericParameter,
                _ => AttributeTargets.All
            };
        }

        // No explicit target, determine from context
        // Walk up to find what the attribute is attached to
        var parentNode = attributeList.Parent;
        if (parentNode == null)
            return AttributeTargets.All;

        return parentNode switch
        {
            // Type declarations
            ClassDeclarationSyntax => AttributeTargets.Class,
            StructDeclarationSyntax => AttributeTargets.Struct,
            InterfaceDeclarationSyntax => AttributeTargets.Interface,
            EnumDeclarationSyntax => AttributeTargets.Enum,
            DelegateDeclarationSyntax => AttributeTargets.Delegate,
            RecordDeclarationSyntax record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) 
                ? AttributeTargets.Struct 
                : AttributeTargets.Class,

            // Member declarations
            MethodDeclarationSyntax => AttributeTargets.Method,
            ConstructorDeclarationSyntax => AttributeTargets.Constructor,
            PropertyDeclarationSyntax => AttributeTargets.Property,
            EventDeclarationSyntax => AttributeTargets.Event,
            EventFieldDeclarationSyntax => AttributeTargets.Event,
            FieldDeclarationSyntax => AttributeTargets.Field,
            IndexerDeclarationSyntax => AttributeTargets.Property,

            // Parameters
            ParameterSyntax => AttributeTargets.Parameter,

            // Type parameters
            TypeParameterSyntax => AttributeTargets.GenericParameter,

            // Assembly/module level
            CompilationUnitSyntax => AttributeTargets.Assembly | AttributeTargets.Module,

            _ => AttributeTargets.All
        };
    }
}
