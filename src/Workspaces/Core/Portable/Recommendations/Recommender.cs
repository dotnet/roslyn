﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
            CancellationToken cancellationToken = default)
        {
            return GetRecommendedSymbolsAtPositionAsync(semanticModel, position, workspace, options, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public static async Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet options = null,
            CancellationToken cancellationToken = default)
        {
            return await GetImmutableRecommendedSymbolsAtPositionAsync(
                semanticModel, position, workspace, options, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<ISymbol>> GetImmutableRecommendedSymbolsAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= workspace.Options;
            var languageRecommender = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IRecommendationService>();

            return await languageRecommender.GetRecommendedSymbolsAtPositionAsync(
                workspace, semanticModel, position, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
