// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    // CONSIDER: We can merge IIncrementalAnalyzer2 with IIncrementalAnalyzer once all of our
    //           IVT partners that use IIncrementalAnalyzer have migrated to ExternalAccess layer.
    internal interface IIncrementalAnalyzer2 : IIncrementalAnalyzer
    {
        Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken);
        Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken);

        Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken);
    }
}
