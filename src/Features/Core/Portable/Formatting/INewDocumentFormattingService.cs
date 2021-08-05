// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface INewDocumentFormattingService : ILanguageService
    {
        /// <summary>
        /// Formats a new document that is being added to a project from the Add New Item dialog.
        /// </summary>
        /// <remarks>
        /// Calls to this method will be on the UI thread so consider using .ConfigureAwait(true) to avoid unnecssary thread switching.
        /// </remarks>
        Task<Document> FormatNewDocumentAsync(Document document, CancellationToken cancellationToken);
    }
}
