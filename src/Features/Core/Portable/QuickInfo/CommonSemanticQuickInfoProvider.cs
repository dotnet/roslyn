// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
{
    private static readonly SyntaxAnnotation s_annotation = new();

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

        // We calculate the set of projects that are candidates for the best binding
        var candidateProjects = candidateResults.SelectAsArray(result => result.docId.ProjectId);

        // We calculate the set of supported projects
        using var _ = ArrayBuilder<ProjectId>.GetInstance(out var invalidProjects);
        candidateResults.Remove(bestBinding);
        foreach (var (docId, tokenInformation) in candidateResults)
        {
            // Does the candidate have anything remotely equivalent?
            if (!tokenInformation.Symbols.Intersect(bestBinding.tokenInformation.Symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                invalidProjects.Add(docId.ProjectId);
        }

        var supportedPlatforms = new SupportedPlatformData(solution, invalidProjects.ToImmutableAndClear(), candidateProjects);
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
        if (syntaxFactsService.IsNameOfAttribute(token.Parent))
        {
            symbols = [.. symbols.OrderBy((s1, s2) =>
                s1.Kind == s2.Kind ? 0 :
                s1.Kind == SymbolKind.NamedType ? -1 :
                s2.Kind == SymbolKind.NamedType ? 1 : 0)];
        }

        return QuickInfoUtilities.CreateQuickInfoItemAsync(
            services, semanticModel, token.Span, symbols, supportedPlatforms,
            tokenInformation.ShowAwaitReturn, tokenInformation.NullabilityInfo, options, onTheFlyDocsInfo, cancellationToken);
    }

    protected abstract bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
    protected abstract bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
    protected abstract bool GetBindableNodeForTokenIndicatingMemberAccess(SyntaxToken token, out SyntaxToken found);

    protected virtual Task<OnTheFlyDocsInfo?> GetOnTheFlyDocsInfoAsync(QuickInfoContext context, CancellationToken cancellationToken)
        => Task.FromResult<OnTheFlyDocsInfo?>(null);

    protected virtual string? GetNullabilityAnalysis(SemanticModel semanticModel, ISymbol symbol, SyntaxNode node, CancellationToken cancellationToken) => null;

    private string? GetNullabilityAnalysis(
        SolutionServices services, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, CancellationToken cancellationToken)
    {
        var languageServices = services.GetLanguageServices(semanticModel.Language);
        var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();

        var bindableParent = syntaxFacts.TryGetBindableParent(token);
        if (bindableParent is null)
            return null;

        return TryGetNullabilityAnalysisForRewrittenExpression(out var analysis)
            ? analysis
            : GetNullabilityAnalysis(semanticModel, symbol, bindableParent, cancellationToken);

        bool TryGetNullabilityAnalysisForRewrittenExpression(out string? analysis)
        {
            analysis = null;

            // Look to see if we're inside a suppression (e.g. `expr!`).  The suppression changes the nullability analysis,
            // and we don't actually want that here as we want to show the original nullability prior to the suppression applying.
            //
            // In that case, actually fork the semantic model with the `!` removed and then re-bind the token, getting the 
            // analysis results from that.
            //
            // Similarly, checks like `x is null` actually change the nullability of 'x' in the analysis.  While this change
            // is desirable, we still want to show the original nullability of 'x' prior to the check.  In other words, what
            // the null state was flowing in, not flowing out.
            var tokenParent = token.GetRequiredParent();
            var nodeToRewrite = GetNodeToRewrite(tokenParent);
            if (nodeToRewrite is null)
                return false;

            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

            var editor = new SyntaxEditor(root, services);
            // First, mark the token, so we can find it later.
            editor.ReplaceNode(
                tokenParent, tokenParent.ReplaceToken(token, token.WithAdditionalAnnotations(s_annotation)));

            // Now walk upwards, removing all the suppressions until we hit the top of the suppression chain.
            for (var current = nodeToRewrite;
                 current is not null;
                 current = GetNodeToRewrite(current))
            {
                editor.ReplaceNode(
                    current,
                    (current, generator) =>
                    {
                        if (syntaxFacts.IsPostfixUnaryExpression(current))
                            return syntaxFacts.GetOperandOfPostfixUnaryExpression(current);

                        if (syntaxFacts.IsIsTypeExpression(current))
                        {
                            syntaxFacts.GetPartsOfAnyIsTypeExpression(current, out var left, out _);
                            return left;
                        }

                        if (syntaxFacts.IsIsPatternExpression(current))
                        {
                            syntaxFacts.GetPartsOfIsPatternExpression(current, out var left, out _, out _);
                            return left;
                        }

                        return current;
                    });
            }

            // Now fork the semantic model with the new root that has the suppressions removed.
            var newRoot = editor.GetChangedRoot();

            var newTree = semanticModel.SyntaxTree.WithRootAndOptions(newRoot, semanticModel.SyntaxTree.Options);
            var newToken = newTree.GetRoot(cancellationToken).GetAnnotatedTokens(s_annotation).Single();

            var newBindableParent = syntaxFacts.TryGetBindableParent(newToken);
            if (newBindableParent is null)
                return false;

            var newCompilation = semanticModel.Compilation.ReplaceSyntaxTree(semanticModel.SyntaxTree, newTree);
            semanticModel = newCompilation.GetSemanticModel(newTree);

            var symbols = BindSymbols(services, semanticModel, newToken, cancellationToken);
            if (symbols.IsEmpty)
                return false;

            analysis = GetNullabilityAnalysis(semanticModel, symbols[0], newBindableParent, cancellationToken);
            return true;

            SyntaxNode? GetNodeToRewrite(SyntaxNode node)
            {
                var last = node;
                for (var current = last.Parent; current != null; last = current, current = current.Parent)
                {
                    if (current.RawKind == syntaxFacts.SyntaxKinds.SuppressNullableWarningExpression)
                        return current;

                    if (syntaxFacts.IsIsTypeExpression(current))
                    {
                        syntaxFacts.GetPartsOfAnyIsTypeExpression(current, out var left, out _);
                        if (left == last)
                            return current;
                    }

                    if (syntaxFacts.IsIsPatternExpression(current))
                    {
                        syntaxFacts.GetPartsOfIsPatternExpression(current, out var left, out _, out _);
                        if (left == last)
                            return current;
                    }
                }

                return null;
            }
        }
    }

    protected ImmutableArray<ISymbol> BindSymbols(
        SolutionServices services, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
    {
        var languageServices = services.GetLanguageServices(semanticModel.Language);
        var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();
        var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

        var bindableParent = syntaxFacts.TryGetBindableParent(token);

        var symbolSet = new HashSet<ISymbol>(SymbolEquivalenceComparer.Instance);
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var filteredSymbols);

        AddSymbols(GetSymbolsFromToken(token, services, semanticModel, cancellationToken), checkAccessibility: true);
        AddSymbols(bindableParent != null ? semanticModel.GetMemberGroup(bindableParent, cancellationToken) : [], checkAccessibility: false);

        return filteredSymbols.ToImmutableAndClear();

        void AddSymbols(ImmutableArray<ISymbol> symbols, bool checkAccessibility)
        {
            foreach (var symbol in symbols)
            {
                if (!IsOk(symbol))
                    continue;

                if (checkAccessibility && !IsAccessible(symbol, enclosingType))
                    continue;

                if (symbolSet.Add(symbol))
                    filteredSymbols.Add(symbol);
            }
        }
    }

    private TokenInformation BindToken(
        SolutionServices services, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
    {
        var filteredSymbols = BindSymbols(services, semanticModel, token, cancellationToken);

        var languageServices = services.GetLanguageServices(semanticModel.Language);
        var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();

        if (filteredSymbols is [var firstSymbol, ..])
        {
            var isAwait = syntaxFacts.IsAwaitKeyword(token);
            var nullabilityInfo = GetNullabilityAnalysis(
                services, semanticModel, firstSymbol, token, cancellationToken);

            return new TokenInformation(filteredSymbols, isAwait, nullabilityInfo);
        }

        // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
        // least bind it to a type.
        if (syntaxFacts.IsOperator(token))
        {
            var typeInfo = semanticModel.GetTypeInfo(token.Parent!, cancellationToken);
            if (IsOk(typeInfo.Type))
                return new TokenInformation([typeInfo.Type]);
        }

        return default;
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
