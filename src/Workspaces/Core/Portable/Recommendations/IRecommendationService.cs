﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal interface IRecommendationService : ILanguageService
    {
        Task<ImmutableArray<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            OptionSet options,
            CancellationToken cancellationToken);
    }
}
