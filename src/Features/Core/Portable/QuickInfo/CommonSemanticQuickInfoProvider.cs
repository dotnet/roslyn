// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
{
    protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
        QuickInfoContext context, SyntaxToken token)
    {
        var (tokenInformation, supportedPlatforms) = await ComputeQuickInfoDataAsync(context, token).ConfigureAwait(false);
        if (tokenInformation.Symbols.IsDefaultOrEmpty)
            return null;

        var cancellationToken = context.CancellationToken;
        var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var services = context.Document.Project.Solution.Services;
        var onTheFlyDocsInfo = await GetOnTheFlyDocsInfoAsync(context, cancellationToken).ConfigureAwait(false);
        return await CreateContentAsync(
            services, semanticModel, token, tokenInformation, supportedPlatforms, context.Options, onTheFlyDocsInfo, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
        CommonQuickInfoContext context, SyntaxToken token)
    {
        var tokenInformation = BindToken(context.Services, context.SemanticModel, token, context.CancellationToken);
        if (tokenInformation.Symbols.IsDefaultOrEmpty)
            return null;

        // onTheFlyDocInfo is null here since On-The-Fly Docs are being computed at the document level.
        return await CreateContentAsync(
            context.Services, context.SemanticModel, token, tokenInformation, supportedPlatforms: null, context.Options, onTheFlyDocsInfo: null, context.CancellationToken).ConfigureAwait(false);
    }

    private async Task<(TokenInformation tokenInformation, SupportedPlatformData? supportedPlatforms)> ComputeQuickInfoDataAsync(
        QuickInfoContext context,
        SyntaxToken token)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;

        var linkedDocumentIds = document.GetLinkedDocumentIds();
        if (linkedDocumentIds.Any())
            return await ComputeFromLinkedDocumentsAsync(context, token, linkedDocumentIds).ConfigureAwait(false);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;
        var tokenInformation = BindToken(services, semanticModel, token, cancellationToken);
        return (tokenInformation, supportedPlatforms: null);
    }

    private async Task<(TokenInformation, SupportedPlatformData supportedPlatforms)> ComputeFromLinkedDocumentsAsync(
        QuickInfoContext context,
        SyntaxToken token,
        ImmutableArray<DocumentId> linkedDocumentIds)
    {
        // Linked files/shared projects: imagine the following when GOO is false
        // #if GOO
        // int x = 3;
        // #endif
        // var y = x$$;
        //
        // 'x' will bind as an error type, so we'll show incorrect information.
        // Instead, we need to find the head in which we get the best binding,
        // which in this case is the one with no errors.

        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var solution = document.Project.Solution;
        var services = solution.Services;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var mainTokenInformation = BindToken(services, semanticModel, token, cancellationToken);

        var candidateProjects = new List<ProjectId> { document.Project.Id };
        var invalidProjects = new List<ProjectId>();

        var candidateResults = new List<(DocumentId docId, TokenInformation tokenInformation)>
        {
            (document.Id, mainTokenInformation)
        };

        foreach (var linkedDocumentId in linkedDocumentIds)
        {
            var linkedDocument = solution.GetRequiredDocument(linkedDocumentId);
            var linkedModel = await linkedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var linkedToken = FindTokenInLinkedDocument(token, linkedModel, cancellationToken);

            if (linkedToken != default)
            {
                // Not in an inactive region, so this file is a candidate.
                candidateProjects.Add(linkedDocumentId.ProjectId);
                var linkedSymbols = BindToken(services, linkedModel, linkedToken, cancellationToken);
                candidateResults.Add((linkedDocumentId, linkedSymbols));
            }
        }

        // Take the first result with no errors.
        // If every file binds with errors, take the first candidate, which is from the current file.
        var bestBinding = candidateResults.FirstOrNull(c => HasNoErrors(c.tokenInformation.Symbols))
            ?? candidateResults.First();

        if (bestBinding.tokenInformation.Symbols.IsDefaultOrEmpty)
            return default;

        // We calculate the set of supported projects
        candidateResults.Remove(bestBinding);
        foreach (var (docId, tokenInformation) in candidateResults)
        {
            // Does the candidate have anything remotely equivalent?
            if (!tokenInformation.Symbols.Intersect(bestBinding.tokenInformation.Symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                invalidProjects.Add(docId.ProjectId);
        }

        var supportedPlatforms = new SupportedPlatformData(solution, invalidProjects, candidateProjects);
        return (bestBinding.tokenInformation, supportedPlatforms);
    }

    private static bool HasNoErrors(ImmutableArray<ISymbol> symbols)
        => symbols.Length > 0
            && !ErrorVisitor.ContainsError(symbols.FirstOrDefault());

    private static SyntaxToken FindTokenInLinkedDocument(
        SyntaxToken token,
        SemanticModel linkedModel,
        CancellationToken cancellationToken)
    {
        var root = linkedModel.SyntaxTree.GetRoot(cancellationToken);
        if (root == null)
            return default;

        // Don't search trivia because we want to ignore inactive regions
        var linkedToken = root.FindToken(token.SpanStart);

        // The new and old tokens should have the same span?
        return token.Span == linkedToken.Span ? linkedToken : default;
    }

    protected static Task<QuickInfoItem> CreateContentAsync(
        SolutionServices services,
        SemanticModel semanticModel,
        SyntaxToken token,
        TokenInformation tokenInformation,
        SupportedPlatformData? supportedPlatforms,
        SymbolDescriptionOptions options,
        OnTheFlyDocsInfo? onTheFlyDocsInfo,
        CancellationToken cancellationToken)
    {
        var syntaxFactsService = services.GetRequiredLanguageService<ISyntaxFactsService>(semanticModel.Language);

        var symbols = tokenInformation.Symbols;

        // if generating quick info for an attribute, prefer bind to the class instead of the constructor
        if (syntaxFactsService.IsNameOfAttribute(token.Parent!))
        {
            symbols = [.. symbols.OrderBy((s1, s2) =>
                s1.Kind == s2.Kind ? 0 :
                s1.Kind == SymbolKind.NamedType ? -1 :
                s2.Kind == SymbolKind.NamedType ? 1 : 0)];
        }

        return QuickInfoUtilities.CreateQuickInfoItemAsync(
            services, semanticModel, token.Span, symbols, supportedPlatforms,
            tokenInformation.ShowAwaitReturn, tokenInformation.NullableFlowState, options, onTheFlyDocsInfo, cancellationToken);
    }

    protected abstract bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
    protected abstract bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
    protected abstract bool GetBindableNodeForTokenIndicatingMemberAccess(SyntaxToken token, out SyntaxToken found);

    protected virtual Task<OnTheFlyDocsInfo?> GetOnTheFlyDocsInfoAsync(QuickInfoContext context, CancellationToken cancellationToken)
        => Task.FromResult<OnTheFlyDocsInfo?>(null);

    protected virtual NullableFlowState GetNullabilityAnalysis(SemanticModel semanticModel, ISymbol symbol, SyntaxNode node, CancellationToken cancellationToken) => NullableFlowState.None;

    private TokenInformation BindToken(
        SolutionServices services, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
    {
        var languageServices = services.GetLanguageServices(semanticModel.Language);
        var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();
        var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

        var symbols = GetSymbolsFromToken(token, services, semanticModel, cancellationToken);

        var bindableParent = syntaxFacts.TryGetBindableParent(token);
        var overloads = bindableParent != null
            ? semanticModel.GetMemberGroup(bindableParent, cancellationToken)
            : [];

        symbols = symbols.Where(IsOk)
                         .Where(s => IsAccessible(s, enclosingType))
                         .Concat(overloads)
                         .Distinct(SymbolEquivalenceComparer.Instance)
                         .ToImmutableArray();

        if (symbols.Any())
        {
            var firstSymbol = symbols.First();
            var isAwait = syntaxFacts.IsAwaitKeyword(token);
            var nullableFlowState = NullableFlowState.None;
            if (bindableParent != null)
            {
                nullableFlowState = GetNullabilityAnalysis(semanticModel, firstSymbol, bindableParent, cancellationToken);
            }

            return new TokenInformation(symbols, isAwait, nullableFlowState);
        }

        // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
        // least bind it to a type.
        if (syntaxFacts.IsOperator(token))
        {
            var typeInfo = semanticModel.GetTypeInfo(token.Parent!, cancellationToken);
            if (IsOk(typeInfo.Type))
            {
                return new TokenInformation([typeInfo.Type]);
            }
        }

        return new TokenInformation([]);
    }

    private ImmutableArray<ISymbol> GetSymbolsFromToken(SyntaxToken token, SolutionServices services, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (GetBindableNodeForTokenIndicatingLambda(token, out var lambdaSyntax))
        {
            var symbol = semanticModel.GetSymbolInfo(lambdaSyntax, cancellationToken).Symbol;
            return symbol != null ? [symbol] : [];
        }

        if (GetBindableNodeForTokenIndicatingPossibleIndexerAccess(token, out var elementAccessExpression))
        {
            var symbol = semanticModel.GetSymbolInfo(elementAccessExpression, cancellationToken).Symbol;
            if (symbol?.IsIndexer() == true)
            {
                return [symbol];
            }
        }

        if (GetBindableNodeForTokenIndicatingMemberAccess(token, out var accessedMember))
        {
            // If the cursor is on the dot in an invocation `x.M()`, then we'll consider the cursor was placed on `M`
            token = accessedMember;
        }

        return semanticModel.GetSemanticInfo(token, services, cancellationToken)
            .GetSymbols(includeType: true);
    }

    private static bool IsOk([NotNullWhen(returnValue: true)] ISymbol? symbol)
    {
        if (symbol == null)
            return false;

        if (symbol.IsErrorType())
            return false;

        if (symbol is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Cref })
            return false;

        return true;
    }

    private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol? within)
        => within == null
            || symbol.IsAccessibleWithin(within);
}
