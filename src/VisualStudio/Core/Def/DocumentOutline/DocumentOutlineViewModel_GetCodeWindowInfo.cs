// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue uses to get the caret position, and data needed to make an LSP request. 
        /// </summary>
        private readonly AsyncBatchingResultQueue<DocumentSymbolsRequestInfo?> _visualStudioCodeWindowInfoQueue;

        private async ValueTask<DocumentSymbolsRequestInfo?> GetVisualStudioCodeWindowInfoAsync(CancellationToken token)
        {
            var info = await _visualStudioCodeWindowInfoService.GetDocumentSymbolsRequestInfoAsync(token).ConfigureAwait(false);
            if (info is not null)
            {
                _documentSymbolQueue.AddWork(info, cancelExistingWork: true);
            }

            return info;
        }
    }
}
