// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
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
        private readonly ImmutableHashSet<char> _csharpTriggerCharacters;
        private readonly ImmutableHashSet<char> _vbTriggerCharacters;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler(
            IGlobalOptionService globalOptions,
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
        {
            _globalOptions = globalOptions;

            _csharpTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.CSharp).SelectMany(
                lz => CommonCompletionUtilities.GetTriggerCharacters(lz.Value)).ToImmutableHashSet();
            _vbTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.VisualBasic).SelectMany(
                lz => CommonCompletionUtilities.GetTriggerCharacters(lz.Value)).ToImmutableHashSet();
        }

        public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;

        public async Task<LSP.CompletionList?> HandleRequestAsync(
            LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);
            Contract.ThrowIfNull(context.Solution);
            var clientCapabilities = context.GetRequiredClientCapabilities();

            // C# and VB share the same LSP language server, and thus share the same default trigger characters.
            // We need to ensure the trigger character is valid in the document's language. For example, the '{'
            // character, while a trigger character in VB, is not a trigger character in C#.
            if (request.Context != null &&
                request.Context.TriggerKind == LSP.CompletionTriggerKind.TriggerCharacter &&
                !char.TryParse(request.Context.TriggerCharacter, out var triggerCharacter) &&
                !char.IsLetterOrDigit(triggerCharacter) &&
                !IsValidTriggerCharacterForDocument(document, triggerCharacter))
            {
                return null;
            }

            var completionOptions = GetCompletionOptions(document);
            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var completionListResult = await GetFilteredCompletionListAsync(request, context, documentText, document, completionOptions, completionService, cancellationToken).ConfigureAwait(false);
            if (completionListResult == null)
                return null;

            var (list, isIncomplete, resultId) = completionListResult.Value;
            return await ConvertToLspCompletionListAsync(document, clientCapabilities, list, isIncomplete, resultId, cancellationToken)
                .ConfigureAwait(false);

            // Local function
            bool IsValidTriggerCharacterForDocument(Document document, char triggerCharacter)
            {
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    return _csharpTriggerCharacters.Contains(triggerCharacter);
                }
                else if (document.Project.Language == LanguageNames.VisualBasic)
                {
                    return _vbTriggerCharacters.Contains(triggerCharacter);
                }

                // Typescript still calls into this for completion.
                // Since we don't know what their trigger characters are, just return true.
                return true;
            }
        }

        private async Task<(CompletionList CompletionList, bool IsIncomplete, long ResultId)?> GetFilteredCompletionListAsync(
            LSP.CompletionParams request,
            RequestContext context,
            SourceText sourceText,
            Document document,
            CompletionOptions completionOptions,
            CompletionService completionService,
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
                var originalTrigger = new CompletionTrigger(CompletionTriggerKind.Invoke);
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

            var resultId = result.Value.ResultId;

            var completionListMaxSize = _globalOptions.GetOption(LspOptionsStorage.MaxCompletionListSize);
            var (completionList, isIncomplete) = FilterCompletionList(result.Value.List, completionListMaxSize, completionTrigger, sourceText);

            return (completionList, isIncomplete, resultId);
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
            var filterText = sourceText.GetSubText(completionList.Span).ToString();

            // Use pattern matching to determine which items are most relevant out of the calculated items.
            using var _ = ArrayBuilder<MatchResult>.GetInstance(out var matchResultsBuilder);
            var index = 0;
            using var helper = new PatternMatchHelper(filterText);
            foreach (var item in completionList.ItemsList)
            {
                if (helper.TryCreateMatchResult(
                    item,
                    completionTrigger.Kind,
                    GetFilterReason(completionTrigger),
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
                .Concat(matchResultsBuilder.Skip(completionListMaxSize).Where(match => ShouldItemBePreselected(match.CompletionItem)))
                .Select(matchResult => matchResult.CompletionItem)
                .ToImmutableArray();
            var newCompletionList = completionList.WithItems(filteredList);

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

        public static bool ShouldItemBePreselected(CompletionItem completionItem)
        {
            // An item should be preselected for LSP when the match priority is preselect and the item is hard selected.
            // LSP does not support soft preselection, so we do not preselect in that scenario to avoid interfering with
            // typing.
            return completionItem.Rules.MatchPriority == MatchPriority.Preselect && completionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection;
        }

        internal CompletionOptions GetCompletionOptions(Document document)
        {
            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            //
            // Also don't trigger completion in argument list automatically, since LSP currently has no concept of soft selection.
            // We want to avoid committing selected item with commit chars like `"` and `)`.
            return _globalOptions.GetCompletionOptions(document.Project.Language) with
            {
                ShowItemsFromUnimportedNamespaces = false,
                ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly,
                UpdateImportCompletionCacheInBackground = false,
                TriggerInArgumentLists = false
            };
        }
    }
}
