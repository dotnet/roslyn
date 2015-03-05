// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Completion.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract partial class AbstractCompletionService : ICompletionService, ITextCompletionService
    {
        private const int MruSize = 10;

        private readonly List<string> _committedItems = new List<string>(MruSize);
        private object _mruGate = new object();

        private void CompletionItemComitted(CompletionItem item)
        {
            lock (_mruGate)
            {
                // We need to remove the item if it's already in the list.
                // If we're at capacity, we need to remove the LRU item.
                var removed = _committedItems.Remove(item.DisplayText);
                if (!removed && _committedItems.Count == MruSize)
                {
                    _committedItems.RemoveAt(0);
                }

                _committedItems.Add(item.DisplayText);
            }
        }

        protected int GetMRUIndex(CompletionItem item)
        {
            lock (_mruGate)
            {
                // A lower value indicates more recently used.  Since items are added
                // to the end of the list, our result just maps to the negation of the 
                // index.
                // -1 => 1  == Not Found
                // 0  => 0  == least recently used 
                // 9  => -9 == most recently used 
                var index = _committedItems.IndexOf(item.DisplayText);
                return -index;
            }
        }

        public void ClearMRUCache()
        {
            lock (_mruGate)
            {
                _committedItems.Clear();
            }
        }

        /// <summary>
        /// Apply any culture-specific quirks to the given text for the purposes of pattern matching.
        /// For example, in the Turkish locale, capital 'i's should be treated specially in Visual Basic.
        /// </summary>
        public virtual string GetCultureSpecificQuirks(string candidate)
        {
            return candidate;
        }

        public abstract IEnumerable<ICompletionProvider> GetDefaultCompletionProviders();

        protected abstract string GetLanguageName();

        public async Task<IEnumerable<CompletionItemGroup>> GetGroupsAsync(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            IEnumerable<ICompletionProvider> completionProviders,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return await GetGroupsAsync(document, text, position, triggerInfo, completionProviders, document.Project.Solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
        }

        public Task<IEnumerable<CompletionItemGroup>> GetGroupsAsync(
            SourceText text,
            int position,
            CompletionTriggerInfo triggerInfo,
            IEnumerable<ICompletionProvider> completionProviders,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            return GetGroupsAsync(null, text, position, triggerInfo, completionProviders, options, cancellationToken);
        }

        private class ProviderGroup
        {
            public ICompletionProvider Provider;
            public CompletionItemGroup Group;
        }

        private async Task<IEnumerable<CompletionItemGroup>> GetGroupsAsync(
            Document documentOpt,
            SourceText text,
            int position,
            CompletionTriggerInfo triggerInfo,
            IEnumerable<ICompletionProvider> completionProviders,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            completionProviders = completionProviders ?? this.GetDefaultCompletionProviders();
            var completionProviderToIndex = GetCompletionProviderToIndex(completionProviders);

            IEnumerable<ICompletionProvider> triggeredProviders;
            switch (triggerInfo.TriggerReason)
            {
                case CompletionTriggerReason.TypeCharCommand:
                    triggeredProviders = completionProviders.Where(p => p.IsTriggerCharacter(text, position - 1, options)).ToList();
                    break;
                case CompletionTriggerReason.BackspaceOrDeleteCommand:
                    triggeredProviders = this.TriggerOnBackspace(text, position, triggerInfo, options)
                        ? completionProviders
                        : SpecializedCollections.EmptyEnumerable<ICompletionProvider>();
                    break;
                default:
                    triggeredProviders = completionProviders;
                    break;
            }

            // Now, ask all the triggered providers if they can provide a group.
            var providersAndGroups = new List<ProviderGroup>();
            foreach (var p in triggeredProviders)
            {
                var g = await GetGroupAsync(p, documentOpt, text, position, triggerInfo, cancellationToken).ConfigureAwait(false);
                if (g != null)
                {
                    providersAndGroups.Add(new ProviderGroup { Provider = p, Group = g });
                }
            }

            // See if there was a group provided that was exclusive and had items in it.  If so, then
            // that's all we'll return.
            var firstExclusiveGroup = providersAndGroups.FirstOrDefault(
                t => t.Group.IsExclusive && t.Group.Items.Any());

            if (firstExclusiveGroup != null)
            {
                return SpecializedCollections.SingletonEnumerable(firstExclusiveGroup.Group);
            }
            else
            {
                // If no exclusive providers provided anything, then go through the remaining
                // triggered list and see if any provide items.
                var nonExclusiveGroups = providersAndGroups.Where(t => !t.Group.IsExclusive).ToList();

                // If we still don't have any items, then we're definitely done.
                if (!nonExclusiveGroups.Any(g => g.Group.Items.Any()))
                {
                    return null;
                }

                // If we do have items, then ask all the other (non exclusive providers) if they
                // want to augment the items.
                var augmentTriggerInfo = triggerInfo.WithIsAugment(true);

                var usedProviders = nonExclusiveGroups.Select(g => g.Provider);
                var nonUsedProviders = completionProviders.Except(usedProviders);
                var nonUsedNonExclusiveProviders = new List<ProviderGroup>();
                foreach (var p in nonUsedProviders)
                {
                    var g = await GetGroupAsync(p, documentOpt, text, position, augmentTriggerInfo, cancellationToken).ConfigureAwait(false);
                    if (g != null && !g.IsExclusive)
                    {
                        nonUsedNonExclusiveProviders.Add(new ProviderGroup { Provider = p, Group = g });
                    }
                }

                var allGroups = nonExclusiveGroups.Concat(nonUsedNonExclusiveProviders).ToList();
                if (allGroups.Count == 0)
                {
                    return null;
                }

                // Providers are ordered, but we processed them in our own order.  Ensure that the
                // groups are properly ordered based on the original providers.
                allGroups.Sort((p1, p2) => completionProviderToIndex[p1.Provider] - completionProviderToIndex[p2.Provider]);
                return allGroups.Select(g => g.Group);
            }
        }

        private Dictionary<ICompletionProvider, int> GetCompletionProviderToIndex(IEnumerable<ICompletionProvider> completionProviders)
        {
            var result = new Dictionary<ICompletionProvider, int>();

            int i = 0;
            foreach (var completionProvider in completionProviders)
            {
                result[completionProvider] = i;
                i++;
            }

            return result;
        }

        private static Task<CompletionItemGroup> GetGroupAsync(
            ICompletionProvider provider,
            Document documentOpt,
            SourceText text,
            int position,
            CompletionTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            return provider is ITextCompletionProvider
                ? Task.FromResult(((ITextCompletionProvider)provider).GetGroup(text, position, triggerInfo, cancellationToken))
                : documentOpt != null
                    ? provider.GetGroupAsync(documentOpt, position, triggerInfo, cancellationToken)
                    : SpecializedTasks.Default<CompletionItemGroup>();
        }

        public bool IsTriggerCharacter(SourceText text, int characterPosition, IEnumerable<ICompletionProvider> completionProviders, OptionSet options)
        {
            if (!options.GetOption(CompletionOptions.TriggerOnTyping, GetLanguageName()))
            {
                return false;
            }

            completionProviders = completionProviders ?? GetDefaultCompletionProviders();
            return completionProviders.Any(p => p.IsTriggerCharacter(text, characterPosition, options));
        }

        protected abstract bool TriggerOnBackspace(SourceText text, int position, CompletionTriggerInfo triggerInfo, OptionSet options);

        public abstract Task<TextSpan> GetDefaultTrackingSpanAsync(Document document, int position, CancellationToken cancellationToken);

        public virtual ICompletionRules GetDefaultCompletionRules()
        {
            return new CompletionRules(this);
        }

        public virtual bool DismissIfEmpty
        {
            get { return false; }
        }

        public Task<string> GetSnippetExpansionNoteForCompletionItemAsync(CompletionItem completionItem, Workspace workspace)
        {
            var insertionText = completionItem.CompletionProvider.GetTextChange(completionItem, '\t').NewText;

            var snippetInfoService = workspace.Services.GetLanguageServices(GetLanguageName()).GetService<ISnippetInfoService>();
            if (snippetInfoService != null && snippetInfoService.SnippetShortcutExists_NonBlocking(insertionText))
            {
                return Task.FromResult(string.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, insertionText));
            }

            return SpecializedTasks.Default<string>();
        }

        public virtual bool SupportSnippetCompletionListOnTab
        {
            get { return false; }
        }

        public virtual bool DismissIfLastFilterCharacterDeleted
        {
            get { return false; }
        }
    }
}
