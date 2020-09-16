// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
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
    internal class CompletionHandler : AbstractRequestHandler<LSP.CompletionParams, LSP.CompletionList?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.CompletionList?> HandleRequestAsync(LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            // Filter out snippets as they are not supported in the LSP client
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1139740
            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var completionOptions = documentOptions
                .WithChangedOption(CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude)
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, false)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, false)
                .WithChangedOption(CompletionServiceOptions.DisallowAddingImports, true);

            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();
            var list = await completionService.GetCompletionsAsync(document, position, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (list == null)
            {
                return null;
            }

            var lspVSClientCapability = context.ClientCapabilities?.HasVisualStudioLspCapability() == true;

            return new LSP.VSCompletionList
            {
                Items = list.Items.Select(item => CreateLSPCompletionItem(request, item, lspVSClientCapability)).ToArray(),
                SuggesstionMode = list.SuggestionModeItem != null,
            };

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
                => new TCompletionItem
                {
                    Label = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : item.DisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData { TextDocument = request.TextDocument, Position = request.Position, DisplayText = item.DisplayText },
                    Preselect = item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection,
                };
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
