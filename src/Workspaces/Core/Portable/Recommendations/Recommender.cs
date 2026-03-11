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
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Recommendations;

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
        var document = solution.GetRequiredDocument(semanticModel.SyntaxTree);
        var context = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

        var languageRecommender = document.GetRequiredLanguageService<IRecommendationService>();
        return languageRecommender.GetRecommendedSymbolsInContext(context, GetOptions(options, document.Project), cancellationToken).NamedSymbols;
    }

    [Obsolete("Use GetRecommendedSymbolsAtPositionAsync(Document, ...)")]
    public static async Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
         SemanticModel semanticModel,
         int position,
         Workspace workspace,
         OptionSet? options = null,
         CancellationToken cancellationToken = default)
    {
        return GetRecommendedSymbolsAtPosition(semanticModel, position, workspace, options, cancellationToken);
    }

    public static async Task<ImmutableArray<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
        Document document,
        int position,
        OptionSet? options = null,
        CancellationToken cancellationToken = default)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var context = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
        var languageRecommender = document.GetRequiredLanguageService<IRecommendationService>();
        return languageRecommender.GetRecommendedSymbolsInContext(context, GetOptions(options, document.Project), cancellationToken).NamedSymbols;
    }

#pragma warning disable RS0030 // Do not used banned APIs: RecommendationOptions
    private static RecommendationServiceOptions GetOptions(OptionSet? options, Project project)
    {
        options ??= project.Solution.Options;
        var language = project.Language;

        return new RecommendationServiceOptions()
        {
            HideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, language),
            FilterOutOfScopeLocals = options.GetOption(RecommendationOptions.FilterOutOfScopeLocals, language),
        };
    }
#pragma warning restore
}
