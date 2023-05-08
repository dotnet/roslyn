// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal sealed partial class CompletionHandler
    {
        internal const string EditRangeSetting = "editRange";

        private static async Task<LSP.CompletionList> ConvertToLspCompletionListAsync(
            Document document,
            LSP.ClientCapabilities clientCapabilities,
            CompletionList list, bool isIncomplete, long resultId,
            CancellationToken cancellationToken)
        {
            if (list.ItemsList.Count == 0)
            {
                return new LSP.VSInternalCompletionList
                {
                    Items = Array.Empty<LSP.CompletionItem>(),
                    // If we have a suggestion mode item, we just need to keep the list in suggestion mode.
                    // We don't need to return the fake suggestion mode item.
                    SuggestionMode = list.SuggestionModeItem is not null,
                    IsIncomplete = isIncomplete,
                };
            }

            var lspVSClientCapability = clientCapabilities.HasVisualStudioLspCapability() == true;

            var completionCapabilities = clientCapabilities.TextDocument?.Completion;
            var supportedKinds = completionCapabilities?.CompletionItemKind?.ValueSet?.ToSet() ?? new HashSet<LSP.CompletionItemKind>();
            var itemDefaultsSupported = completionCapabilities?.CompletionListSetting?.ItemDefaults?.Contains(EditRangeSetting) == true;

            // We use the default completion list span as our comparison point for optimization when generating the TextEdits later on.
            var defaultSpan = list.Span;
            var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultRange = ProtocolConversions.TextSpanToRange(defaultSpan, documentText);

            // Set resolve data on list if the client supports it, otherwise set it on each item.
            var resolveData = new CompletionResolveData() { ResultId = resultId };
            var (completionItemResolveData, completionListResolvedData) = clientCapabilities.HasCompletionListDataCapability()
                ? (null as CompletionResolveData, resolveData)
                : (resolveData, null);

            using var _ = ArrayBuilder<LSP.CompletionItem>.GetInstance(out var lspCompletionItems);
            var commitCharactersRuleCache = new Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]>(CommitCharacterArrayComparer.Instance);

            foreach (var item in list.ItemsList)
                lspCompletionItems.Add(await CreateLSPCompletionItemAsync(item).ConfigureAwait(false));

            var completionList = new LSP.VSInternalCompletionList
            {
                Items = lspCompletionItems.ToArray(),
                SuggestionMode = list.SuggestionModeItem != null,
                IsIncomplete = isIncomplete,
                Data = completionListResolvedData,
            };

            if (clientCapabilities.HasCompletionListCommitCharactersCapability())
                PromoteCommonCommitCharactersOntoList(completionList);

            if (itemDefaultsSupported)
            {
                completionList.ItemDefaults = new LSP.CompletionListItemDefaults
                {
                    EditRange = defaultRange,
                };
            }

            return new LSP.OptimizedVSCompletionList(completionList);

            async Task<LSP.CompletionItem> CreateLSPCompletionItemAsync(CompletionItem item)
            {
                var snippetsSupported = completionCapabilities?.CompletionItem?.SnippetSupport ?? false;
                var creationService = document.Project.Solution.Services.GetRequiredService<ILspCompletionResultCreationService>();

                // Defer to host to create the actual completion item (including potential subclasses), and add any
                // custom information.
                var lspItem = await creationService.CreateAsync(
                    document, documentText, snippetsSupported, itemDefaultsSupported, defaultSpan, item, cancellationToken).ConfigureAwait(false);

                // Now add data common to all hosts.
                lspItem.Data = completionItemResolveData;
                lspItem.Label = $"{item.DisplayTextPrefix}{item.DisplayText}{item.DisplayTextSuffix}";

                lspItem.SortText = item.SortText;
                lspItem.FilterText = item.FilterText;

                lspItem.Kind = GetCompletionKind(item.Tags, supportedKinds);
                lspItem.Preselect = ShouldItemBePreselected(item);

                lspItem.CommitCharacters = GetCommitCharacters(item, commitCharactersRuleCache, lspVSClientCapability);

                return lspItem;
            }

            static LSP.CompletionItemKind GetCompletionKind(
                ImmutableArray<string> tags,
                ISet<LSP.CompletionItemKind> supportedClientKinds)
            {
                foreach (var tag in tags)
                {
                    if (ProtocolConversions.RoslynTagToCompletionItemKinds.TryGetValue(tag, out var completionItemKinds))
                    {
                        // Always at least pick the core kind provided.
                        var kind = completionItemKinds[0];

                        // If better kinds are preferred, return them if the client supports them.
                        for (var i = 1; i < completionItemKinds.Length; i++)
                        {
                            var preferredKind = completionItemKinds[i];
                            if (supportedClientKinds.Contains(preferredKind))
                                kind = preferredKind;
                        }

                        return kind;
                    }
                }

                return LSP.CompletionItemKind.Text;
            }

            static string[]? GetCommitCharacters(
                CompletionItem item,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]> currentRuleCache,
                bool supportsVSExtensions)
            {
                // VSCode does not have the concept of soft selection, the list is always hard selected.
                // In order to emulate soft selection behavior for things like argument completion, regex completion, datetime completion, etc
                // we create a completion item without any specific commit characters.  This means only tab / enter will commit.
                // VS supports soft selection, so we only do this for non-VS clients.
                if (!supportsVSExtensions && item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.SoftSelection)
                    return Array.Empty<string>();

                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // VS will use the default commit characters if no items are specified on the completion item.
                // However, other clients like VSCode do not support this behavior so we must specify
                // commit characters on every completion item - https://github.com/microsoft/vscode/issues/90987
                if (supportsVSExtensions && commitCharacterRules.IsEmpty)
                    return null;

                if (!currentRuleCache.TryGetValue(commitCharacterRules, out var cachedCommitCharacters))
                {
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

                    cachedCommitCharacters = commitCharacters.Select(c => c.ToString()).ToArray();
                    currentRuleCache.Add(item.Rules.CommitCharacterRules, cachedCommitCharacters);
                }

                return cachedCommitCharacters;
            }

            static void PromoteCommonCommitCharactersOntoList(LSP.VSInternalCompletionList completionList)
            {
                if (completionList.Items.IsEmpty())
                {
                    return;
                }

                var defaultCommitCharacters = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
                var commitCharacterReferences = new Dictionary<object, int>();
                var mostUsedCount = 0;
                string[]? mostUsedCommitCharacters = null;
                for (var i = 0; i < completionList.Items.Length; i++)
                {
                    var completionItem = completionList.Items[i];
                    var commitCharacters = completionItem.CommitCharacters;
                    // The commit characters on the item are null, this means the commit characters are actually
                    // the default commit characters we passed in the initialize request.
                    commitCharacters ??= defaultCommitCharacters;

                    commitCharacterReferences.TryGetValue(commitCharacters, out var existingCount);
                    existingCount++;

                    if (existingCount > mostUsedCount)
                    {
                        // Capture the most used commit character counts so we don't need to re-iterate the array later
                        mostUsedCommitCharacters = commitCharacters;
                        mostUsedCount = existingCount;
                    }

                    commitCharacterReferences[commitCharacters] = existingCount;
                }

                Contract.ThrowIfNull(mostUsedCommitCharacters);

                // Promoted the most used commit characters onto the list and then remove these from child items.
                completionList.CommitCharacters = mostUsedCommitCharacters;
                for (var i = 0; i < completionList.Items.Length; i++)
                {
                    var completionItem = completionList.Items[i];
                    if (completionItem.CommitCharacters == mostUsedCommitCharacters)
                    {
                        completionItem.CommitCharacters = null;
                    }
                }
            }
        }

        private sealed class CommitCharacterArrayComparer : IEqualityComparer<ImmutableArray<CharacterSetModificationRule>>
        {
            public static readonly CommitCharacterArrayComparer Instance = new();

            private CommitCharacterArrayComparer()
            {
            }

            public bool Equals([AllowNull] ImmutableArray<CharacterSetModificationRule> x, [AllowNull] ImmutableArray<CharacterSetModificationRule> y)
            {
                if (x == y)
                    return true;

                for (var i = 0; i < x.Length; i++)
                {
                    var xKind = x[i].Kind;
                    var yKind = y[i].Kind;
                    if (xKind != yKind)
                    {
                        return false;
                    }

                    var xCharacters = x[i].Characters;
                    var yCharacters = y[i].Characters;
                    if (xCharacters.Length != yCharacters.Length)
                    {
                        return false;
                    }

                    for (var j = 0; j < xCharacters.Length; j++)
                    {
                        if (xCharacters[j] != yCharacters[j])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public int GetHashCode([DisallowNull] ImmutableArray<CharacterSetModificationRule> obj)
            {
                var combinedHash = Hash.CombineValues(obj);
                return combinedHash;
            }
        }
    }
}
