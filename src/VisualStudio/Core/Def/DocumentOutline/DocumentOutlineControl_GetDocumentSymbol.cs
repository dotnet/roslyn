// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.LanguageServices.DocumentOutline.DocumentOutlineControl;
using Microsoft.CodeAnalysis.Collections;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private readonly AsyncBatchingWorkQueue<DocumentOutlineSettings?, DocumentSymbolDataModel?> _documentSymbolQueue;

        private void EnqueuGetDocumentSymbolTask(DocumentOutlineSettings settings)
        {
            _documentSymbolQueue.AddWork(settings, cancelExistingWork: true);
        }

        private async ValueTask<DocumentSymbolDataModel?> GetDocumentSymbolAsync(ImmutableSegmentedList<DocumentOutlineSettings?> settings, CancellationToken cancellationToken)
        {
            var setting = settings.Last();
            if (setting is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var (textBuffer, filePath, search, sort, caret) = setting;

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

            EnqueueFilterAndSortDataModelTask(search, sort, caret);
            return model;
        }
    }
}
