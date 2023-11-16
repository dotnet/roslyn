// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SymbolMapping
{
    internal interface ISymbolMappingService : IWorkspaceService
    {
        /// <summary>
        /// Given a <see cref="SymbolKey"/> and the document whence the corresponding <see cref="ISymbol"/>
        /// came, locate an identical symbol in the correct solution for performing common symbol operations
        /// (e.g. find references) as defined by this service.
        /// </summary>
        /// <param name="document">The document whence the symbol came</param>
        /// <param name="symbolId">The id of the symbol to map</param>
        /// <param name="cancellationToken">To cancel symbol resolution</param>
        /// <returns>The matching symbol from the correct solution or null</returns>
        Task<SymbolMappingResult?> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Given an <see cref="ISymbol"/> and the document whence the corresponding <see cref="ISymbol"/>
        /// came, locate an identical symbol in the correct solution for performing common symbol operations
        /// (e.g. find references) as defined by this service.
        /// </summary>
        /// <param name="document">The document whence the symbol came</param>
        /// <param name="symbol">The symbol to map</param>
        /// <param name="cancellationToken">To cancel symbol resolution</param>
        /// <returns>The matching symbol from the correct solution or null</returns>
        Task<SymbolMappingResult?> MapSymbolAsync(Document document, ISymbol symbol, CancellationToken cancellationToken = default);
    }
}
