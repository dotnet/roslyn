// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Progression.CodeSchema.Api;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue that uses the language-server-protocol to get document symbol information.
        /// This queue can return null if it is called before and LSP server is registered for our document.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<VisualStudioCodeWindowInfo, DocumentSymbolDataModel?> _documentSymbolQueue;

        private ITextSnapshot? _currentSnapshot;

        private async ValueTask<DocumentSymbolDataModel?> GetDocumentSymbolAsync(ImmutableSegmentedList<VisualStudioCodeWindowInfo> infos, CancellationToken cancellationToken)
        {
            var info = infos.Last();

            cancellationToken.ThrowIfCancellationRequested();

            var (textBuffer, filePath, _) = info;

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
            // It would be a bug in the LSP server implementation if we get back a null result here.
            Assumes.NotNull(responseBody);

            var model = DocumentOutlineHelper.CreateDocumentSymbolDataModel(responseBody, response.Value.snapshot);

            var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);

            // lock while we update the collection
            using (await _guard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _currentSnapshot = model.OriginalSnapshot;
                DocumentOutlineHelper.UnselectAll(DocumentSymbolViewModelItems);
                var allCollapsed = DocumentOutlineHelper.AreAllCollapsed(DocumentSymbolViewModelItems);
                DocumentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>(documentSymbolViewModelItems);

                if (_currentCaretPosition.HasValue)
                {
                    // if we previously had a node selected, select that node in the new tree
                    EnqueueSelectTreeNode(_currentCaretPosition.Value);
                }
                else if (allCollapsed)
                {
                    // we previously had all nodes collapsed, so maintain that
                    EnqueueExpandCollapseUpdate(ExpansionOption.Collapse);
                }
            }

            return model;
        }
    }
}
