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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<LSP.CompletionParams, LSP.CompletionItem[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler()
        {
        }

        public async Task<LSP.CompletionItem[]> HandleRequestAsync(Solution solution, LSP.CompletionParams request, LSP.ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var completionOptions = documentOptions
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, false)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, false);

            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();
            var list = await completionService.GetCompletionsAsync(document, position, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                var completeDisplayText = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix;
                var completionItem = new TCompletionItem
                {
                    Label = completeDisplayText,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : completeDisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData { CompletionParams = request, DisplayText = item.DisplayText },
                };

                // We only set the commit characters if they differ from the default.
                var commitCharacters = GetCommitCharacters(item);
                if (commitCharacters != null)
                {
                    completionItem.CommitCharacters = commitCharacters;
                }

                return completionItem;
            }

            static string[]? GetCommitCharacters(CompletionItem item)
            {
                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // If the item doesn't have any special rules, just use the default commit characters.
                if (commitCharacterRules.IsEmpty)
                {
                    return null;
                }

                using var _ = ArrayBuilder<char>.GetInstance(out var commitCharacters);
                commitCharacters.AddRange(CompletionRules.Default.DefaultCommitCharacters);
                foreach (var rule in commitCharacterRules)
                {
                    switch (rule.Kind)
                    {
                        case CharacterSetModificationKind.Add:
                            foreach (var c in rule.Characters)
                            {
                                if (!commitCharacters.Contains(c))
                                {
                                    commitCharacters.Add(c);
                                }
                            }
                            continue;

                        case CharacterSetModificationKind.Remove:
                            foreach (var c in rule.Characters)
                            {
                                if (commitCharacters.Contains(c))
                                {
                                    commitCharacters.Remove(c);
                                }
                            }
                            continue;

                        case CharacterSetModificationKind.Replace:
                            commitCharacters.Clear();
                            commitCharacters.AddRange(rule.Characters);
                            continue;
                    }
                }

                return commitCharacters.Select(c => c.ToString()).ToArray();
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
