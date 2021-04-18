// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class Recommender
    {
        public static IEnumerable<ISymbol> GetRecommendedSymbolsAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= workspace.Options;
            var languageRecommender = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<IRecommendationService>();

            return languageRecommender.GetRecommendedSymbolsAtPosition(workspace, semanticModel, position, options, cancellationToken).NamedSymbols;
        }

        public static Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
             SemanticModel semanticModel,
             int position,
             Workspace workspace,
             OptionSet? options = null,
             CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetRecommendedSymbolsAtPosition(semanticModel, position, workspace, options, cancellationToken));
        }
    }
}
