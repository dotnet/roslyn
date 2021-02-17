// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// </summary>
    internal class CompletionResolveHandler : IRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
    {
        private readonly CompletionListCache _completionListCache;

        public string Method => LSP.Methods.TextDocumentCompletionResolveName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CompletionResolveHandler(CompletionListCache completionListCache)
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
            if (data.ResultId.HasValue)
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

            var selectedItem = list.Items.FirstOrDefault(i => i.DisplayTextPrefix + i.DisplayText + i.DisplayTextSuffix == data.CompleteDisplayText);
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

            // We compute the TextEdit resolves for override and partial method completions here.
            // Lazily resolving TextEdits is a supported hack in VS, but when the hack is removed
            // this logic will need to change and VS will need to provide official support for
            // TextEdit resolution in some form.
            if (resolvedCompletionItem.InsertText == null && resolvedCompletionItem.TextEdit == null)
            {
                resolvedCompletionItem.TextEdit = await GenerateTextEditAsync(
                    document, completionService, selectedItem, cancellationToken).ConfigureAwait(false);
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
                Icon = completionItem is LSP.VSCompletionItem vsCompletionItem ? vsCompletionItem.Icon : null,
                InsertText = completionItem.InsertText,
                InsertTextFormat = completionItem.InsertTextFormat,
                Kind = completionItem.Kind,
                Label = completionItem.Label,
                SortText = completionItem.SortText,
                TextEdit = completionItem.TextEdit,
                Preselect = completionItem.Preselect
            };
        }

        private static async Task<LSP.TextEdit> GenerateTextEditAsync(
            Document document,
            CompletionService completionService,
            CompletionItem selectedItem,
            CancellationToken cancellationToken)
        {
            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var completionChange = await completionService.GetChangeAsync(
                document, selectedItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;

            var textEdit = new LSP.TextEdit()
            {
                NewText = completionChange.TextChange.NewText ?? "",
                Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
            };

            return textEdit;
        }
    }
}
