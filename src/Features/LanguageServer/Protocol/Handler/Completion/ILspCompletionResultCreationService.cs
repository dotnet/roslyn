// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal interface ILspCompletionResultCreationService : IWorkspaceService
    {
        Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            string label,
            CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService)), Shared]
    internal sealed class DefaultLspCompletionResultCreationService : ILspCompletionResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspCompletionResultCreationService()
        {
        }

        public async Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            string label,
            CancellationToken cancellationToken)
        {
            var completionItem = new LSP.CompletionItem();
            PopulateItem(
                item, completionItem, label, completionItem);
            await PopulateTextEditAsync(
                document, documentText, itemDefaultsSupported, defaultSpan, item, completionItem, cancellationToken).ConfigureAwait(false);

            return completionItem;
        }

        public static void PopulateItem(
            CompletionItem item,
            LSP.CompletionItem completionItem,
            string label)
        {
            completionItem.Label = label;
            completionItem.SortText = item.SortText;
            completionItem.FilterText = item.FilterText;
            completionItem.Kind = GetCompletionKind(item.Tags);
            completionItem.Data = completionResolveData;
            completionItem.Preselect = CompletionHandler.ShouldItemBePreselected(item);
            completionItem.CommitCharacters = GetCommitCharacters(item, commitCharacterRulesCache, supportsVSExtensions);

            return;

            static LSP.CompletionItemKind GetCompletionKind(ImmutableArray<string> tags)
            {
                foreach (var tag in tags)
                {
                    if (ProtocolConversions.RoslynTagToCompletionItemKind.TryGetValue(tag, out var completionItemKind))
                        return completionItemKind;
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
        }

        public static async Task PopulateTextEditAsync(
            Document document,
            SourceText documentText,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            LSP.CompletionItem lspItem,
            CancellationToken cancellationToken)
        {
            var completionService = document.GetRequiredLanguageService<CompletionService>();

            var completionChange = await completionService.GetChangeAsync(
                document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText ?? "";

            if (itemDefaultsSupported && completionChangeSpan == defaultSpan)
            {
                // The span is the same as the default, we just need to store the new text as
                // the insert text so the client can create the text edit from it and the default range.
                lspItem.InsertText = newText;
            }
            else
            {
                lspItem.TextEdit = new LSP.TextEdit()
                {
                    NewText = newText,
                    Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                };
            }
        }
    }
}
