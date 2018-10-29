// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface IDocumentTextDifferencingService : IWorkspaceService
    {
        /// <summary>
        /// Computes the text changes between two documents.
        /// </summary>
        /// <param name="oldDocument">The old version of the document.</param>
        /// <param name="newDocument">The new version of the document.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An array of changes.</returns>
        Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken);

        /// <summary>
        /// Computes the text changes between two documents.
        /// </summary>
        /// <param name="oldDocument">The old version of the document.</param>
        /// <param name="newDocument">The new version of the document.</param>
        /// <param name="preferredDifferenceType">The type of differencing to perform. Not supported by all text differencing services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An array of changes.</returns>
        Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, TextDifferenceTypes preferredDifferenceType, CancellationToken cancellationToken);
    }
}
