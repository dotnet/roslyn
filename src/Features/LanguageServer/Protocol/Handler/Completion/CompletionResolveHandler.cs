// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
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

            // Find the matching completion item in the completion list
            var selectedItem = list.Items.FirstOrDefault(
                i => data.DisplayText == i.DisplayText &&
                    completionItem.Label.StartsWith(i.DisplayTextPrefix) &&
                    completionItem.Label.EndsWith(i.DisplayTextSuffix));

            if (selectedItem == null)
            {
                return completionItem;
            }

            var description = await completionService.GetDescriptionAsync(document, selectedItem, cancellationToken).ConfigureAwait(false);

            if (completionItem is LSP.VSCompletionItem vsCompletionItem)
            {
                vsCompletionItem.Description = new ClassifiedTextElement(description.TaggedParts
                    .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
            }

            // We compute the TextEdit resolves for complex text edits (e.g. override and partial
            // method completions) here. Lazily resolving TextEdits is technically a violation of
            // the LSP spec, but is currently supported by the VS client anyway. Once the VS client
            // adheres to the spec, this logic will need to change and VS will need to provide
            // official support for TextEdit resolution in some form.
            if (selectedItem.IsComplexTextEdit)
            {
                Contract.ThrowIfTrue(completionItem.InsertText != null);
                Contract.ThrowIfTrue(completionItem.TextEdit != null);

                var snippetsSupported = context.ClientCapabilities.TextDocument?.Completion?.CompletionItem?.SnippetSupport ?? false;

                completionItem.TextEdit = await GenerateTextEditAsync(
                    document, completionService, selectedItem, snippetsSupported, cancellationToken).ConfigureAwait(false);
            }

            completionItem.Detail = description.TaggedParts.GetFullText();
            return completionItem;
        }

        // Internal for testing
        internal static async Task<LSP.TextEdit> GenerateTextEditAsync(
            Document document,
            CompletionService completionService,
            CompletionItem selectedItem,
            bool snippetsSupported,
            CancellationToken cancellationToken)
        {
            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var completionChange = await completionService.GetChangeAsync(
                document, selectedItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText;
            Contract.ThrowIfNull(newText);

            // If snippets are supported, that means we can move the caret (represented by $0) to
            // a new location.
            if (snippetsSupported)
            {
                var caretPosition = completionChange.NewPosition;
                if (caretPosition.HasValue)
                {
                    // caretPosition is the absolute position of the caret in the document.
                    // We want the position relative to the start of the snippet.
                    var relativeCaretPosition = caretPosition.Value - completionChangeSpan.Start;

                    // The caret could technically be placed outside the bounds of the text
                    // being inserted. This situation is currently unsupported in LSP, so in
                    // these cases we won't move the caret.
                    if (relativeCaretPosition >= 0 && relativeCaretPosition <= newText.Length)
                    {
                        newText = newText.Insert(relativeCaretPosition, "$0");
                    }
                }
            }

            var textEdit = new LSP.TextEdit()
            {
                NewText = newText,
                Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
            };

            return textEdit;
        }
    }
}
