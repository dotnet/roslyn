// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface INewDocumentFormattingService : ILanguageService
    {
        /// <summary>
        /// Formats a new document that is added from the New Item dialog or added via a code action.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="hintDocument">An optional additional document that can be used to inform the formatting operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CancellationToken cancellationToken);

        /// <summary>
        /// Formats a new document that is added from the New Item dialog or added via a code action.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="optionSet">The options to be considered for the new document</param>
        /// <param name="hintDocument">An optional additional document that can be used to inform the formatting operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task<Document> FormatNewDocumentAsync(Document document, DocumentOptionSet optionSet, Document? hintDocument, CancellationToken cancellationToken);
    }
}
