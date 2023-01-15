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

        var normalReferences = await FindReferencesInTokensAsync(
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

        return await FindDocumentsWithPredicateAsync(project, documents, HasIdentifierContainerPreprocessorDirectiveTrivia, cancellationToken)
            .ConfigureAwait(false);

        bool HasIdentifierContainerPreprocessorDirectiveTrivia(SyntaxTreeIndex syntaxTreeIndex)
        {
            return syntaxTreeIndex.ContainsIdentifierContainerPreprocessingDirective
                && syntaxTreeIndex.ProbablyContainsIdentifier(symbol.Name);
        }
    }
}
