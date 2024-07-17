// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract partial class AbstractSymbolCompletionProvider<TSyntaxContext> : LSPCompletionProvider
    where TSyntaxContext : SyntaxContext
{
    protected AbstractSymbolCompletionProvider()
    {
    }

    protected abstract (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, TSyntaxContext context);

    protected abstract Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext? completionContext,
        TSyntaxContext syntaxContext,
        int position,
        CompletionOptions options,
        CancellationToken cancellationToken);

    protected abstract CompletionItem CreateItem(
        CompletionContext completionContext,
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        TSyntaxContext context,
        SupportedPlatformData? supportedPlatformData);

    /// <param name="typeConvertibilityCache">A cache to use for repeated lookups. This should be created with <see cref="SymbolEqualityComparer.Default"/>
    /// because we ignore nullability.</param>
    private static bool ShouldIncludeInTargetTypedCompletionList(
        ISymbol symbol,
        ImmutableArray<ITypeSymbol> inferredTypes,
        SemanticModel semanticModel,
        int position,
        Dictionary<ITypeSymbol, bool> typeConvertibilityCache)
    {
        // When searching for identifiers of type C, exclude the symbol for the `C` type itself.
        if (symbol.Kind == SymbolKind.NamedType)
        {
            return false;
        }

        // Avoid offering members of object since they too commonly show up and are infrequently desired.
        if (symbol.ContainingType?.SpecialType == SpecialType.System_Object)
        {
            return false;
        }

        // Don't offer locals on the right-hand-side of their declaration: `int x = x`
        if (symbol.Kind == SymbolKind.Local)
        {
            var local = (ILocalSymbol)symbol;
            var declarationSyntax = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).SingleOrDefault();
            if (declarationSyntax != null && position < declarationSyntax.FullSpan.End)
            {
                return false;
            }
        }

        var type = symbol.GetMemberType() ?? symbol.GetSymbolType();
        if (type == null)
        {
            return false;
        }

        if (typeConvertibilityCache.TryGetValue(type, out var isConvertible))
        {
            return isConvertible;
        }

        typeConvertibilityCache[type] = CompletionUtilities.IsTypeImplicitlyConvertible(semanticModel.Compilation, type, inferredTypes);
        return typeConvertibilityCache[type];
    }

    /// <summary>
    /// Given a list of symbols, and a mapping from each symbol to its original SemanticModel, 
    /// creates the list of completion items for them.
    /// </summary>
    private ImmutableArray<CompletionItem> CreateItems(
        CompletionContext completionContext,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        Func<SymbolAndSelectionInfo, TSyntaxContext> contextLookup,
        Dictionary<ISymbol, List<ProjectId>>? invalidProjectMap,
        List<ProjectId>? totalProjects)
    {
        // We might get symbol w/o name but CanBeReferencedByName is still set to true, 
        // need to filter them out.
        // https://github.com/dotnet/roslyn/issues/47690
        var symbolGroups = from symbol in symbols
                           let texts = GetDisplayAndSuffixAndInsertionText(symbol.Symbol, contextLookup(symbol))
                           where !string.IsNullOrWhiteSpace(texts.displayText)
                           group symbol by texts into g
                           select g;

        using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var itemListBuilder);
        var typeConvertibilityCache = new Dictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);

        foreach (var symbolGroup in symbolGroups)
        {
            var includeItemInTargetTypedCompletion = false;
            var arbitraryFirstContext = contextLookup(symbolGroup.First());
            var symbolList = symbolGroup.ToImmutableArray();

            if (completionContext.CompletionOptions.TargetTypedCompletionFilter)
            {
                includeItemInTargetTypedCompletion = TryFindFirstSymbolMatchesTargetTypes(contextLookup, symbolList, typeConvertibilityCache, out var index);
                if (includeItemInTargetTypedCompletion && index > 0)
                {
                    // This would ensure a symbol matches target types to be used for description if there's any,
                    // assuming the default implementation of GetDescriptionWorkerAsync is used.
                    var firstMatch = symbolList[index];
                    symbolList = symbolList.RemoveAt(index);
                    symbolList = symbolList.Insert(0, firstMatch);
                }
            }

            var supportedPlatformData = ComputeSupportedPlatformData(completionContext, symbolList, invalidProjectMap, totalProjects);
            var item = CreateItem(
                completionContext, symbolGroup.Key.displayText, symbolGroup.Key.suffix, symbolGroup.Key.insertionText, symbolList, arbitraryFirstContext, supportedPlatformData);

            if (includeItemInTargetTypedCompletion)
            {
                item = item.AddTag(WellKnownTags.TargetTypeMatch);
            }

            itemListBuilder.Add(item);
        }

        return itemListBuilder.ToImmutableAndClear();
    }

    protected static bool TryFindFirstSymbolMatchesTargetTypes(
        Func<SymbolAndSelectionInfo, TSyntaxContext> contextLookup,
        ImmutableArray<SymbolAndSelectionInfo> symbolList,
        Dictionary<ITypeSymbol, bool> typeConvertibilityCache,
        out int index)
    {
        for (index = 0; index < symbolList.Length; ++index)
        {
            var symbol = symbolList[index];
            var syntaxContext = contextLookup(symbol);
            if (ShouldIncludeInTargetTypedCompletionList(symbol.Symbol, syntaxContext.InferredTypes, syntaxContext.SemanticModel, syntaxContext.Position, typeConvertibilityCache))
                break;
        }

        return index < symbolList.Length;
    }

    private static SupportedPlatformData? ComputeSupportedPlatformData(
        CompletionContext completionContext,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        Dictionary<ISymbol, List<ProjectId>>? invalidProjectMap,
        List<ProjectId>? totalProjects)
    {
        SupportedPlatformData? supportedPlatformData = null;
        if (invalidProjectMap != null)
        {
            List<ProjectId>? invalidProjects = null;
            foreach (var symbol in symbols)
            {
                if (invalidProjectMap.TryGetValue(symbol.Symbol, out invalidProjects))
                    break;
            }

            if (invalidProjects != null)
                supportedPlatformData = new SupportedPlatformData(completionContext.Document.Project.Solution, invalidProjects, totalProjects);
        }

        return supportedPlatformData;
    }

    protected static CompletionItem CreateItemDefault(
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        TSyntaxContext context,
        SupportedPlatformData? supportedPlatformData)
    {
        var preselect = symbols.Any(static t => t.Preselect);
        return SymbolCompletionItem.CreateWithSymbolId(
            displayText: displayText,
            displayTextSuffix: displayTextSuffix,
            insertionText: insertionText,
            filterText: GetFilterTextDefault(symbols[0].Symbol, displayText, context),
            contextPosition: context.Position,
            symbols: symbols.SelectAsArray(t => t.Symbol),
            supportedPlatforms: supportedPlatformData,
            rules: CompletionItemRules.Default
                .WithMatchPriority(preselect ? MatchPriority.Preselect : MatchPriority.Default)
                .WithSelectionBehavior(context.IsRightSideOfNumericType ? CompletionItemSelectionBehavior.SoftSelection : CompletionItemSelectionBehavior.Default));
    }

    protected static string GetFilterTextDefault(ISymbol symbol, string displayText, TSyntaxContext context)
    {
        return displayText == symbol.Name || displayText is ['@', ..] || (context.IsAttributeNameContext && symbol.IsAttribute())
            ? displayText
            : symbol.Name;
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
    {
        try
        {
            var document = completionContext.Document;
            var position = completionContext.Position;
            var options = completionContext.CompletionOptions;
            var cancellationToken = completionContext.CancellationToken;

            // If we were triggered by typing a character, then do a semantic check to make sure
            // we're still applicable.  If not, then return immediately.
            if (completionContext.Trigger.Kind == CompletionTriggerKind.Insertion)
            {
                var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
                if (!isSemanticTriggerCharacter)
                {
                    return;
                }
            }

            completionContext.IsExclusive = IsExclusive();

            using (Logger.LogBlock(FunctionId.Completion_SymbolCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var syntaxContext = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false) as TSyntaxContext;
                Contract.ThrowIfNull(syntaxContext);

                var regularItems = await GetItemsAsync(completionContext, syntaxContext, document, position, options, cancellationToken).ConfigureAwait(false);
                completionContext.AddItems(regularItems);
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private async Task<ImmutableArray<CompletionItem>> GetItemsAsync(
        CompletionContext completionContext,
        TSyntaxContext syntaxContext,
        Document document,
        int position,
        CompletionOptions options,
        CancellationToken cancellationToken)
    {
        var relatedDocumentIds = document.GetLinkedDocumentIds();

        if (relatedDocumentIds.IsEmpty)
        {
            var itemsForCurrentDocument = await GetSymbolsAsync(completionContext, syntaxContext, position, options, cancellationToken).ConfigureAwait(false);
            return CreateItems(completionContext, itemsForCurrentDocument, _ => syntaxContext, invalidProjectMap: null, totalProjects: null);
        }

        using var _ = PooledDictionary<DocumentId, int>.GetInstance(out var documentIdToIndex);
        documentIdToIndex.Add(document.Id, 0);
        foreach (var documentId in relatedDocumentIds)
            documentIdToIndex.Add(documentId, documentIdToIndex.Count);

        var contextAndSymbolLists = await GetPerContextSymbolsAsync(completionContext, document, options, documentIdToIndex.Keys, cancellationToken).ConfigureAwait(false);

        // We want the resultant contexts ordered in the same order the related documents came in.  Importantly, the
        // context for *our* starting document should be placed first.
        contextAndSymbolLists = contextAndSymbolLists
            .OrderBy((tuple1, tuple2) => documentIdToIndex[tuple1.documentId] - documentIdToIndex[tuple2.documentId])
            .ToImmutableArray();

        var symbolToContextMap = UnionSymbols(contextAndSymbolLists);
        var missingSymbolsMap = FindSymbolsMissingInLinkedContexts(symbolToContextMap, contextAndSymbolLists);
        var totalProjects = contextAndSymbolLists.Select(t => t.documentId.ProjectId).ToList();

        return CreateItems(
            completionContext, [.. symbolToContextMap.Keys], symbol => symbolToContextMap[symbol], missingSymbolsMap, totalProjects);
    }

    protected virtual bool IsExclusive()
        => false;

    protected virtual Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        => SpecializedTasks.True;

    private static Dictionary<SymbolAndSelectionInfo, TSyntaxContext> UnionSymbols(
        ImmutableArray<(DocumentId documentId, TSyntaxContext syntaxContext, ImmutableArray<SymbolAndSelectionInfo> symbols)> linkedContextSymbolLists)
    {
        // To correctly map symbols back to their SyntaxContext, we do care about assembly identity.
        // We don't care about assembly identity when creating the union.
        var result = new Dictionary<SymbolAndSelectionInfo, TSyntaxContext>();
        foreach (var (documentId, syntaxContext, symbols) in linkedContextSymbolLists)
        {
            // We need to use the SemanticModel any particular symbol came from in order to generate its description correctly.
            // Therefore, when we add a symbol to set of union symbols, add a mapping from it to its SyntaxContext.
            foreach (var symbol in symbols.GroupBy(s => new { s.Symbol.Name, s.Symbol.Kind }).Select(g => g.First()))
                result.TryAdd(symbol, syntaxContext);
        }

        return result;
    }

    private async Task<ImmutableArray<(DocumentId documentId, TSyntaxContext syntaxContext, ImmutableArray<SymbolAndSelectionInfo> symbols)>> GetPerContextSymbolsAsync(
        CompletionContext completionContext, Document document, CompletionOptions options, IEnumerable<DocumentId> relatedDocuments, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        return await ProducerConsumer<(DocumentId documentId, TSyntaxContext syntaxContext, ImmutableArray<SymbolAndSelectionInfo> symbols)>.RunParallelAsync(
            source: relatedDocuments,
            produceItems: static async (relatedDocumentId, callback, args, cancellationToken) =>
            {
                var (@this, solution, completionContext, options) = args;
                var relatedDocument = solution.GetRequiredDocument(relatedDocumentId);
                var syntaxContext = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(
                    relatedDocument, cancellationToken).ConfigureAwait(false) as TSyntaxContext;

                Contract.ThrowIfNull(syntaxContext);
                var symbols = await @this.TryGetSymbolsForContextAsync(
                    completionContext, syntaxContext, options, cancellationToken).ConfigureAwait(false);

                if (!symbols.IsDefault)
                    callback((relatedDocument.Id, syntaxContext, symbols));
            },
            args: (@this: this, solution, completionContext, options),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// If current context is in active region, returns available symbols. Otherwise, returns null.
    /// </summary>
    protected async Task<ImmutableArray<SymbolAndSelectionInfo>> TryGetSymbolsForContextAsync(
        CompletionContext? completionContext, TSyntaxContext syntaxContext, CompletionOptions options, CancellationToken cancellationToken)
    {
        var syntaxFacts = syntaxContext.GetRequiredLanguageService<ISyntaxFactsService>();
        return syntaxFacts.IsInInactiveRegion(syntaxContext.SyntaxTree, syntaxContext.Position, cancellationToken)
            ? default
            : await GetSymbolsAsync(completionContext, syntaxContext, syntaxContext.Position, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Given a list of symbols, determine which are not recommended at the same position in linked documents.
    /// </summary>
    /// <param name="symbolToContext">The symbols recommended in the active context.</param>
    /// <param name="linkedContextSymbolLists">The symbols recommended in linked documents</param>
    /// <returns>The list of projects each recommended symbol did NOT appear in.</returns>
    private static Dictionary<ISymbol, List<ProjectId>> FindSymbolsMissingInLinkedContexts(
        Dictionary<SymbolAndSelectionInfo, TSyntaxContext> symbolToContext,
        ImmutableArray<(DocumentId documentId, TSyntaxContext syntaxContext, ImmutableArray<SymbolAndSelectionInfo> symbols)> linkedContextSymbolLists)
    {
        var missingSymbols = new Dictionary<ISymbol, List<ProjectId>>(LinkedFilesSymbolEquivalenceComparer.Instance);

        foreach (var (documentId, syntaxContext, symbols) in linkedContextSymbolLists)
        {
            var symbolsMissingInLinkedContext = symbolToContext.Keys.Except(symbols);
            foreach (var (symbol, _) in symbolsMissingInLinkedContext)
                missingSymbols.GetOrAdd(symbol, m => []).Add(documentId.ProjectId);
        }

        return missingSymbols;
    }

    public sealed override Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        => Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, GetInsertionText(selectedItem, ch)));

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
        => SymbolCompletionItem.GetInsertionText(item);
}
