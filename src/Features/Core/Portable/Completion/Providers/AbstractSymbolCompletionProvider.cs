// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal abstract partial class AbstractSymbolCompletionProvider : CompletionListProvider
    {
        // PERF: Many CompletionProviders derive AbstractSymbolCompletionProvider and therefore
        // compute identical contexts. This actually shows up on the 2-core typing test.
        // Cache the most recent document/position/computed SyntaxContext to reduce repeat computation.
        private static readonly ConditionalWeakTable<Document, Task<AbstractSyntaxContext>> s_cachedDocuments = new ConditionalWeakTable<Document, Task<AbstractSyntaxContext>>();
        private static int s_cachedPosition;
        private static readonly object s_cacheGate = new object();

        protected AbstractSymbolCompletionProvider()
        {
        }

        protected abstract ValueTuple<string, string> GetDisplayAndInsertionText(ISymbol symbol, AbstractSyntaxContext context);

        protected abstract TextSpan GetTextChangeSpan(SourceText text, int position);
        protected abstract CompletionItemRules GetCompletionItemRules();

        /// <summary>
        /// Given a list of symbols, creates the list of completion items for them.
        /// </summary>
        protected async Task<IEnumerable<CompletionItem>> CreateItemsAsync(
            int position,
            IEnumerable<ISymbol> symbols,
            AbstractSyntaxContext context,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect,
            CancellationToken cancellationToken)
        {
            var tree = context.SyntaxTree;

            var text = await context.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textChangeSpan = this.GetTextChangeSpan(text, position);
            var q = from symbol in symbols
                    let texts = GetDisplayAndInsertionText(symbol, context)
                    group symbol by texts into g
                    select this.CreateItem(g.Key, position, g.ToList(), context, textChangeSpan, invalidProjectMap, totalProjects, preselect, cancellationToken);

            return q.ToList();
        }

        /// <summary>
        /// Given a list of symbols, and a mapping from each symbol to its original SemanticModel, creates the list of completion items for them.
        /// </summary>
        protected IEnumerable<CompletionItem> CreateItems(
            int position,
            IEnumerable<ISymbol> symbols,
            TextSpan textChangeSpan,
            Dictionary<ISymbol, AbstractSyntaxContext> originatingContextMap,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect,
            CancellationToken cancellationToken)
        {
            var q = from symbol in symbols
                    let texts = GetDisplayAndInsertionText(symbol, originatingContextMap[symbol])
                    group symbol by texts into g
                    select this.CreateItem(g.Key, position, g.ToList(), originatingContextMap[g.First()], textChangeSpan, invalidProjectMap, totalProjects, preselect, cancellationToken);

            return q.ToList();
        }

        /// <summary>
        /// Given a Symbol, creates the completion item for it.
        /// </summary>
        private CompletionItem CreateItem(
            ValueTuple<string, string> displayAndInsertionText,
            int position,
            List<ISymbol> symbols,
            AbstractSyntaxContext context,
            TextSpan textChangeSpan,
            Dictionary<ISymbol, List<ProjectId>> invalidProjectMap,
            List<ProjectId> totalProjects,
            bool preselect,
            CancellationToken cancellationToken)
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

            return CreateItem(displayAndInsertionText, position, symbols, context, textChangeSpan, preselect, supportedPlatformData);
        }

        protected virtual CompletionItem CreateItem(ValueTuple<string, string> displayAndInsertionText, int position, List<ISymbol> symbols, AbstractSyntaxContext context, TextSpan textChangeSpan, bool preselect, SupportedPlatformData supportedPlatformData)
        {
            return new SymbolCompletionItem(
                this,
                displayAndInsertionText.Item1,
                displayAndInsertionText.Item2,
                GetFilterText(symbols[0], displayAndInsertionText.Item1, context),
                textChangeSpan,
                position,
                symbols,
                context,
                supportedPlatforms: supportedPlatformData,
                preselect: preselect,
                rules: GetCompletionItemRules());
        }

        protected virtual string GetFilterText(ISymbol symbol, string displayText, AbstractSyntaxContext context)
        {
            return (displayText == symbol.Name) ||
                (displayText.Length > 0 && displayText[0] == '@') ||
                (context.IsAttributeNameContext && symbol.IsAttribute())
                ? displayText
                : symbol.Name;
        }

        protected abstract Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken);

        protected virtual Task<IEnumerable<ISymbol>> GetPreselectedSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyEnumerable<ISymbol>();
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var triggerInfo = context.TriggerInfo;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            // If we were triggered by typing a character, then do a semantic check to make sure
            // we're still applicable.  If not, then return immediately.
            if (triggerInfo.TriggerReason == CompletionTriggerReason.TypeCharCommand)
            {
                var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
                if (!isSemanticTriggerCharacter)
                {
                    return;
                }
            }

            if (IsExclusive())
            {
                context.MakeExclusive(true);
            }

            using (Logger.LogBlock(FunctionId.Completion_SymbolCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var regularItems = await GetItemsWorkerAsync(document, position, triggerInfo, options, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                context.AddItems(regularItems);

                var preselectedItems = await GetItemsWorkerAsync(document, position, triggerInfo, options, preselect: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                context.AddItems(preselectedItems);
            }
        }

        private async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, OptionSet options, bool preselect, CancellationToken cancellationToken)
        {
            var relatedDocumentIds = document.GetLinkedDocumentIds();
            var relatedDocuments = relatedDocumentIds.Concat(document.Id).Select(document.Project.Solution.GetDocument);
            lock (s_cacheGate)
            {
                // Invalidate the cache if it's for a different position or a different set of Documents.
                // It's fairly likely that we'll only have to check the first document, unless someone
                // specially constructed a Solution with mismatched linked files.
                Task<AbstractSyntaxContext> value;
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
                return await CreateItemsAsync(position, itemsForCurrentDocument, context, null, null, preselect, cancellationToken).ConfigureAwait(false);
            }

            var contextAndSymbolLists = await GetPerContextSymbols(document, position, options, new[] { document.Id }.Concat(relatedDocumentIds), preselect, cancellationToken).ConfigureAwait(false);

            Dictionary<ISymbol, AbstractSyntaxContext> originatingContextMap = null;
            var unionedSymbolsList = UnionSymbols(contextAndSymbolLists, out originatingContextMap);
            var missingSymbolsMap = FindSymbolsMissingInLinkedContexts(unionedSymbolsList, contextAndSymbolLists);
            var totalProjects = contextAndSymbolLists.Select(t => t.Item1.ProjectId).ToList();

            var textChangeSpan = await GetTextChangeSpanAsync(position, context, cancellationToken).ConfigureAwait(false);

            return CreateItems(position, unionedSymbolsList, textChangeSpan, originatingContextMap, missingSymbolsMap, totalProjects, preselect: preselect, cancellationToken: cancellationToken);
        }

        protected virtual bool IsExclusive()
        {
            return false;
        }

        protected virtual Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            return SpecializedTasks.True;
        }

        private async Task<TextSpan> GetTextChangeSpanAsync(int position, AbstractSyntaxContext context, CancellationToken cancellationToken)
        {
            var text = await context.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return GetTextChangeSpan(text, position);
        }

        private Task<IEnumerable<ISymbol>> GetSymbolsWorker(int position, bool preselect, AbstractSyntaxContext context, OptionSet options, CancellationToken cancellationToken)
        {
            return preselect
                ? GetPreselectedSymbolsWorker(context, position, options, cancellationToken)
                : GetSymbolsWorker(context, position, options, cancellationToken);
        }

        private HashSet<ISymbol> UnionSymbols(List<Tuple<DocumentId, AbstractSyntaxContext, IEnumerable<ISymbol>>> linkedContextSymbolLists, out Dictionary<ISymbol, AbstractSyntaxContext> originDictionary)
        {
            // To correctly map symbols back to their SyntaxContext, we do care about assembly identity.
            originDictionary = new Dictionary<ISymbol, AbstractSyntaxContext>(LinkedFilesSymbolEquivalenceComparer.Instance);

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

        protected async Task<List<Tuple<DocumentId, AbstractSyntaxContext, IEnumerable<ISymbol>>>> GetPerContextSymbols(Document document, int position, OptionSet options, IEnumerable<DocumentId> relatedDocuments, bool preselect, CancellationToken cancellationToken)
        {
            var perContextSymbols = new List<Tuple<DocumentId, AbstractSyntaxContext, IEnumerable<ISymbol>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = document.Project.Solution.GetDocument(relatedDocumentId);
                var context = await GetOrCreateContext(relatedDocument, position, cancellationToken).ConfigureAwait(false);

                if (IsCandidateProject(context, cancellationToken))
                {
                    var symbols = await GetSymbolsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);
                    perContextSymbols.Add(Tuple.Create(relatedDocument.Id, context, symbols ?? SpecializedCollections.EmptyEnumerable<ISymbol>()));
                }
            }

            return perContextSymbols;
        }

        private bool IsCandidateProject(AbstractSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxFacts = context.GetLanguageService<ISyntaxFactsService>();
            return !syntaxFacts.IsInInactiveRegion(context.SyntaxTree, context.Position, cancellationToken);
        }

        protected OptionSet GetUpdatedRecommendationOptions(OptionSet options, string language)
        {
            var filterOutOfScopeLocals = options.GetOption(CompletionOptions.FilterOutOfScopeLocals);
            var hideAdvancedMembers = options.GetOption(CompletionOptions.HideAdvancedMembers, language);

            return options
                .WithChangedOption(RecommendationOptions.FilterOutOfScopeLocals, language, filterOutOfScopeLocals)
                .WithChangedOption(RecommendationOptions.HideAdvancedMembers, language, hideAdvancedMembers);
        }

        protected abstract Task<AbstractSyntaxContext> CreateContext(Document document, int position, CancellationToken cancellationToken);

        private Task<AbstractSyntaxContext> GetOrCreateContext(Document document, int position, CancellationToken cancellationToken)
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
        protected Dictionary<ISymbol, List<ProjectId>> FindSymbolsMissingInLinkedContexts(HashSet<ISymbol> expectedSymbols, IEnumerable<Tuple<DocumentId, AbstractSyntaxContext, IEnumerable<ISymbol>>> linkedContextSymbolLists)
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
    }
}
