// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    internal abstract partial class AbstractSymbolCompletionProvider : AbstractCompletionProvider
    {
        // PERF: Many CompletionProviders derive AbstractSymbolCompletionProvider and therefore
        // compute identical contexts. This actually shows up on the 2-core typing test.
        // Cache the most recent document/position/computed SyntaxContext to reduce repeat computation.
        private static Dictionary<Document, Task<AbstractSyntaxContext>> s_cachedDocuments = new Dictionary<Document, Task<AbstractSyntaxContext>>();
        private static int s_cachedPosition;
        private static readonly object s_cacheGate = new object();

        protected AbstractSymbolCompletionProvider()
        {
        }

        protected abstract ValueTuple<string, string> GetDisplayAndInsertionText(ISymbol symbol, AbstractSyntaxContext context);

        protected abstract string GetInsertionText(ISymbol symbol, AbstractSyntaxContext context, char ch);
        protected abstract TextSpan GetTextChangeSpan(SourceText text, int position);

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            var symbolItem = (SymbolCompletionItem)selectedItem;
            var insertionText = ch == null
                ? symbolItem.InsertionText
                : GetInsertionText(symbolItem, ch.Value);

            return new TextChange(selectedItem.FilterSpan, insertionText);
        }

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
                preselect: preselect);
        }

        private string GetInsertionText(SymbolCompletionItem symbolItem, char ch)
        {
            return GetInsertionText(symbolItem.Symbols[0], symbolItem.Context, ch);
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

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position, CompletionTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_SymbolCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var regularItems = await GetItemsWorkerAsync(document, position, triggerInfo, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                var preselectedItems = await GetItemsWorkerAsync(document, position, triggerInfo, preselect: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return regularItems.Concat(preselectedItems);
            }
        }

        private async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, bool preselect, CancellationToken cancellationToken)
        {
            var relatedDocumentIds = document.GetLinkedDocumentIds();
            var relatedDocuments = relatedDocumentIds.Concat(document.Id).Select(document.Project.Solution.GetDocument);
            lock (s_cacheGate)
            {
                // Invalidate the cache if it's for a different position or a different set of Documents.
                // It's fairly likely that we'll only have to check the first document, unless someone
                // specially constructed a Solution with mismatched linked files.
                if (s_cachedPosition != position ||
                    !relatedDocuments.All(s_cachedDocuments.ContainsKey))
                {
                    s_cachedPosition = position;
                    s_cachedDocuments.Clear();
                    foreach (var related in relatedDocuments)
                    {
                        s_cachedDocuments.Add(related, null);
                    }
                }
            }

            var context = await GetOrCreateContext(document, position, cancellationToken).ConfigureAwait(false);
            var options = GetOptions(document, triggerInfo, context);

            if (!relatedDocumentIds.Any())
            {
                IEnumerable<ISymbol> itemsForCurrentDocument = await GetSymbolsWorker(position, preselect, context, options, cancellationToken).ConfigureAwait(false);

                itemsForCurrentDocument = itemsForCurrentDocument ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
                return await CreateItemsAsync(position, itemsForCurrentDocument, context, null, null, preselect, cancellationToken).ConfigureAwait(false);
            }

            var contextAndSymbolLists = await GetPerContextSymbols(document, position, options, new[] { document.Id }.Concat(relatedDocumentIds), preselect, cancellationToken).ConfigureAwait(false);

            Dictionary<ISymbol, AbstractSyntaxContext> orignatingContextMap = null;
            var unionedSymbolsList = UnionSymbols(contextAndSymbolLists, out orignatingContextMap);
            var missingSymbolsMap = FindSymbolsMissingInLinkedContexts(unionedSymbolsList, contextAndSymbolLists);
            var totalProjects = contextAndSymbolLists.Select(t => t.Item1.ProjectId).ToList();

            var textChangeSpan = await GetTextChangeSpanAsync(position, context, cancellationToken).ConfigureAwait(false);

            return CreateItems(position, unionedSymbolsList, textChangeSpan, orignatingContextMap, missingSymbolsMap, totalProjects, preselect: preselect, cancellationToken: cancellationToken);
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

        protected OptionSet GetOptions(Document document, CompletionTriggerInfo triggerInfo, AbstractSyntaxContext context)
        {
            var optionService = context.GetWorkspaceService<IOptionService>();
            var filterOutOfScopeLocals = !triggerInfo.IsDebugger;
            var hideAdvancedMembers = document.ShouldHideAdvancedMembers();
            var options = optionService
                .GetOptions()
                .WithChangedOption(RecommendationOptions.FilterOutOfScopeLocals, context.SemanticModel.Language, filterOutOfScopeLocals)
                .WithChangedOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language, hideAdvancedMembers);
            return options;
        }

        protected abstract Task<AbstractSyntaxContext> CreateContext(Document document, int position, CancellationToken cancellationToken);

        private Task<AbstractSyntaxContext> GetOrCreateContext(Document document, int position, CancellationToken cancellationToken)
        {
            lock (s_cacheGate)
            {
                Task<AbstractSyntaxContext> cachedContext;
                if (s_cachedDocuments.TryGetValue(document, out cachedContext) && cachedContext != null)
                {
                    return cachedContext;
                }
            }

            var context = CreateContext(document, position, cancellationToken);
            lock (s_cacheGate)
            {
                s_cachedDocuments[document] = context;
                return context;
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
