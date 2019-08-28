// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<LSP.CompletionParams, object>
    {
        public async Task<object> HandleRequestAsync(Solution solution, LSP.CompletionParams request, LSP.ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(keepThreadContext);

            var completionService = document.Project.LanguageServices.GetService<CompletionService>();
            var list = await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(keepThreadContext);
            if (list == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var lspVSClientCapability = clientCapabilities?.HasVisualStudioLspCapability() == true;
            return list.Items.Select(item => CreateLSPCompletionItem(request, item, lspVSClientCapability)).ToArray();

            // local functions
            static LSP.CompletionItem CreateLSPCompletionItem(LSP.CompletionParams request, CompletionItem item, bool useVSCompletionItem)
            {
                if (useVSCompletionItem)
                {
                    var vsCompletionItem = CreateCompletionItem<LSP.VSCompletionItem>(request, item);
                    vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
                    return vsCompletionItem;
                }
                else
                {
                    var roslynCompletionItem = CreateCompletionItem<RoslynCompletionItem>(request, item);
                    roslynCompletionItem.Tags = item.Tags.ToArray();
                    return roslynCompletionItem;
                }
            }

            static TCompletionItem CreateCompletionItem<TCompletionItem>(LSP.CompletionParams request, CompletionItem item) where TCompletionItem : LSP.CompletionItem, new()
            {
                var label = "";
                if (item.DisplayTextPrefix != null)
                {
                    label += item.DisplayTextPrefix;
                }
                label += item.DisplayText;
                if (item.DisplayTextSuffix != null)
                {
                    label += item.DisplayTextSuffix;
                }
                return new TCompletionItem
                {
                    Label = label,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : item.DisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData { CompletionParams = request, DisplayText = item.DisplayText }
                };
            }
        }

        private static LSP.CompletionItemKind GetCompletionKind(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (ProtocolConversions.RoslynTagToCompletionItemKind.TryGetValue(tag, out var completionItemKind))
                {
                    return completionItemKind;
                }
            }

            return LSP.CompletionItemKind.Text;
        }
    }
}
