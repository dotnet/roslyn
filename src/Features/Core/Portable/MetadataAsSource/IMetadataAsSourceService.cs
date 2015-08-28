// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

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
        /// <param name="symbol">The symbol whose interface to generate source for</param>
        /// <param name="cancellationToken">To cancel document operations</param>
        /// <returns>The updated document</returns>
        Task<Document> AddSourceToAsync(Document document, ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken));
    }
}
