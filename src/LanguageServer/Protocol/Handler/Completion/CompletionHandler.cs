// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Information needed to adjust completion item positions from a spliced document back to the original.
/// </summary>
internal readonly struct SpliceAdjustment
{
    /// <param name="spliceStart">The position in the original document where the splice was inserted.</param>
    /// <param name="insertedLength">The length of text that was inserted during splicing.</param>
    public SpliceAdjustment(int spliceStart, int insertedLength)
    {
        Debug.Assert(spliceStart >= 0);
        Debug.Assert(insertedLength >= 0);
        SpliceStart = spliceStart;
        InsertedLength = insertedLength;
    }

    public int SpliceStart { get; }
    public int InsertedLength { get; }
}

/// <summary>
/// Handle a completion request.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(CompletionHandler)), Shared]
[Method(LSP.Methods.TextDocumentCompletionName)]
internal sealed partial class CompletionHandler : ILspServiceDocumentRequestHandler<LSP.CompletionParams, LSP.CompletionList?>
{
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CompletionHandler(
        IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;

    public async Task<LSP.CompletionList?> HandleRequestAsync(
        LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Document);
        Contract.ThrowIfNull(context.Solution);

        var document = context.Document;
        var position = await document
            .GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken)
            .ConfigureAwait(false);

        var capabilityHelper = new CompletionCapabilityHelper(context.GetRequiredClientCapabilities());
        var completionListCache = context.GetRequiredLspService<CompletionListCache>();

        return await GetCompletionListAsync(
            document,
            position,
            request.Context,
            _globalOptions,
            capabilityHelper,
            completionListCache,
            cancellationToken).ConfigureAwait(false);
    }

    public static Task<LSP.VSInternalCompletionList?> GetCompletionListAsync(
        Document document,
        int position,
        LSP.CompletionContext? completionContext,
        IGlobalOptionService globalOptions,
        CompletionCapabilityHelper capabilityHelper,
        CompletionListCache completionListCache,
        CancellationToken cancellationToken)
    {
        return GetCompletionListAsync(
            document,
            position,
            completionContext,
            globalOptions,
            capabilityHelper,
            completionListCache,
            spliceAdjustment: null,
            completionOptionsOverride: null,
            cancellationToken);
    }

    /// <summary>
    /// Gets a completion list for the specified position.
    /// </summary>
    /// <param name="spliceAdjustment">
    /// Optional splice adjustment for debugger completion. When specified, cached completion items
    /// will have their positions shifted back by the inserted length to reference the original
    /// (unspliced) document. This is used by debugger completion where completion is computed
    /// against a spliced document, but resolve runs against the original document.
    /// </param>
    /// <param name="completionOptionsOverride">
    /// Optional completion options override. When provided, these options are used instead of
    /// computing options from <paramref name="globalOptions"/>. This is used by debugger completion
    /// to set options like <see cref="CompletionOptions.FilterOutOfScopeLocals"/> to false.
    /// </param>
    public static async Task<LSP.VSInternalCompletionList?> GetCompletionListAsync(
        Document document,
        int position,
        LSP.CompletionContext? completionContext,
        IGlobalOptionService globalOptions,
        CompletionCapabilityHelper capabilityHelper,
        CompletionListCache completionListCache,
        SpliceAdjustment? spliceAdjustment,
        CompletionOptions? completionOptionsOverride,
        CancellationToken cancellationToken)
    {
        var completionOptions = completionOptionsOverride ?? globalOptions.GetCompletionOptionsForLsp(document.Project.Language, capabilityHelper);
        var completionListMaxSize = globalOptions.GetOption(LspOptionsStorage.MaxCompletionListSize);

        var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var completionTrigger = await ProtocolConversions
            .LSPToRoslynCompletionTriggerAsync(completionContext, document, position, cancellationToken)
            .ConfigureAwait(false);
        var completionService = document.GetRequiredLanguageService<CompletionService>();

        var project = document.Project;

        // Let CompletionService decide if we should trigger completion, unless the request is for incomplete results, in which case we always trigger. 
        if (completionContext?.TriggerKind is not LSP.CompletionTriggerKind.TriggerForIncompleteCompletions
            && !completionService.ShouldTriggerCompletion(project, project.Services, documentText, position, completionTrigger, completionOptions, project.Solution.Options, roles: null))
        {
            return null;
        }

        var completionListResult = await GetFilteredCompletionListAsync(
            completionContext, document, documentText, position, completionOptions, capabilityHelper, completionService, completionListCache, completionListMaxSize, spliceAdjustment, cancellationToken).ConfigureAwait(false);

        if (completionListResult == null)
            return null;

        var (list, isIncomplete, isHardSelection, resultId) = completionListResult.Value;

        var result = await CompletionResultFactory
            .ConvertToLspCompletionListAsync(document, capabilityHelper, list, isIncomplete, isHardSelection, resultId, cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    private static async Task<(CompletionList CompletionList, bool IsIncomplete, bool isHardSelection, long ResultId)?> GetFilteredCompletionListAsync(
        LSP.CompletionContext? context,
        Document document,
        SourceText sourceText,
        int position,
        CompletionOptions completionOptions,
        CompletionCapabilityHelper capabilityHelper,
        CompletionService completionService,
        CompletionListCache completionListCache,
        int completionListMaxSize,
        SpliceAdjustment? spliceAdjustment,
        CancellationToken cancellationToken)
    {
        var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(context, document, position, cancellationToken).ConfigureAwait(false);
        var isTriggerForIncompleteCompletions = context?.TriggerKind == LSP.CompletionTriggerKind.TriggerForIncompleteCompletions;

        (CompletionList List, long ResultId)? result;
        if (isTriggerForIncompleteCompletions)
        {
            // We don't have access to the original trigger, but we know the completion list is already present.
            // It is safe to recompute with the invoked trigger as we will get all the items and filter down based on the current trigger.
            var originalTrigger = CompletionTrigger.Invoke;
            result = await CalculateListAsync(document, position, originalTrigger, completionOptions, completionService, completionListCache, spliceAdjustment, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // This is a new completion request, clear out the last result Id for incomplete results.
            result = await CalculateListAsync(document, position, completionTrigger, completionOptions, completionService, completionListCache, spliceAdjustment, cancellationToken).ConfigureAwait(false);
        }

        if (result == null)
        {
            return null;
        }

        var (completionList, resultId) = result.Value;

        // By default, Roslyn would treat continuous alphabetical text as a single word for completion purpose.
        // e.g. when user triggers completion at the location of {$} in "pub{$}class", the span would cover "pubclass",
        // which is used for subsequent matching and commit.
        // This works fine for VS async-completion, where we have full control of entire completion process.
        // However, the insert mode in VSCode (i.e. the mode our LSP server supports) expects us to return TextEdit that only
        // covers the span ends at the cursor location, e.g. "pub" in the example above. Here we detect when that occurs and
        // adjust the span accordingly.
        if (!capabilityHelper.SupportVSInternalClientCapabilities && position < completionList.Span.End)
        {
            var defaultSpan = new TextSpan(completionList.Span.Start, length: position - completionList.Span.Start);
            completionList = completionList.WithSpan(defaultSpan);
        }

        var (filteredCompletionList, isIncomplete, isHardSelection) = FilterCompletionList(completionList, completionListMaxSize, completionTrigger, sourceText, capabilityHelper);

        return (filteredCompletionList, isIncomplete, isHardSelection, resultId);
    }

    private static async Task<(CompletionList CompletionList, long ResultId)?> CalculateListAsync(
        Document document,
        int position,
        CompletionTrigger completionTrigger,
        CompletionOptions completionOptions,
        CompletionService completionService,
        CompletionListCache completionListCache,
        SpliceAdjustment? spliceAdjustment,
        CancellationToken cancellationToken)
    {
        var completionList = await completionService.GetCompletionsAsync(document, position, completionOptions, document.Project.Solution.Options, completionTrigger, cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (completionList.ItemsList.IsEmpty())
        {
            return null;
        }

        // If a splice adjustment is specified (for debug completions), adjust item positions
        // so that resolve operations work correctly with the original (unspliced) document.
        var listToCache = spliceAdjustment.HasValue
            ? AdjustCompletionListForOriginalDocument(completionList, spliceAdjustment.Value)
            : completionList;

        // Cache the completion list so we can avoid recomputation in the resolve handler
        var resultId = completionListCache.UpdateCache(new CompletionListCache.CacheEntry(listToCache));

        return (completionList, resultId);
    }

    /// <summary>
    /// Creates a copy of the completion list with all item positions shifted back to reference
    /// the original (unspliced) document. This is the inverse operation of splicing.
    /// </summary>
    /// <remarks>
    /// When splicing, we insert text at <paramref name="adjustment"/>.SpliceStart with length
    /// <paramref name="adjustment"/>.InsertedLength. To reverse this for resolve:
    /// <list type="bullet">
    /// <item>Positions before SpliceStart are unchanged</item>
    /// <item>Positions within the inserted text map to SpliceStart</item>
    /// <item>Positions after the inserted text shift back by InsertedLength</item>
    /// </list>
    /// IMPORTANT: If a new position-carrying property is added to <see cref="CompletionItem"/>,
    /// it must also be adjusted here for debugger completion to resolve correctly.
    /// </remarks>
    private static CompletionList AdjustCompletionListForOriginalDocument(
        CompletionList completionList,
        SpliceAdjustment adjustment)
    {
        var adjustedItems = new FixedSizeArrayBuilder<CompletionItem>(completionList.ItemsList.Count);
        var spliceStart = adjustment.SpliceStart;
        var insertedLength = adjustment.InsertedLength;
        var spliceEnd = spliceStart + insertedLength;

        foreach (var item in completionList.ItemsList)
        {
            var adjustedPosition = AdjustPosition(item.Span.Start, spliceStart, spliceEnd, insertedLength);
            var adjustedSpan = new TextSpan(adjustedPosition, 0);

            item.Span = adjustedSpan;

            var properties = item.GetProperties();
            var adjustedItem = ReplaceContextPosition(properties, adjustedPosition, out var updatedProperties)
                ? item.WithProperties(updatedProperties)
                : item;

            adjustedItems.Add(adjustedItem);
        }

        // Also adjust the list's overall span
        var adjustedListPosition = AdjustPosition(completionList.Span.Start, spliceStart, spliceEnd, insertedLength);
        var adjustedListSpan = new TextSpan(adjustedListPosition, 0);

        return completionList.With(
            span: adjustedListSpan,
            itemsList: new(adjustedItems.MoveToImmutable()));

        // Adjusts a position from the spliced document back to the original document
        static int AdjustPosition(int position, int spliceStart, int spliceEnd, int insertedLength)
        {
            if (position < spliceStart)
            {
                // Position is before the splice - unchanged
                return position;
            }
            else if (position < spliceEnd)
            {
                // Position is within the inserted text - map to splice point
                return spliceStart;
            }

            // Position is after the inserted text - shift back
            return position - insertedLength;
        }

        // Replaces the "ContextPosition" entry (set by SymbolCompletionItem) with the adjusted value
        // Returns true if a replacement was made
        static bool ReplaceContextPosition(
            ImmutableArray<KeyValuePair<string, string>> properties, int adjustedPosition, out ImmutableArray<KeyValuePair<string, string>> updatedProperties)
        {
            if (!properties.Any(p => p.Key == CompletionItem.ContextPositionKey))
            {
                updatedProperties = default;
                return false;
            }

            var builder = new FixedSizeArrayBuilder<KeyValuePair<string, string>>(properties.Length);
            for (var i = 0; i < properties.Length; i++)
            {
                var kvp = properties[i].Key == CompletionItem.ContextPositionKey
                    ? CompletionItem.CreateContextPositionKvp(adjustedPosition)
                    : properties[i];

                builder.Add(kvp);
            }

            updatedProperties = builder.MoveToImmutable();
            return true;
        }
    }
    private static (CompletionList CompletionList, bool IsIncomplete, bool isHardSelection) FilterCompletionList(
        CompletionList completionList,
        int completionListMaxSize,
        CompletionTrigger completionTrigger,
        SourceText sourceText,
        CompletionCapabilityHelper completionCapabilityHelper)
    {
        var filterText = sourceText.GetSubText(completionList.Span).ToString();
        var filterReason = GetFilterReason(completionTrigger);

        // Determine if the list should be hard selected or soft selected.
        var isFilterTextAllPunctuation = CompletionService.IsAllPunctuation(filterText);

        // If we only had punctuation - we set soft selection and the list to be incomplete so we get called back when the user continues typing.
        // If they type something that is not punctuation, we may need to update the hard vs soft selection.
        // For example, typing '_' should initially be soft selection, but if the user types 'o' we should hard select '_otherVar' (if it exists).
        // This isn't perfect - ideally we would make this determination every time a filter character is typed, but we do not get called back
        // for typing filter characters in LSP (unless we always set isIncomplete, which is expensive).
        var isHardSelection = completionList.SuggestionModeItem is null && !isFilterTextAllPunctuation;
        var isIncomplete = isFilterTextAllPunctuation;

        // If our completion list hasn't hit the max size, we don't need to do anything filtering
        if (completionListMaxSize < 0 || completionListMaxSize >= completionList.ItemsList.Count)
            return (completionList, isIncomplete, isHardSelection);

        // Use pattern matching to determine which items are most relevant out of the calculated items.
        using var _ = ArrayBuilder<MatchResult>.GetInstance(discardLargeInstances: false, out var matchResultsBuilder);
        var index = 0;
        using var helper = new PatternMatchHelper(filterText);
        foreach (var item in completionList.ItemsList)
        {
            if (helper.TryCreateMatchResult(
                item,
                completionTrigger.Kind,
                filterReason,
                recentItemIndex: -1,
                includeMatchSpans: false,
                index,
                out var matchResult))
            {
                matchResultsBuilder.Add(matchResult);
                index++;
            }
        }

        // Next, we sort the list based on the pattern matching result.
        matchResultsBuilder.Sort(MatchResult.SortingComparer);

        // Finally, truncate the list to 1000 items plus any preselected items that occur after the first 1000.
        var filteredArray = GetFilteredArray(completionListMaxSize, matchResultsBuilder);
        var newCompletionList = completionList.WithItemsList(filteredArray);

        // Per the LSP spec, the completion list should be marked with isIncomplete = false when further insertions will
        // not generate any more completion items.  This means that we should be checking if the matchedResults is larger
        // than the filteredList.  However, the VS client has a bug where they do not properly re-trigger completion
        // when a character is deleted to go from a complete list back to an incomplete list.
        // For example, the following scenario.
        // User types "So" -> server gives subset of items for "So" with isIncomplete = true
        // User types "m" -> server gives entire set of items for "Som" with isIncomplete = false
        // User deletes "m" -> client has to remember that "So" results were incomplete and re-request if the user types something else, like "n"
        //
        // Currently the VS client does not remember to re-request, so the completion list only ever shows items from "Som"
        // so we always set the isIncomplete flag to true when the original list size (computed when no filter text was typed) is too large.
        // VS bug here - https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1335142
        isIncomplete |= completionCapabilityHelper.SupportVSInternalClientCapabilities
            ? completionList.ItemsList.Count > newCompletionList.ItemsList.Count
            : matchResultsBuilder.Count > filteredArray.Length;

        return (newCompletionList, isIncomplete, isHardSelection);

        static CompletionFilterReason GetFilterReason(CompletionTrigger trigger)
        {
            return trigger.Kind switch
            {
                CompletionTriggerKind.Insertion => CompletionFilterReason.Insertion,
                CompletionTriggerKind.Deletion => CompletionFilterReason.Deletion,
                _ => CompletionFilterReason.Other,
            };
        }

        static ImmutableArray<CompletionItem> GetFilteredArray(int completionListMaxSize, ArrayBuilder<MatchResult> matchResultsBuilder)
        {
            var takeCount = Math.Min(completionListMaxSize, matchResultsBuilder.Count);
            var preselectCount = 0;

            // Count how many preselected items are beyond takeCount
            for (var i = takeCount; i < matchResultsBuilder.Count; i++)
            {
                if (matchResultsBuilder[i].CompletionItem.Rules.MatchPriority == MatchPriority.Preselect)
                {
                    preselectCount++;
                }
            }

            // Allocate the final array to hold the filtered items
            FixedSizeArrayBuilder<CompletionItem> filteredBuilder = new(takeCount + preselectCount);

            // Add the first takeCount items
            for (var i = 0; i < takeCount; i++)
            {
                filteredBuilder.Add(matchResultsBuilder[i].CompletionItem);
            }

            if (preselectCount > 0)
            {
                // Find the preselected items beyond takeCount and add them
                for (var i = takeCount; i < matchResultsBuilder.Count; i++)
                {
                    var item = matchResultsBuilder[i].CompletionItem;
                    if (item.Rules.MatchPriority == MatchPriority.Preselect)
                    {
                        filteredBuilder.Add(item);
                    }
                }
            }

            return filteredBuilder.MoveToImmutable();
        }
    }
}
