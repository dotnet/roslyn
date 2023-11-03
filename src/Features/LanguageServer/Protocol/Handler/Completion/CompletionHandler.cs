﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
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
            var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var capabilityHelper = new CompletionCapabilityHelper(context.GetRequiredClientCapabilities());

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(request.Context, document, position, cancellationToken).ConfigureAwait(false);
            var completionOptions = GetCompletionOptions(document, capabilityHelper);
            var completionService = document.GetRequiredLanguageService<CompletionService>();

            // Let CompletionService decide if we should trigger completion, unless the request is for incomplete results, in which case we always trigger. 
            if (request.Context?.TriggerKind is not LSP.CompletionTriggerKind.TriggerForIncompleteCompletions
                && !completionService.ShouldTriggerCompletion(document.Project, document.Project.Services, documentText, position, completionTrigger, completionOptions, document.Project.Solution.Options, roles: null))
            {
                return null;
            }

            var completionListResult = await GetFilteredCompletionListAsync(
                request, context, documentText, document, completionOptions, completionService, capabilityHelper, cancellationToken).ConfigureAwait(false);

            if (completionListResult == null)
                return null;

            var (list, isIncomplete, resultId) = completionListResult.Value;

            var creationService = document.Project.Solution.Services.GetRequiredService<ILspCompletionResultCreationService>();
            return await creationService.ConvertToLspCompletionListAsync(document, position, capabilityHelper, list, isIncomplete, resultId, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<(CompletionList CompletionList, bool IsIncomplete, long ResultId)?> GetFilteredCompletionListAsync(
            LSP.CompletionParams request,
            RequestContext context,
            SourceText sourceText,
            Document document,
            CompletionOptions completionOptions,
            CompletionService completionService,
            CompletionCapabilityHelper capabilityHelper,
            CancellationToken cancellationToken)
        {
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(request.Context, document, position, cancellationToken).ConfigureAwait(false);
            var isTriggerForIncompleteCompletions = request.Context?.TriggerKind == LSP.CompletionTriggerKind.TriggerForIncompleteCompletions;
            var completionListCache = context.GetRequiredLspService<CompletionListCache>();

            (CompletionList List, long ResultId)? result;
            if (isTriggerForIncompleteCompletions)
            {
                // We don't have access to the original trigger, but we know the completion list is already present.
                // It is safe to recompute with the invoked trigger as we will get all the items and filter down based on the current trigger.
                var originalTrigger = CompletionTrigger.Invoke;
                result = await CalculateListAsync(request, document, position, originalTrigger, completionOptions, completionService, completionListCache, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // This is a new completion request, clear out the last result Id for incomplete results.
                result = await CalculateListAsync(request, document, position, completionTrigger, completionOptions, completionService, completionListCache, cancellationToken).ConfigureAwait(false);
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

            var completionListMaxSize = _globalOptions.GetOption(LspOptionsStorage.MaxCompletionListSize);
            var (filteredCompletionList, isIncomplete) = FilterCompletionList(completionList, completionListMaxSize, completionTrigger, sourceText);

            return (filteredCompletionList, isIncomplete, resultId);
        }

        private static async Task<(CompletionList CompletionList, long ResultId)?> CalculateListAsync(
            LSP.CompletionParams request,
            Document document,
            int position,
            CompletionTrigger completionTrigger,
            CompletionOptions completionOptions,
            CompletionService completionService,
            CompletionListCache completionListCache,
            CancellationToken cancellationToken)
        {
            var completionList = await completionService.GetCompletionsAsync(document, position, completionOptions, document.Project.Solution.Options, completionTrigger, cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (completionList.ItemsList.IsEmpty())
            {
                return null;
            }

            // Cache the completion list so we can avoid recomputation in the resolve handler
            var resultId = completionListCache.UpdateCache(new CompletionListCache.CacheEntry(request.TextDocument, completionList));

            return (completionList, resultId);
        }

        private static (CompletionList CompletionList, bool IsIncomplete) FilterCompletionList(
            CompletionList completionList,
            int completionListMaxSize,
            CompletionTrigger completionTrigger,
            SourceText sourceText)
        {
            if (completionListMaxSize < 0 || completionListMaxSize >= completionList.ItemsList.Count)
                return (completionList, false);

            var filterText = sourceText.GetSubText(completionList.Span).ToString();
            var filterReason = GetFilterReason(completionTrigger);

            // Use pattern matching to determine which items are most relevant out of the calculated items.
            using var _ = ArrayBuilder<MatchResult>.GetInstance(out var matchResultsBuilder);
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
            var filteredList = matchResultsBuilder
                .Take(completionListMaxSize)
                .Concat(matchResultsBuilder.Skip(completionListMaxSize).Where(match => match.CompletionItem.Rules.MatchPriority == MatchPriority.Preselect))
                .Select(matchResult => matchResult.CompletionItem)
                .ToImmutableArray();
            var newCompletionList = completionList.WithItemsList(filteredList);

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
            var isIncomplete = completionList.ItemsList.Count > newCompletionList.ItemsList.Count;

            return (newCompletionList, isIncomplete);

            static CompletionFilterReason GetFilterReason(CompletionTrigger trigger)
            {
                return trigger.Kind switch
                {
                    CompletionTriggerKind.Insertion => CompletionFilterReason.Insertion,
                    CompletionTriggerKind.Deletion => CompletionFilterReason.Deletion,
                    _ => CompletionFilterReason.Other,
                };
            }
        }

        private CompletionOptions GetCompletionOptions(Document document, CompletionCapabilityHelper capabilityHelper)
        {
            var options = _globalOptions.GetCompletionOptions(document.Project.Language);

            if (capabilityHelper.SupportVSInternalClientCapabilities)
            {
                // Filter out unimported types for now as there are two issues with providing them:
                // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
                //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
                // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
                //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
                // 3.  LSP client should support completion filters / expanders
                options = options with
                {
                    ShowItemsFromUnimportedNamespaces = false,
                    ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly,
                    UpdateImportCompletionCacheInBackground = false,
                };
            }
            else
            {
                var updateImportCompletionCacheInBackground = options.ShowItemsFromUnimportedNamespaces is true;
                options = options with
                {
                    ShowNewSnippetExperienceUserOption = false,
                    UpdateImportCompletionCacheInBackground = updateImportCompletionCacheInBackground
                };
            }

            return options;
        }
    }
}
