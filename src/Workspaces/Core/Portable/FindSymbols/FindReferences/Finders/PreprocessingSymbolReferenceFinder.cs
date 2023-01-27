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

            // In VB, a #Const directive assigns a value that can also
            // derive from preprocessing symbols
            if (state.Document.Project.Language is LanguageNames.VisualBasic)
            {
                var extendedTokenParent = tokenParent;
                while (true)
                {
                    extendedTokenParent = extendedTokenParent.Parent;

                    if (extendedTokenParent is null)
                        break;

                    if (extendedTokenParent.RawKind == syntaxFacts.SyntaxKinds.DefineDirectiveTrivia)
                    {
                        return true;
                    }
                }
            }

            // Otherwise, only inside an #if or #elif directive are preprocessing symbols used
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
        // NOTE: We intentionally search for all documents in the entire solution. This is because
        //       the symbols are validly bound by their requested name, despite their current definition
        //       state. Therefore, the same symbol name could be shared across multiple projects and
        //       configured in the project configuration with the same shared identifier.

        return await FindAllSolutionDocumentsAsync(project.Solution, HasDirectiveProbablyContainsIdentifier, symbol, cancellationToken)
            .ConfigureAwait(false);

        static async ValueTask<bool> HasDirectiveProbablyContainsIdentifier(Document document, IPreprocessingSymbol symbol, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is not { ContainsDirectives: true })
                return false;

            var syntaxTreeIndex = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
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
        var symbol = state.SemanticModel.Compilation.CreatePreprocessingSymbol(token.ValueText);
        return await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, symbol, cancellationToken).ConfigureAwait(false);
    }

    private static FinderLocation CreateFinderLocation(FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
        => CreateFinderLocation(state, token, CandidateReason.None, cancellationToken);
}
