// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal class PreprocessingSymbolReferenceFinder : AbstractReferenceFinder<IPreprocessingSymbol>
{
    protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        IPreprocessingSymbol symbol,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var tokens = await FindMatchingIdentifierTokensAsync(state, symbol.Name, cancellationToken).ConfigureAwait(false);

        var normalReferences = await FindPreprocessingReferencesInTokensAsync(
            symbol, state,
            tokens.WhereAsArray(MatchesPreprocessingReference, state),
            cancellationToken).ConfigureAwait(false);

        return normalReferences;

        static bool MatchesPreprocessingReference(SyntaxToken token, FindReferencesDocumentState state)
        {
            var syntaxFacts = state.SyntaxFacts;

            var tokenParent = token.Parent;
            Debug.Assert(tokenParent is not null);

            // Quickly evaluate the common case that the parent is a #define or #undef directive
            var parentKind = tokenParent.RawKind;
            if (parentKind == syntaxFacts.SyntaxKinds.DefineDirectiveTrivia)
                return true;

            if (parentKind == syntaxFacts.SyntaxKinds.UndefDirectiveTrivia)
                return true;

            // Only inside an #if or #elif directive are preprocessing symbols used
            return syntaxFacts.SpansIfOrElseIfPreprocessorDirective(tokenParent);
        }
    }

    protected override bool CanFind(IPreprocessingSymbol symbol) => true;

    protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        IPreprocessingSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // NOTE: We intentionally search for multiple documents and consider multiple preprocessing
        //       symbols with the same name to be the same preprocessing symbol. This is because
        //       the symbols could either be defined in the compilation options, the project file
        //       or the document itself that includes the #define directive. In all of the above cases
        //       the preprocessing symbol is evaluated by name, and thus it's considered to be a global
        //       symbol that can be used based on whether it's currently defined in the given document
        //       and line.
        //       After all, writing any name for a preprocessing symbol, defined or not, is valid and will
        //       be computed during preprocessing evaluation of the tree

        return await FindDocumentsAsync(project, documents, HasDirectiveProbablyContainsIdentifier, symbol, cancellationToken)
            .ConfigureAwait(false);

        static async ValueTask<bool> HasDirectiveProbablyContainsIdentifier(Document document, IPreprocessingSymbol symbol, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is not { ContainsDirectives: true })
                return false;

            var syntaxTreeIndex = await document.GetSyntaxTreeIndexAsync(ct).ConfigureAwait(false);
            return syntaxTreeIndex.ProbablyContainsIdentifier(symbol.Name);
        }
    }

    private static async ValueTask<ImmutableArray<FinderLocation>> FindPreprocessingReferencesInTokensAsync(
        ISymbol symbol,
        FindReferencesDocumentState state,
        ImmutableArray<SyntaxToken> tokens,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matched = await PreprocessingSymbolMatchesAsync(
                symbol, state, token, cancellationToken).ConfigureAwait(false);
            if (matched)
            {
                var finderLocation = CreateFinderLocation(state, token, cancellationToken);

                locations.Add(finderLocation);
            }
        }

        return locations.ToImmutable();
    }

    private static async ValueTask<bool> PreprocessingSymbolMatchesAsync(ISymbol symbol, FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
    {
        var preprocessingSearchSymbol = symbol as IPreprocessingSymbol;
        Debug.Assert(preprocessingSearchSymbol is not null);

        return await PreprocessingSymbolsMatchAsync(preprocessingSearchSymbol, state, token, cancellationToken).ConfigureAwait(false);
    }
    private static async ValueTask<bool> PreprocessingSymbolsMatchAsync(
        IPreprocessingSymbol searchSymbol, FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
    {
        var symbolInfo = state.Cache.GetPreprocessingSymbolInfo(token);
        return await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false);
    }

    private static FinderLocation CreateFinderLocation(FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
        => CreateFinderLocation(state, token, CandidateReason.None, cancellationToken);
}
