// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        private readonly AsyncBatchingWorkQueue<VisualStudioCodeWindowInfo?, DocumentSymbolDataModel?> _documentSymbolQueue;

        private async ValueTask EnqueueModelUpdateAsync()
        {
            var info = await _visualStudioCodeWindowInfoService.GetVisualStudioCodeWindowInfoAsync(CancellationToken).ConfigureAwait(false);
            if (info is not null)
            {
                _documentSymbolQueue.AddWork(info, cancelExistingWork: true);
            }
        }

        private async ValueTask<DocumentSymbolDataModel?> GetDocumentSymbolAsync(ImmutableSegmentedList<VisualStudioCodeWindowInfo?> settings, CancellationToken cancellationToken)
        {
            var setting = settings.Last();
            if (setting is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var (textBuffer, filePath, caretPoint) = setting;

            // Obtain the LSP response and text snapshot used.
            var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(
            textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

            // If there is no matching LSP server registered the client will return null here - e.g. wrong content type on the buffer, the
            // server totally failed to start, server doesn't support the right capabilities. For C# we might know it's a bug if we get a null
            // response here, but we don't know that in general for all languages.
            if (response is null)
            {
                return null;
            }

            var responseBody = response.Value.response.ToObject<DocumentSymbol[]>();
            if (responseBody is null)
            {
                return null;
            }

            var model = DocumentOutlineHelper.CreateDocumentSymbolDataModel(responseBody, response.Value.snapshot);

            EnqueueFilterAndSortTask(caretPoint);
            return model;
        }

        private record DocumentOutlineSettings(ITextBuffer TextBuffer, string FilePath);
    }
}
