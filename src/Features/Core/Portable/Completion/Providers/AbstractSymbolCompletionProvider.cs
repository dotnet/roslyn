// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractSymbolCompletionProvider : CommonCompletionProvider
    {
        // PERF: Many CompletionProviders derive AbstractSymbolCompletionProvider and therefore
        // compute identical contexts. This actually shows up on the 2-core typing test.
        // Cache the most recent document/position/computed SyntaxContext to reduce repeat computation.
        private static readonly ConditionalWeakTable<Document, Task<SyntaxContext>> s_cachedDocuments = new ConditionalWeakTable<Document, Task<SyntaxContext>>();
        private static int s_cachedPosition;
        private static readonly object s_cacheGate = new object();

        protected AbstractSymbolCompletionProvider()
        {
        }

        protected abstract (string displayText, string insertionText) GetDisplayAndInsertionText(ISymbol symbol, SyntaxContext context);
        protected abstract CompletionItemRules GetCompletionItemRules(IReadOnlyList<(ISymbol symbol, CompletionItemRules rules)> symbols, SyntaxContext context);

        /// <summary>
        /// Given a list of symbols, creates the list of completion items for them.
        /// </summary>
        protected IEnumerable<CompletionItem> CreateItems(
            IEnumerable<(ISymbol symbol, CompletionItemRules)> items,
            SyntaxContext context,
            Dictionary<(ISymbol, CompletionItemRules), List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            var tree = context.SyntaxTree;

            var q = from item in items
                    let texts = GetDisplayAndInsertionText(item.symbol, context)
                    group item by texts into g
                    select this.CreateItem(
                        g.Key.displayText, g.Key.insertionText, g.ToList(), context,
                        invalidProjectMap, totalProjects, preselect);

            return q.ToList();
        }

        /// <summary>
        /// Given a list of symbols, and a mapping from each symbol to its original SemanticModel, creates the list of completion items for them.
        /// </summary>
        protected IEnumerable<CompletionItem> CreateItems(
            IEnumerable<(ISymbol symbol, CompletionItemRules)> items,
            Dictionary<(ISymbol, CompletionItemRules), SyntaxContext> originatingContextMap,
            Dictionary<(ISymbol, CompletionItemRules), List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            var q = from item in items
                    let texts = GetDisplayAndInsertionText(item.symbol, originatingContextMap[item])
                    group item by texts into g
                    select this.CreateItem(
                        g.Key.displayText, g.Key.insertionText, g.ToList(),
                        originatingContextMap[g.First()], invalidProjectMap, totalProjects, preselect);

            return q.ToList();
        }

        /// <summary>
        /// Given a Symbol, creates the completion item for it.
        /// </summary>
        private CompletionItem CreateItem(
            string displayText,
            string insertionText,
            List<(ISymbol, CompletionItemRules)> items,
            SyntaxContext context,
            Dictionary<(ISymbol, CompletionItemRules), List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            Contract.ThrowIfNull(items);

            SupportedPlatformData supportedPlatformData = null;
            if (invalidProjectMap != null)
            {
                List<ProjectId> invalidProjects = null;
                foreach (var item in items)
                {
                    if (invalidProjectMap.TryGetValue(item, out invalidProjects))
                    {
                        break;
                    }
                }

                if (invalidProjects != null)
                {
                    supportedPlatformData = new SupportedPlatformData(invalidProjects, totalProjects, context.Workspace);
                }
            }

            return CreateItem(displayText, insertionText, items, context, preselect, supportedPlatformData);
        }

        protected virtual CompletionItem CreateItem(
            string displayText, string insertionText,
            List<(ISymbol symbol, CompletionItemRules)> items,
            SyntaxContext context, bool preselect,
            SupportedPlatformData supportedPlatformData)
        {
            // TODO: 1. Do we need to make CreateWithSymbolId take the tuple? 
            // TODO: 2. if we do (1) then we need to remove .Select(item => item.symbol).ToImmutableArray()
            // TODO: 3. Rename symbols to items
            // TODO: 4. Remove GetCompletionItemRules

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                insertionText: insertionText,
                filterText: GetFilterText(items[0].symbol, displayText, context),
                contextPosition: context.Position,
                symbols: items.Select(item => item.symbol).ToImmutableArray(),
                supportedPlatforms: supportedPlatformData,
                matchPriority: preselect ? MatchPriority.Preselect : MatchPriority.Default,
                rules: GetCompletionItemRules(items, context));
        }

        protected virtual string GetFilterText(ISymbol symbol, string displayText, SyntaxContext context)
        {
            return (displayText == symbol.Name) ||
                (displayText.Length > 0 && displayText[0] == '@') ||
                (context.IsAttributeNameContext && symbol.IsAttribute())
                ? displayText
                : symbol.Name;
        }

        protected abstract Task<ImmutableArray<ISymbol>> GetSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken);

        protected virtual Task<ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>> GetPreselectedItemsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            // If we were triggered by typing a character, then do a semantic check to make sure
            // we're still applicable.  If not, then return immediately.
            if (context.Trigger.Kind == CompletionTriggerKind.Insertion)
            {
                var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
                if (!isSemanticTriggerCharacter)
                {
                    return;
                }
            }

            context.IsExclusive = IsExclusive();

            using (Logger.LogBlock(FunctionId.Completion_SymbolCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var regularItems = await GetItemsWorkerAsync(document, position, options, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                context.AddItems(regularItems);

                var preselectedItems = await GetItemsWorkerAsync(document, position, options, preselect: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                context.AddItems(preselectedItems);
            }
        }

        private async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position, OptionSet options, bool preselect, CancellationToken cancellationToken)
        {
            var relatedDocumentIds = document.GetLinkedDocumentIds();
            var relatedDocuments = relatedDocumentIds.Concat(document.Id).Select(document.Project.Solution.GetDocument);
            lock (s_cacheGate)
            {
                // Invalidate the cache if it's for a different position or a different set of Documents.
                // It's fairly likely that we'll only have to check the first document, unless someone
                // specially constructed a Solution with mismatched linked files.
                if (s_cachedPosition != position ||
                    !relatedDocuments.All((Document d) => s_cachedDocuments.TryGetValue(d, out var value)))
                {
                    s_cachedPosition = position;
                    foreach (var related in relatedDocuments)
                    {
                        s_cachedDocuments.Remove(document);
                    }
                }
            }

            var context = await GetOrCreateContext(document, position, cancellationToken).ConfigureAwait(false);
            options = GetUpdatedRecommendationOptions(options, document.Project.Language);

            if (!relatedDocumentIds.Any())
            {
                IEnumerable<(ISymbol, CompletionItemRules)> itemsForCurrentDocument = await GetItemsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);

                itemsForCurrentDocument = itemsForCurrentDocument ?? SpecializedCollections.EmptyEnumerable<(ISymbol, CompletionItemRules)>();
                return CreateItems(itemsForCurrentDocument, context,
                    invalidProjectMap: null,
                    totalProjects: null,
                    preselect: preselect);
            }

            var contextAndCompletionItemLists = await GetPerContextItems(document, position, options, new[] { document.Id }.Concat(relatedDocumentIds), preselect, cancellationToken).ConfigureAwait(false);
            var unionedSymbolsList = UnionSymbols(contextAndCompletionItemLists, out var originatingContextMap);
            var missingSymbolsMap = FindSymbolsMissingInLinkedContexts(unionedSymbolsList, contextAndCompletionItemLists);
            var totalProjects = contextAndCompletionItemLists.Select(t => t.Item1.ProjectId).ToList();

            return CreateItems(unionedSymbolsList, originatingContextMap, missingSymbolsMap, totalProjects, preselect);
        }

        protected virtual bool IsExclusive()
        {
            return false;
        }

        protected virtual Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            return SpecializedTasks.True;
        }

        private async Task<ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>> GetItemsWorker(int position, bool preselect, SyntaxContext context, OptionSet options, CancellationToken cancellationToken)
        {
            try
            {
                if (preselect)
                {
                    return (await GetPreselectedItemsWorker(context, position, options, cancellationToken).ConfigureAwait(false));
                }

                return (await GetSymbolsWorker(context, position, options, cancellationToken).ConfigureAwait(false))
                        .Select(s => (s, CompletionItemRules.Default)).ToImmutableArray();
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private HashSet<(ISymbol, CompletionItemRules)> UnionSymbols(List<Tuple<DocumentId, SyntaxContext, ImmutableArray<(ISymbol symbol, CompletionItemRules)>>> linkedContextCompletionItemLists, out Dictionary<(ISymbol, CompletionItemRules), SyntaxContext> originDictionary)
        {
            // To correctly map symbols back to their SyntaxContext, we do care about assembly identity.
            originDictionary = new Dictionary<(ISymbol, CompletionItemRules), SyntaxContext>(LinkedFilesItemEquivalenceComparer.Instance);

            // We don't care about assembly identity when creating the union.
            var set = new HashSet<(ISymbol, CompletionItemRules)>(LinkedFilesItemEquivalenceComparer.Instance);
            foreach (var linkedContextCompletionItemList in linkedContextCompletionItemLists)
            {
                // We need to use the SemanticModel any particular symbol came from in order to generate its description correctly.
                // Therefore, when we add a symbol to set of union symbols, add a mapping from it to its SyntaxContext.
                foreach (var item in linkedContextCompletionItemList.Item3.GroupBy(item => new { item.symbol.Name, item.symbol.Kind }).Select(g => g.First()))
                {
                    if (set.Add(item))
                    {
                        originDictionary.Add(item, linkedContextCompletionItemList.Item2);
                    }
                }
            }

            return set;
        }

        protected async Task<List<Tuple<DocumentId, SyntaxContext, ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>>>> GetPerContextItems(Document document, int position, OptionSet options, IEnumerable<DocumentId> relatedDocuments, bool preselect, CancellationToken cancellationToken)
        {
            var perContextItems = new List<Tuple<DocumentId, SyntaxContext, ImmutableArray<(ISymbol, CompletionItemRules)>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = document.Project.Solution.GetDocument(relatedDocumentId);
                var context = await GetOrCreateContext(relatedDocument, position, cancellationToken).ConfigureAwait(false);

                if (IsCandidateProject(context, cancellationToken))
                {
                    var items = await GetItemsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);
                    perContextItems.Add(Tuple.Create(relatedDocument.Id, context, items));
                }
            }

            return perContextItems;
        }

        private bool IsCandidateProject(SyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxFacts = context.GetLanguageService<ISyntaxFactsService>();
            return !syntaxFacts.IsInInactiveRegion(context.SyntaxTree, context.Position, cancellationToken);
        }

        protected OptionSet GetUpdatedRecommendationOptions(OptionSet options, string language)
        {
            var filterOutOfScopeLocals = options.GetOption(CompletionControllerOptions.FilterOutOfScopeLocals);
            var hideAdvancedMembers = options.GetOption(CompletionOptions.HideAdvancedMembers, language);

            return options
                .WithChangedOption(RecommendationOptions.FilterOutOfScopeLocals, language, filterOutOfScopeLocals)
                .WithChangedOption(RecommendationOptions.HideAdvancedMembers, language, hideAdvancedMembers);
        }

        protected abstract Task<SyntaxContext> CreateContext(Document document, int position, CancellationToken cancellationToken);

        private Task<SyntaxContext> GetOrCreateContext(Document document, int position, CancellationToken cancellationToken)
        {
            lock (s_cacheGate)
            {
                return s_cachedDocuments.GetValue(document, d => CreateContext(d, position, cancellationToken));
            }
        }

        /// <summary>
        /// Given a list of symbols, determine which are not recommended at the same position in linked documents.
        /// </summary>
        /// <param name="expectedSymbols">The symbols recommended in the active context.</param>
        /// <param name="linkedContextItemLists">The symbols recommended in linked documents</param>
        /// <returns>The list of projects each recommended symbol did NOT appear in.</returns>
        protected Dictionary<(ISymbol, CompletionItemRules), List<ProjectId>> FindSymbolsMissingInLinkedContexts(
            HashSet<(ISymbol symbol, CompletionItemRules)> expectedSymbols,
            IEnumerable<Tuple<DocumentId, SyntaxContext, ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>>> linkedContextItemLists)
        {
            var missingSymbols = new Dictionary<(ISymbol, CompletionItemRules), List<ProjectId>>(LinkedFilesItemEquivalenceComparer.Instance);

            foreach (var linkedContextItemList in linkedContextItemLists)
            {
                ImmutableArray<(ISymbol symbol, CompletionItemRules)> v = linkedContextItemList.Item3;
                var symbolsMissingInLinkedContext = expectedSymbols.Except(v, LinkedFilesItemEquivalenceComparer.Instance);
                foreach (var missingSymbol in symbolsMissingInLinkedContext)
                {
                    missingSymbols.GetOrAdd(missingSymbol, (m) => new List<ProjectId>()).Add(linkedContextItemList.Item1.ProjectId);
                }
            }

            return missingSymbols;
        }

        public override Task<TextChange?> GetTextChangeAsync(
            Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult<TextChange?>(new TextChange(
                selectedItem.Span, GetInsertionText(selectedItem, ch)));
        }

        private string GetInsertionText(CompletionItem item, char? ch)
        {
            return ch == null
                ? SymbolCompletionItem.GetInsertionText(item)
                : GetInsertionText(item, ch.Value);
        }

        /// <summary>
        /// Override this if you want to provide customized insertion based on the character typed.
        /// </summary>
        protected virtual string GetInsertionText(CompletionItem item, char ch)
        {
            return SymbolCompletionItem.GetInsertionText(item);
        }
    }
}