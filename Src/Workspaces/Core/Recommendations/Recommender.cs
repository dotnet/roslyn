// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.WorkspaceServices;

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
            options = options ?? WorkspaceService.GetService<IOptionService>(workspace).GetOptions();
            var languageRecommender = LanguageService.GetService<IRecommendationService>(workspace, semanticModel.Language);

            return languageRecommender.GetRecommendedSymbolsAtPosition(workspace, semanticModel, position, options, cancellationToken);
        }
    }
}
