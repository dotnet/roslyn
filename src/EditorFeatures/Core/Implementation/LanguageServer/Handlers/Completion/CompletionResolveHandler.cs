// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// 
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once the
    /// references to VS icon types and classified text runs are removed.
    /// See https://github.com/dotnet/roslyn/issues/55142
    /// </summary>
    [Method(LSP.Methods.TextDocumentCompletionResolveName)]
    internal sealed class CompletionResolveHandler : IRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
    {
        private readonly CompletionListCache _completionListCache;
        private readonly IGlobalOptionService _globalOptions;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CompletionResolveHandler(IGlobalOptionService globalOptions, CompletionListCache completionListCache)
        {
            _globalOptions = globalOptions;
            _completionListCache = completionListCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionItem request)
            => GetCompletionListCacheEntry(request)?.TextDocument;

        public async Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();
            var cacheEntry = GetCompletionListCacheEntry(completionItem);
            if (cacheEntry == null)
            {
                // Don't have a cache associated with this completion item, cannot resolve.
                context.TraceInformation("No cache entry found for the provided completion item at resolve time.");
                return completionItem;
            }

            var list = cacheEntry.CompletionList;

            // Find the matching completion item in the completion list
            var selectedItem = list.Items.FirstOrDefault(cachedCompletionItem => MatchesLSPCompletionItem(completionItem, cachedCompletionItem));
            if (selectedItem == null)
            {
                return completionItem;
            }

            var completionOptions = _globalOptions.GetCompletionOptions(document.Project.Language);
            var displayOptions = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var description = await completionService.GetDescriptionAsync(document, selectedItem, completionOptions, displayOptions, cancellationToken).ConfigureAwait(false)!;
            if (description != null)
            {
                var supportsVSExtensions = context.ClientCapabilities.HasVisualStudioLspCapability();
                if (supportsVSExtensions)
                {
                    var vsCompletionItem = (LSP.VSInternalCompletionItem)completionItem;
                    vsCompletionItem.Description = new ClassifiedTextElement(description.TaggedParts
                        .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
                }
                else
                {
                    var clientSupportsMarkdown = context.ClientCapabilities.TextDocument?.Completion?.CompletionItem?.DocumentationFormat.Contains(LSP.MarkupKind.Markdown) == true;
                    completionItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.TaggedParts, document, clientSupportsMarkdown);
                }
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

            return completionItem;
        }

        private static bool MatchesLSPCompletionItem(LSP.CompletionItem lspCompletionItem, CompletionItem completionItem)
        {
            if (!lspCompletionItem.Label.StartsWith(completionItem.DisplayTextPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            // The prefix matches, consume the matching prefix from the lsp completion item label.
            var displayTextWithSuffix = lspCompletionItem.Label.Substring(completionItem.DisplayTextPrefix.Length, lspCompletionItem.Label.Length - completionItem.DisplayTextPrefix.Length);
            if (!displayTextWithSuffix.EndsWith(completionItem.DisplayTextSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            // The suffix matches, consume the matching suffix from the lsp completion item label.
            var originalDisplayText = displayTextWithSuffix.Substring(0, displayTextWithSuffix.Length - completionItem.DisplayTextSuffix.Length);

            // Now we're left with what should be the original display text for the lsp completion item.
            // Check to make sure it matches the cached completion item label.
            return string.Equals(originalDisplayText, completionItem.DisplayText);
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

        private CompletionListCache.CacheEntry? GetCompletionListCacheEntry(LSP.CompletionItem request)
        {
            Contract.ThrowIfNull(request.Data);
            var resolveData = ((JToken)request.Data).ToObject<CompletionResolveData>();
            if (resolveData?.ResultId == null)
            {
                Contract.Fail("Result id should always be provided when resolving a completion item we returned.");
                return null;
            }

            var cacheEntry = _completionListCache.GetCachedCompletionList(resolveData.ResultId.Value);
            if (cacheEntry == null)
            {
                // No cache for associated completion item. Log some telemetry so we can understand how frequently this actually happens.
                Logger.Log(FunctionId.LSP_CompletionListCacheMiss, KeyValueLogMessage.NoProperty);
            }

            return cacheEntry;
        }
    }
}
