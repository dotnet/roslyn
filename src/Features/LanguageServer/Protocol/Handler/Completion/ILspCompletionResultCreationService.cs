// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal interface ILspCompletionResultCreationService : IWorkspaceService
    {
        Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService)), Shared]
    internal sealed class DefaultLspCompletionResultCreationService : ILspCompletionResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspCompletionResultCreationService()
        {
        }

        public async Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CancellationToken cancellationToken)
        {
            var completionItem = new LSP.CompletionItem();

            await PopulateTextEditAsync(
                document, documentText, itemDefaultsSupported, defaultSpan, item, completionItem, cancellationToken).ConfigureAwait(false);

            return completionItem;
        }

        public static async Task PopulateTextEditAsync(
            Document document,
            SourceText documentText,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            LSP.CompletionItem lspItem,
            CancellationToken cancellationToken)
        {
            var completionService = document.GetRequiredLanguageService<CompletionService>();

            var completionChange = await completionService.GetChangeAsync(
                document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText ?? "";

            if (itemDefaultsSupported && completionChangeSpan == defaultSpan)
            {
                // The span is the same as the default, we just need to store the new text as
                // the insert text so the client can create the text edit from it and the default range.
                lspItem.InsertText = newText;
            }
            else
            {
                lspItem.TextEdit = new LSP.TextEdit()
                {
                    NewText = newText,
                    Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                };
            }
        }
    }
}
