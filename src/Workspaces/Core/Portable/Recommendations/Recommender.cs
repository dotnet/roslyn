// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class Recommender
    {
        /// <summary>
        /// Obsolete.  Use <see cref="GetRecommendedSymbolsAtPositionAsync(SemanticModel, int, Workspace, OptionSet, CancellationToken)"/>.
        /// </summary>
        [Obsolete("Use GetRecommendedSymbolsAtPositionAsync instead.")]
        public static IEnumerable<ISymbol> GetRecommendedSymbolsAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetRecommendedSymbolsAtPositionAsync(semanticModel, position, workspace, options, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public static Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? workspace.Options;
            var languageRecommender = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IRecommendationService>();

            return languageRecommender.GetRecommendedSymbolsAtPositionAsync(workspace, semanticModel, position, options, cancellationToken);
        }
    }
}
