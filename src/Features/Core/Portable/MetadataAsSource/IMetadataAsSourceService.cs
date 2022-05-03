// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal interface IMetadataAsSourceService : ILanguageService
    {
        /// <summary>
        /// Generates formatted source code containing general information about the symbol's
        /// containing assembly, and the public, protected, and protected-or-internal interface of
        /// which the given ISymbol is or is a part of into the given document
        /// </summary>
        /// <param name="document">The document to generate source into</param>
        /// <param name="symbolCompilation">The <see cref="Compilation"/> in which <paramref name="symbol"/> is resolved.</param>
        /// <param name="symbol">The symbol to generate source for</param>
        /// <param name="options">Options to use to generate and format the code.</param>
        /// <param name="cancellationToken">To cancel document operations</param>
        /// <returns>The updated document</returns>
        Task<Document> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CleanCodeGenerationOptions options, CancellationToken cancellationToken);
    }
}
