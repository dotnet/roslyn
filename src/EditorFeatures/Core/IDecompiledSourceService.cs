// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IDecompiledSourceService : ILanguageService
    {
        /// <summary>
        /// Generates formatted source code containing general information about the symbol's
        /// containing assembly and the decompiled source code which the given ISymbol is or is a part of
        /// into the given document
        /// </summary>
        /// <param name="document">The document to generate source into</param>
        /// <param name="symbolCompilation">The <see cref="Compilation"/> in which symbol is resolved.</param>
        /// <param name="symbol">The symbol to generate source for</param>
        /// <param name="cancellationToken">To cancel document operations</param>
        /// <returns>The updated document</returns>
        Task<Document> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken);
    }
}
