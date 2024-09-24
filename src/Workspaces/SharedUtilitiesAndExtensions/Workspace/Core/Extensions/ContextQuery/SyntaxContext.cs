// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

internal abstract class SyntaxContext
{
    public Document Document { get; }
    public SemanticModel SemanticModel { get; }
    public SyntaxTree SyntaxTree { get; }
    public int Position { get; }

    /// <summary>
    /// The token to the left of <see cref="Position"/>. This token may be touching the position.
    /// </summary>
    public SyntaxToken LeftToken { get; }

    /// <summary>
    /// The first token to the left of <see cref="Position"/> that we're not touching. Equal to <see cref="LeftToken"/>
    /// if we aren't touching <see cref="LeftToken" />.
    /// </summary>
    public SyntaxToken TargetToken { get; }

    public bool IsAnyExpressionContext { get; }
    public bool IsAtEndOfPattern { get; }
    public bool IsAtStartOfPattern { get; }
    public bool IsAttributeNameContext { get; }
    public bool IsAwaitKeywordContext { get; }
    public bool IsEnumBaseListContext { get; }
    public bool IsEnumTypeMemberAccessContext { get; }
    public bool IsGenericConstraintContext { get; }
    public bool IsGlobalStatementContext { get; }
    public bool IsInImportsDirective { get; }
    public bool IsInQuery { get; }
    public bool IsTaskLikeTypeContext { get; }
    public bool IsNameOfContext { get; }
    public bool IsNamespaceContext { get; }
    public bool IsNamespaceDeclarationNameContext { get; }
    public bool IsOnArgumentListBracketOrComma { get; }
    public bool IsPossibleTupleContext { get; }
    public bool IsPreProcessorDirectiveContext { get; }
    public bool IsPreProcessorExpressionContext { get; }
    public bool IsRightAfterUsingOrImportDirective { get; }
    public bool IsRightOfNameSeparator { get; }
    public bool IsRightSideOfNumericType { get; }
    public bool IsStatementContext { get; }
    public bool IsTypeContext { get; }
    public bool IsWithinAsyncMethod { get; }
    public bool IsInstanceContext { get; }

    public ImmutableArray<ITypeSymbol> InferredTypes { get; }

    protected SyntaxContext(
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
        bool isEnumBaseListContext,
        bool isEnumTypeMemberAccessContext,
        bool isGenericConstraintContext,
        bool isGlobalStatementContext,
        bool isInImportsDirective,
        bool isInQuery,
        bool isInstanceContext,
        bool isTaskLikeTypeContext,
        bool isNameOfContext,
        bool isNamespaceContext,
        bool isNamespaceDeclarationNameContext,
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
        this.Document = document;
        this.SemanticModel = semanticModel;
        this.SyntaxTree = semanticModel.SyntaxTree;
        this.Position = position;
        this.LeftToken = leftToken;
        this.TargetToken = targetToken;

        this.IsAnyExpressionContext = isAnyExpressionContext;
        this.IsAtEndOfPattern = isAtEndOfPattern;
        this.IsAtStartOfPattern = isAtStartOfPattern;
        this.IsAttributeNameContext = isAttributeNameContext;
        this.IsAwaitKeywordContext = isAwaitKeywordContext;
        this.IsEnumBaseListContext = isEnumBaseListContext;
        this.IsEnumTypeMemberAccessContext = isEnumTypeMemberAccessContext;
        this.IsGenericConstraintContext = isGenericConstraintContext;
        this.IsGlobalStatementContext = isGlobalStatementContext;
        this.IsInImportsDirective = isInImportsDirective;
        this.IsInQuery = isInQuery;
        this.IsInstanceContext = isInstanceContext;
        this.IsTaskLikeTypeContext = isTaskLikeTypeContext;
        this.IsNameOfContext = isNameOfContext;
        this.IsNamespaceContext = isNamespaceContext;
        this.IsNamespaceDeclarationNameContext = isNamespaceDeclarationNameContext;
        this.IsOnArgumentListBracketOrComma = isOnArgumentListBracketOrComma;
        this.IsPossibleTupleContext = isPossibleTupleContext;
        this.IsPreProcessorDirectiveContext = isPreProcessorDirectiveContext;
        this.IsPreProcessorExpressionContext = isPreProcessorExpressionContext;
        this.IsRightAfterUsingOrImportDirective = isRightAfterUsingOrImportDirective;
        this.IsRightOfNameSeparator = isRightOfNameSeparator;
        this.IsRightSideOfNumericType = isRightSideOfNumericType;
        this.IsStatementContext = isStatementContext;
        this.IsTypeContext = isTypeContext;
        this.IsWithinAsyncMethod = isWithinAsyncMethod;

        this.InferredTypes = document.GetRequiredLanguageService<ITypeInferenceService>().InferTypes(semanticModel, position, cancellationToken);
    }

    public TService? GetLanguageService<TService>() where TService : class, ILanguageService
        => Document.GetLanguageService<TService>();

    public TService GetRequiredLanguageService<TService>() where TService : class, ILanguageService
        => Document.GetRequiredLanguageService<TService>();
}
