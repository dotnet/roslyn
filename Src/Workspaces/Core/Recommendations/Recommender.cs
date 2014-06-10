// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    public static class Recommender
    {
        public static IEnumerable<ISymbol> GetRecommendedSymbolsAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? workspace.Options;
            var languageRecommender = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IRecommendationService>();

            return languageRecommender.GetRecommendedSymbolsAtPosition(workspace, semanticModel, position, options, cancellationToken);
        }
    }
}
