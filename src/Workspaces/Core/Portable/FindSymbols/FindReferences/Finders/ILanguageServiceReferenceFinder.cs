// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

/// <summary>
/// Extensibility interface to allow individual languages to extend the 'Find References' service. 
/// Languages can use this to provide specialized cascading logic between symbols that 'Find 
/// References' is searching for.
/// </summary>
internal interface ILanguageServiceReferenceFinder : ILanguageService
{
    Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        ISymbol symbol, Project project, CancellationToken cancellationToken);
}
