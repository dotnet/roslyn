// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

internal abstract class SyntaxContext(
    Document document,
    SemanticModel semanticModel,
    int position,
    SyntaxToken leftToken,
    SyntaxToken targetToken,
    bool isAnyExpressionContext,
    bool isAtEndOfPattern,
    bool isAtStartOfPattern,
    bool isAttributeNameContext,
    bool isAwaitKeywordContext,
    bool isBaseListContext,
    bool isEnumBaseListContext,
    bool isEnumTypeMemberAccessContext,
    bool isGenericConstraintContext,
    bool isGlobalStatementContext,
    bool isInImportsDirective,
    bool isInQuery,
    bool isTaskLikeTypeContext,
    bool isNameOfContext,
    bool isNamespaceContext,
    bool isNamespaceDeclarationNameContext,
    bool isObjectCreationTypeContext,
    bool isOnArgumentListBracketOrComma,
    bool isPossibleTupleContext,
    bool isPreProcessorDirectiveContext,
    bool isPreProcessorExpressionContext,
    bool isRightAfterUsingOrImportDirective,
    bool isRightOfNameSeparator,
    bool isRightSideOfNumericType,
    bool isStatementContext,
    bool isTypeContext,
    bool isWithinAsyncMethod,
    CancellationToken cancellationToken)
{
    public Document Document { get; } = document;
    public SemanticModel SemanticModel { get; } = semanticModel;
    public SyntaxTree SyntaxTree { get; } = semanticModel.SyntaxTree;
    public int Position { get; } = position;

    /// <summary>
    /// The token to the left of <see cref="Position"/>. This token may be touching the position.
    /// </summary>
    public SyntaxToken LeftToken { get; } = leftToken;

    /// <summary>
    /// The first token to the left of <see cref="Position"/> that we're not touching. Equal to <see cref="LeftToken"/>
    /// if we aren't touching <see cref="LeftToken" />.
    /// </summary>
    public SyntaxToken TargetToken { get; } = targetToken;

    public bool IsAnyExpressionContext { get; } = isAnyExpressionContext;
    public bool IsAtEndOfPattern { get; } = isAtEndOfPattern;
    public bool IsAtStartOfPattern { get; } = isAtStartOfPattern;
    public bool IsAttributeNameContext { get; } = isAttributeNameContext;
    public bool IsAwaitKeywordContext { get; } = isAwaitKeywordContext;

    /// <summary>
    /// Is in the base list of a type declaration.  Note, this only counts when at the top level of the base list, not
    /// *within* any type already in the base list.  For example <c>class C : $$</c> is in the base list.  But <c>class
    /// C : A&lt;$$&gt;</c> is not.
    /// </summary>
    public bool IsBaseListContext { get; } = isBaseListContext;
    public bool IsEnumBaseListContext { get; } = isEnumBaseListContext;
    public bool IsEnumTypeMemberAccessContext { get; } = isEnumTypeMemberAccessContext;
    public bool IsGenericConstraintContext { get; } = isGenericConstraintContext;
    public bool IsGlobalStatementContext { get; } = isGlobalStatementContext;
    public bool IsInImportsDirective { get; } = isInImportsDirective;
    public bool IsInQuery { get; } = isInQuery;
    public bool IsTaskLikeTypeContext { get; } = isTaskLikeTypeContext;
    public bool IsNameOfContext { get; } = isNameOfContext;
    public bool IsNamespaceContext { get; } = isNamespaceContext;
    public bool IsNamespaceDeclarationNameContext { get; } = isNamespaceDeclarationNameContext;
    public bool IsObjectCreationTypeContext { get; } = isObjectCreationTypeContext;
    public bool IsOnArgumentListBracketOrComma { get; } = isOnArgumentListBracketOrComma;
    public bool IsPossibleTupleContext { get; } = isPossibleTupleContext;
    public bool IsPreProcessorDirectiveContext { get; } = isPreProcessorDirectiveContext;
    public bool IsPreProcessorExpressionContext { get; } = isPreProcessorExpressionContext;
    public bool IsRightAfterUsingOrImportDirective { get; } = isRightAfterUsingOrImportDirective;
    public bool IsRightOfNameSeparator { get; } = isRightOfNameSeparator;
    public bool IsRightSideOfNumericType { get; } = isRightSideOfNumericType;
    public bool IsStatementContext { get; } = isStatementContext;
    public bool IsTypeContext { get; } = isTypeContext;
    public bool IsWithinAsyncMethod { get; } = isWithinAsyncMethod;

    public ImmutableArray<ITypeSymbol> InferredTypes { get; } = document.GetRequiredLanguageService<ITypeInferenceService>().InferTypes(semanticModel, position, cancellationToken);

    /// <summary>
    /// Gets the valid attribute targets for the current attribute context.
    /// Returns <see cref="AttributeTargets.All"/> if not in an attribute context or if language-specific
    /// filtering is not implemented.
    /// </summary>
    public virtual AttributeTargets ValidAttributeTargets => AttributeTargets.All;

    public TService? GetLanguageService<TService>() where TService : class, ILanguageService
        => Document.GetLanguageService<TService>();

    public TService GetRequiredLanguageService<TService>() where TService : class, ILanguageService
        => Document.GetRequiredLanguageService<TService>();
}
