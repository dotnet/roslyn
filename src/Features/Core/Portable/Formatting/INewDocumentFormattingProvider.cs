// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface INewDocumentFormattingProvider
    {
        /// <inheritdoc cref="INewDocumentFormattingService.FormatNewDocumentAsync(Document, Document, CodeCleanupOptions, CancellationToken)"/>
        Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken);
    }
}
