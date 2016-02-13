// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal interface IRecommendationService : ILanguageService
    {
        Task<IEnumerable<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            OptionSet options,
            CancellationToken cancellationToken);
    }
}
