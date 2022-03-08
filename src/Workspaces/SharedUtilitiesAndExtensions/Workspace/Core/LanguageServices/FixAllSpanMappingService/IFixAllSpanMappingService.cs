// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface IFixAllSpanMappingService : ILanguageService
    {
        /// <summary>
        /// For the given <paramref name="fixAllScope"/> and <paramref name="triggerSpan"/> in the <paramref name="document"/>,
        /// returns the documents and spans within each document corresponding to containing symbol
        /// declarations for the trigger span.
        /// </summary>
        Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetDocumentsAndSpansForContainingSymbolDeclarationsAsync(
            Document document, TextSpan triggerSpan, FixAllScope fixAllScope, CancellationToken cancellationToken);
    }
}
