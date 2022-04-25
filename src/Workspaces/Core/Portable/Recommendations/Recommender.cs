// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class Recommender
    {
        [Obsolete("Use GetRecommendedSymbolsAtPositionAsync(Document, ...)")]
        public static IEnumerable<ISymbol> GetRecommendedSymbolsAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet? options = null,
            CancellationToken cancellationToken = default)
        {
            var solution = workspace.CurrentSolution;
            options ??= solution.Options;
            var document = solution.GetRequiredDocument(semanticModel.SyntaxTree);
            var languageRecommender = document.GetRequiredLanguageService<IRecommendationService>();
            return languageRecommender.GetRecommendedSymbolsAtPosition(document, semanticModel, position, RecommendationServiceOptions.From(options, document.Project.Language), cancellationToken).NamedSymbols;
        }

        [Obsolete("Use GetRecommendedSymbolsAtPositionAsync(Document, ...)")]
        public static Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
             SemanticModel semanticModel,
             int position,
             Workspace workspace,
             OptionSet? options = null,
             CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetRecommendedSymbolsAtPosition(semanticModel, position, workspace, options, cancellationToken));
        }

        public static async Task<ImmutableArray<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            Document document,
            int position,
            OptionSet? options = null,
            CancellationToken cancellationToken = default)
        {
            var solution = document.Project.Solution;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            options ??= solution.Options;
            var languageRecommender = document.GetRequiredLanguageService<IRecommendationService>();
            return languageRecommender.GetRecommendedSymbolsAtPosition(document, semanticModel, position, RecommendationServiceOptions.From(options, document.Project.Language), cancellationToken).NamedSymbols;
        }
    }
}
