// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
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
        public async Task<object> HandleRequestAsync(Solution solution, LSP.CompletionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var completionService = document.Project.LanguageServices.GetService<CompletionService>();
            var list = await completionService.GetCompletionsAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (list == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var lspClientCapability = clientCapabilities.HasVisualStudioLspCapability();

            return list.Items.Select(item => CreateCompletionItem(request, item)).ToArray();

            // local functions
            static LSP.VSCompletionItem CreateCompletionItem(LSP.CompletionParams request, CompletionItem item)
                => new LSP.VSCompletionItem
                {
                    Label = item.DisplayText,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : item.DisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData { CompletionParams = request, DisplayText = item.DisplayText },
                    Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId())
                };
        }

        private static LSP.CompletionItemKind GetCompletionKind(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (Enum.TryParse<LSP.CompletionItemKind>(tag, out var kind))
                {
                    return kind;
                }
                else if (tag == WellKnownTags.Local || tag == WellKnownTags.Parameter)
                {
                    return LSP.CompletionItemKind.Variable;
                }
                else if (tag == WellKnownTags.Structure)
                {
                    return LSP.CompletionItemKind.Struct;
                }
                else if (tag == WellKnownTags.Delegate)
                {
                    return LSP.CompletionItemKind.Function;
                }
            }

            return LSP.CompletionItemKind.Text;
        }
    }
}
