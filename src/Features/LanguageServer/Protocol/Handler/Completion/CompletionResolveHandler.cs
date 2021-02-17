// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionResolveName, mutatesSolutionState: false)]
    internal class CompletionResolveHandler : IRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
    {
        private readonly CompletionListCache? _completionListCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionResolveHandler(CompletionListCache? completionListCache)
        {
            _completionListCache = completionListCache;
        }

        private static CompletionResolveData GetCompletionResolveData(LSP.CompletionItem request)
        {
            Contract.ThrowIfNull(request.Data);

            return ((JToken)request.Data).ToObject<CompletionResolveData>();
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionItem request)
            => GetCompletionResolveData(request).TextDocument;

        public async Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return completionItem;
            }

            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();
            var data = GetCompletionResolveData(completionItem);

            CompletionList? list = null;

            // See if we have a cache of the completion list we need
            if (_completionListCache != null && data.ResultId.HasValue)
            {
                list = await _completionListCache.GetCachedCompletionListAsync(data.ResultId.Value, cancellationToken).ConfigureAwait(false);
            }

            if (list == null)
            {
                // We don't have a cache, so we need to recompute the list
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(data.Position), cancellationToken).ConfigureAwait(false);
                var completionOptions = await CompletionHandler.GetCompletionOptionsAsync(document, cancellationToken).ConfigureAwait(false);

                list = await completionService.GetCompletionsAsync(document, position, data.CompletionTrigger, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (list == null)
                {
                    return completionItem;
                }
            }

            var selectedItem = list.Items.FirstOrDefault(i => i.DisplayText == data.DisplayText);
            if (selectedItem == null)
            {
                return completionItem;
            }

            var description = await completionService.GetDescriptionAsync(document, selectedItem, cancellationToken).ConfigureAwait(false);

            var lspVSClientCapability = context.ClientCapabilities?.HasVisualStudioLspCapability() == true;
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

        private static LSP.VSCompletionItem CloneVSCompletionItem(LSP.CompletionItem completionItem)
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
                TextEdit = completionItem.TextEdit,
                Preselect = completionItem.Preselect
            };
        }
    }
}
