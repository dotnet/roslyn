// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        protected abstract ValueTuple<string, string> GetDisplayAndInsertionText(ISymbol symbol, SyntaxContext context);
        protected abstract CompletionItemRules GetCompletionItemRules(IReadOnlyList<ISymbol> symbols, SyntaxContext context);

        /// <summary>
        /// Given a list of symbols, creates the list of completion items for them.
        /// </summary>
        protected IEnumerable<CompletionItem> CreateItems(
            IEnumerable<ISymbol> symbols,
            SyntaxContext context,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            var tree = context.SyntaxTree;

            var q = from symbol in symbols
                    let texts = GetDisplayAndInsertionText(symbol, context)
                    group symbol by texts into g
                    select this.CreateItem(g.Key.Item1, g.Key.Item2, g.ToList(), context, invalidProjectMap, totalProjects, preselect);

            return q.ToList();
        }

        /// <summary>
        /// Given a list of symbols, and a mapping from each symbol to its original SemanticModel, creates the list of completion items for them.
        /// </summary>
        protected IEnumerable<CompletionItem> CreateItems(
            IEnumerable<ISymbol> symbols,
            Dictionary<ISymbol, SyntaxContext> originatingContextMap,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            var q = from symbol in symbols
                    let texts = GetDisplayAndInsertionText(symbol, originatingContextMap[symbol])
                    group symbol by texts into g
                    select this.CreateItem(g.Key.Item1, g.Key.Item2, g.ToList(), originatingContextMap[g.First()], invalidProjectMap, totalProjects, preselect);

            return q.ToList();
        }

        /// <summary>
        /// Given a Symbol, creates the completion item for it.
        /// </summary>
        private CompletionItem CreateItem(
            string displayText,
            string insertionText,
            List<ISymbol> symbols,
            SyntaxContext context,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect)
        {
            Contract.ThrowIfNull(symbols);

            SupportedPlatformData supportedPlatformData = null;
            if (invalidProjectMap != null)
            {
                List<ProjectId> invalidProjects = null;
                foreach (var symbol in symbols)
                {
                    if (invalidProjectMap.TryGetValue(symbol, out invalidProjects))
                    {
                        break;
                    }
                }

                if (invalidProjects != null)
                {
                    supportedPlatformData = new SupportedPlatformData(invalidProjects, totalProjects, context.Workspace);
                }
            }

            return CreateItem(displayText, insertionText, symbols, context, preselect, supportedPlatformData);
        }

        protected virtual CompletionItem CreateItem(
            string displayText, string insertionText,
            List<ISymbol> symbols,
            SyntaxContext context, bool preselect,
            SupportedPlatformData supportedPlatformData)
        {
            return SymbolCompletionItem.Create(
                displayText: displayText,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                contextPosition: context.Position,
                symbols: symbols,
                supportedPlatforms: supportedPlatformData,
                matchPriority: preselect ? MatchPriority.Preselect : MatchPriority.Default,
                rules: GetCompletionItemRules(symbols, context),
                properties: GetInitialProperties(symbols[0], context));
        }

        protected virtual ImmutableDictionary<string, string> GetInitialProperties(
            ISymbol symbol, SyntaxContext context)
        {
            return null;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
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

        protected virtual Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

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
                Task<SyntaxContext> value;
                if (s_cachedPosition != position ||
                    !relatedDocuments.All((Document d) => s_cachedDocuments.TryGetValue(d, out value)))
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
                IEnumerable<ISymbol> itemsForCurrentDocument = await GetSymbolsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);

                itemsForCurrentDocument = itemsForCurrentDocument ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
                return CreateItems(itemsForCurrentDocument, context,
                    invalidProjectMap: null,
                    totalProjects: null,
                    preselect: preselect);
            }

            var contextAndSymbolLists = await GetPerContextSymbols(document, position, options, new[] { document.Id }.Concat(relatedDocumentIds), preselect, cancellationToken).ConfigureAwait(false);

            Dictionary<ISymbol, SyntaxContext> originatingContextMap = null;
            var unionedSymbolsList = UnionSymbols(contextAndSymbolLists, out originatingContextMap);
            var missingSymbolsMap = FindSymbolsMissingInLinkedContexts(unionedSymbolsList, contextAndSymbolLists);
            var totalProjects = contextAndSymbolLists.Select(t => t.Item1.ProjectId).ToList();

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

        private Task<ImmutableArray<ISymbol>> GetSymbolsWorker(int position, bool preselect, SyntaxContext context, OptionSet options, CancellationToken cancellationToken)
        {
            return preselect
                ? GetPreselectedSymbolsWorker(context, position, options, cancellationToken)
                : GetSymbolsWorker(context, position, options, cancellationToken);
        }

        private HashSet<ISymbol> UnionSymbols(List<Tuple<DocumentId, SyntaxContext, ImmutableArray<ISymbol>>> linkedContextSymbolLists, out Dictionary<ISymbol, SyntaxContext> originDictionary)
        {
            // To correctly map symbols back to their SyntaxContext, we do care about assembly identity.
            originDictionary = new Dictionary<ISymbol, SyntaxContext>(LinkedFilesSymbolEquivalenceComparer.Instance);

            // We don't care about assembly identity when creating the union.
            var set = new HashSet<ISymbol>(LinkedFilesSymbolEquivalenceComparer.Instance);
            foreach (var linkedContextSymbolList in linkedContextSymbolLists)
            {
                // We need to use the SemanticModel any particular symbol came from in order to generate its description correctly.
                // Therefore, when we add a symbol to set of union symbols, add a mapping from it to its SyntaxContext.
                foreach (var symbol in linkedContextSymbolList.Item3.GroupBy(s => new { s.Name, s.Kind }).Select(g => g.First()))
                {
                    if (set.Add(symbol))
                    {
                        originDictionary.Add(symbol, linkedContextSymbolList.Item2);
                    }
                }
            }

            return set;
        }

        protected async Task<List<Tuple<DocumentId, SyntaxContext, ImmutableArray<ISymbol>>>> GetPerContextSymbols(Document document, int position, OptionSet options, IEnumerable<DocumentId> relatedDocuments, bool preselect, CancellationToken cancellationToken)
        {
            var perContextSymbols = new List<Tuple<DocumentId, SyntaxContext, ImmutableArray<ISymbol>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = document.Project.Solution.GetDocument(relatedDocumentId);
                var context = await GetOrCreateContext(relatedDocument, position, cancellationToken).ConfigureAwait(false);

                if (IsCandidateProject(context, cancellationToken))
                {
                    var symbols = await GetSymbolsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);
                    perContextSymbols.Add(Tuple.Create(relatedDocument.Id, context, symbols));
                }
            }

            return perContextSymbols;
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
        /// <param name="linkedContextSymbolLists">The symbols recommended in linked documents</param>
        /// <returns>The list of projects each recommended symbol did NOT appear in.</returns>
        protected Dictionary<ISymbol, List<ProjectId>> FindSymbolsMissingInLinkedContexts(
            HashSet<ISymbol> expectedSymbols, 
            IEnumerable<Tuple<DocumentId, SyntaxContext, ImmutableArray<ISymbol>>> linkedContextSymbolLists)
        {
            var missingSymbols = new Dictionary<ISymbol, List<ProjectId>>(LinkedFilesSymbolEquivalenceComparer.Instance);

            foreach (var linkedContextSymbolList in linkedContextSymbolLists)
            {
                var symbolsMissingInLinkedContext = expectedSymbols.Except(linkedContextSymbolList.Item3, LinkedFilesSymbolEquivalenceComparer.Instance);
                foreach (var missingSymbol in symbolsMissingInLinkedContext)
                {
                    missingSymbols.GetOrAdd(missingSymbol, (m) => new List<ProjectId>()).Add(linkedContextSymbolList.Item1.ProjectId);
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

        /// <summary>
        /// Override this if you want to provide customized insertion based on the character typed.
        /// </summary>
        private string GetInsertionText(CompletionItem item, char? ch)
        {
            return ch == null
                ? SymbolCompletionItem.GetInsertionText(item)
                : GetInsertionText(item, ch.Value);
        }

        protected virtual string GetInsertionText(CompletionItem item, char ch)
        {
            return SymbolCompletionItem.GetInsertionText(item);
        }
    }
}