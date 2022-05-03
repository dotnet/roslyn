// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.MetadataAsSource
{
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
        /// <param name="cancellationToken">To cancel document operations</param>
        /// <returns>The updated document</returns>
        [Obsolete("Use overloads that takes formatting options")]
        public static async Task<Document> AddSourceToAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IMetadataAsSourceService>();

            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);

            var options = new CleanCodeGenerationOptions(
                GenerationOptions: CodeGenerationOptions.GetDefault(document.Project.LanguageServices),
                CleanupOptions: cleanupOptions);

            return await service.AddSourceToAsync(document, symbolCompilation, symbol, options, cancellationToken).ConfigureAwait(false);
        }

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

            var options = new CleanCodeGenerationOptions(
                GenerationOptions: CodeGenerationOptions.GetDefault(document.Project.LanguageServices),
                CleanupOptions: formattingOptions.CleanupOptions);

            return service.AddSourceToAsync(document, symbolCompilation, symbol, options, cancellationToken);
        }
    }
}
