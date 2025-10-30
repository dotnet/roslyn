// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.MetadataAsSource;

internal static class OmniSharpMetadataAsSourceService
{
    /// <summary>
    /// Generates formatted source code containing general information about the symbol's
    /// containing assembly, and the public, protected, and protected-or-internal interface of
    /// which the given ISymbol is or is a part of into the given document
    /// </summary>
    /// <param name="document">The document to generate source into</param>
    /// <param name="symbolCompilation">The <see cref="Compilation"/> in which <paramref name="symbol"/> is resolved.</param>
    /// <param name="symbol">The symbol to generate source for</param>
    /// <param name="formattingOptions">Options to use to format the document.</param>
    /// <param name="cancellationToken">To cancel document operations</param>
    /// <returns>The updated document</returns>
    public static Task<Document> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, OmniSharpSyntaxFormattingOptionsWrapper formattingOptions, CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IMetadataAsSourceService>();
        return service.AddSourceToAsync(document, symbolCompilation, symbol, formattingOptions.UnderlyingObject, cancellationToken);
    }
}
