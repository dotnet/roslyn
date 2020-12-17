// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionName, mutatesSolutionState: false)]
    internal class CompletionHandler : IRequestHandler<LSP.CompletionParams, LSP.CompletionList?>
    {
        private readonly ImmutableHashSet<char> _csharpTriggerCharacters;
        private readonly ImmutableHashSet<char> _vbTriggerCharacters;

        private readonly CompletionListCache? _completionListCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler(
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
            CompletionListCache? completionListCache)
        {
            _csharpTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.CSharp).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();
            _vbTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.VisualBasic).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();

            _completionListCache = completionListCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;

        public async Task<LSP.CompletionList?> HandleRequestAsync(LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            // C# and VB share the same LSP language server, and thus share the same default trigger characters.
            // We need to ensure the trigger character is valid in the document's language. For example, the '{'
            // character, while a trigger character in VB, is not a trigger character in C#.
            if (request.Context != null &&
                request.Context.TriggerKind == LSP.CompletionTriggerKind.TriggerCharacter &&
                !char.TryParse(request.Context.TriggerCharacter, out var triggerCharacter) &&
                !char.IsLetterOrDigit(triggerCharacter) &&
                !IsValidTriggerCharacterForDocument(document, triggerCharacter))
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completionOptions = await GetCompletionOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();

            // TO-DO: More LSP.CompletionTriggerKind mappings are required to properly map to Roslyn CompletionTriggerKinds.
            // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1178726
            var completionTrigger = ProtocolConversions.LSPToRoslynCompletionTrigger(request.Context);

            var list = await completionService.GetCompletionsAsync(document, position, completionTrigger, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (list == null)
            {
                return null;
            }

            var lspVSClientCapability = context.ClientCapabilities?.HasVisualStudioLspCapability() == true;

            var commitCharactersRuleCache = new Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>>();

            long? resultId = null;
            if (_completionListCache != null)
            {
                // Cache the completion list so we can avoid recomputation in the resolve handler
                resultId = await _completionListCache.UpdateCacheAsync(list, cancellationToken).ConfigureAwait(false);
            }

            return new LSP.VSCompletionList
            {
                Items = list.Items.Select(item => CreateLSPCompletionItem(
                    request, item, resultId, lspVSClientCapability, completionTrigger, commitCharactersRuleCache)).ToArray(),
                SuggestionMode = list.SuggestionModeItem != null,
            };

            // Local functions
            bool IsValidTriggerCharacterForDocument(Document document, char triggerCharacter)
            {
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    return _csharpTriggerCharacters.Contains(triggerCharacter);
                }
                else if (document.Project.Language == LanguageNames.VisualBasic)
                {
                    return _vbTriggerCharacters.Contains(triggerCharacter);
                }

                // Typescript still calls into this for completion.
                // Since we don't know what their trigger characters are, just return true.
                return true;
            }

            static LSP.CompletionItem CreateLSPCompletionItem(
                LSP.CompletionParams request,
                CompletionItem item,
                long? resultId,
                bool useVSCompletionItem,
                CompletionTrigger completionTrigger,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> commitCharacterRulesCache)
            {
                if (useVSCompletionItem)
                {
                    var vsCompletionItem = CreateCompletionItem<LSP.VSCompletionItem>(request, item, resultId, completionTrigger, commitCharacterRulesCache);
                    vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
                    return vsCompletionItem;
                }
                else
                {
                    var roslynCompletionItem = CreateCompletionItem<LSP.CompletionItem>(request, item, resultId, completionTrigger, commitCharacterRulesCache);
                    return roslynCompletionItem;
                }
            }

            static TCompletionItem CreateCompletionItem<TCompletionItem>(
                LSP.CompletionParams request,
                CompletionItem item,
                long? resultId,
                CompletionTrigger completionTrigger,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> commitCharacterRulesCache) where TCompletionItem : LSP.CompletionItem, new()
            {
                var completeDisplayText = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix;
                var completionItem = new TCompletionItem
                {
                    Label = completeDisplayText,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : completeDisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData
                    {
                        TextDocument = request.TextDocument,
                        Position = request.Position,
                        DisplayText = item.DisplayText,
                        CompletionTrigger = completionTrigger,
                        ResultId = resultId,
                    },
                    Preselect = item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection,
                };
                var commitCharacters = GetCommitCharacters(item, commitCharacterRulesCache);
                if (commitCharacters != null)
                {
                    completionItem.CommitCharacters = commitCharacters;
                }

                return completionItem;
            }

            static string[]? GetCommitCharacters(CompletionItem item, Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> currentRuleCache)
            {
                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // If the item doesn't have any special rules, just use the default commit characters.
                if (commitCharacterRules.IsEmpty)
                {
                    return null;
                }

                if (currentRuleCache.TryGetValue(commitCharacterRules, out var currentRules))
                {
                    return currentRules.ToArray();
                }

                using var _ = PooledHashSet<char>.GetInstance(out var commitCharacters);
                commitCharacters.AddAll(CompletionRules.Default.DefaultCommitCharacters);
                foreach (var rule in commitCharacterRules)
                {
                    switch (rule.Kind)
                    {
                        case CharacterSetModificationKind.Add:
                            commitCharacters.UnionWith(rule.Characters);
                            continue;
                        case CharacterSetModificationKind.Remove:
                            commitCharacters.ExceptWith(rule.Characters);
                            continue;
                        case CharacterSetModificationKind.Replace:
                            commitCharacters.Clear();
                            commitCharacters.AddRange(rule.Characters);
                            break;
                    }
                }

                var commitCharacterSet = commitCharacters.Select(c => c.ToString()).ToImmutableArray();
                currentRuleCache.Add(item.Rules.CommitCharacterRules, commitCharacterSet);
                return commitCharacterSet.ToArray();
            }
        }

        internal static ImmutableHashSet<char> GetTriggerCharacters(CompletionProvider provider)
        {
            if (provider is LSPCompletionProvider lspProvider)
            {
                return lspProvider.TriggerCharacters;
            }

            return ImmutableHashSet<char>.Empty;
        }

        internal static async Task<OptionSet> GetCompletionOptionsAsync(Document document, CancellationToken cancellationToken)
        {
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
            return completionOptions;
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
