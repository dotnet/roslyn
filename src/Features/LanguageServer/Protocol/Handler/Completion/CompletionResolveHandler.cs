// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionResolveName)]
    internal class CompletionResolveHandler : IRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
    {
        public async Task<LSP.CompletionItem> HandleRequestAsync(Solution solution, LSP.CompletionItem completionItem,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext)
        {
            CompletionResolveData data;
            if (completionItem.Data is CompletionResolveData)
            {
                data = (CompletionResolveData)completionItem.Data;
            }
            else
            {
                data = ((JToken)completionItem.Data).ToObject<CompletionResolveData>();
            }

            var request = data.CompletionParams;

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return completionItem;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(keepThreadContext);

            var completionService = document.Project.LanguageServices.GetService<CompletionService>();
            var list = await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(keepThreadContext);
            if (list == null)
            {
                return completionItem;
            }

            var selectedItem = list.Items.FirstOrDefault(i => i.DisplayText == data.DisplayText);
            if (selectedItem == null)
            {
                return completionItem;
            }

            var description = await completionService.GetDescriptionAsync(document, selectedItem, cancellationToken).ConfigureAwait(keepThreadContext);

            var lspVSClientCapability = clientCapabilities?.HasVisualStudioLspCapability() == true;
            LSP.CompletionItem resolvedCompletionItem;
            if (lspVSClientCapability)
            {
                resolvedCompletionItem = CloneVSCompletionItem(completionItem);
                ((LSP.VSCompletionItem)resolvedCompletionItem).Description = new ClassifiedTextElement(description.TaggedParts
                    .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
            }
            else
            {
                resolvedCompletionItem = RoslynCompletionItem.From(completionItem);
                ((RoslynCompletionItem)resolvedCompletionItem).Description = description.TaggedParts.Select(
                    tp => new RoslynTaggedText { Tag = tp.Tag, Text = tp.Text }).ToArray();
            }

            resolvedCompletionItem.Detail = description.TaggedParts.GetFullText();
            return resolvedCompletionItem;
        }

        private LSP.VSCompletionItem CloneVSCompletionItem(LSP.CompletionItem completionItem)
        {
            return new LSP.VSCompletionItem
            {
                AdditionalTextEdits = completionItem.AdditionalTextEdits,
                Command = completionItem.Command,
                CommitCharacters = completionItem.CommitCharacters,
                Data = completionItem.Data,
                Detail = completionItem.Detail,
                Documentation = completionItem.Documentation,
                FilterText = completionItem.FilterText,
                InsertText = completionItem.InsertText,
                InsertTextFormat = completionItem.InsertTextFormat,
                Kind = completionItem.Kind,
                Label = completionItem.Label,
                SortText = completionItem.SortText,
                TextEdit = completionItem.TextEdit
            };
        }
    }
}
