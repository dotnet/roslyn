// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    internal class CompletionHandler : IRequestHandler<LSP.CompletionParams, LSP.CompletionList?>
    {
        /// <summary>
        /// This sets the max list size we will return in response to a completion request.
        /// If there are more than this many items, we will set the isIncomplete flag on the returned completion list.
        /// </summary>
        private const int MaxCompletionListSize = 1000;

        /// <summary>
        /// Semaphore to guard access to the <see cref="_lastIncompleteResultId"/>
        /// While unlikely, since completion is a non-mutating request there is a possibility of concurrent requests from the client.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// This is a pair of the source text with the completionListSpan removed to the resultId for an incomplete response.
        /// When the client asks us for more items using <see cref="LSP.CompletionTriggerKind.TriggerForIncompleteCompletions"/>
        /// we calculate the source text with the completionListSpan removed (no filter text).  If the source text
        /// matches the value here, we can re-use the associated resultId to lookup the cached completion list.
        /// </summary>
        private (SourceText, long)? _lastIncompleteResultId = null;

        private readonly ImmutableHashSet<char> _csharpTriggerCharacters;
        private readonly ImmutableHashSet<char> _vbTriggerCharacters;

        private readonly CompletionListCache _completionListCache;

        public string Method => LSP.Methods.TextDocumentCompletionName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CompletionHandler(
            IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
            CompletionListCache completionListCache)
        {
            _csharpTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.CSharp).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();
            _vbTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.VisualBasic).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();

            _completionListCache = completionListCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;

        public async Task<LSP.CompletionList?> HandleRequestAsync(LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

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

            var completionOptions = await GetCompletionOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var completionListResult = await GetFilteredCompletionListAsync(request, documentText, document, completionOptions, completionService, cancellationToken).ConfigureAwait(false);
            if (completionListResult == null)
            {
                return null;
            }

            var (list, isIncomplete, resultId) = completionListResult.Value;

            var lspVSClientCapability = context.ClientCapabilities.HasVisualStudioLspCapability() == true;
            var snippetsSupported = context.ClientCapabilities.TextDocument?.Completion?.CompletionItem?.SnippetSupport ?? false;
            var commitCharactersRuleCache = new Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]>(CommitCharacterArrayComparer.Instance);

            // Feature flag to enable the return of TextEdits instead of InsertTexts (will increase payload size).
            // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
            // We also check against the CompletionOption for test purposes only.
            Contract.ThrowIfNull(context.Solution);
            var featureFlagService = context.Solution.Workspace.Services.GetRequiredService<IExperimentationService>();
            var returnTextEdits = featureFlagService.IsExperimentEnabled(WellKnownExperimentNames.LSPCompletion) ||
                completionOptions.GetOption(CompletionOptions.ForceRoslynLSPCompletionExperiment, document.Project.Language);

            TextSpan? defaultSpan = null;
            LSP.Range? defaultRange = null;
            if (returnTextEdits)
            {
                // We want to compute the document's text just once.
                documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                // We use the first item in the completion list as our comparison point for span
                // and range for optimization when generating the TextEdits later on.
                var completionChange = await completionService.GetChangeAsync(
                    document, list.Items.First(), cancellationToken: cancellationToken).ConfigureAwait(false);

                // If possible, we want to compute the item's span and range just once.
                // Individual items can override this range later.
                defaultSpan = completionChange.TextChange.Span;
                defaultRange = ProtocolConversions.TextSpanToRange(defaultSpan.Value, documentText);
            }

            var supportsCompletionListData = context.ClientCapabilities.HasCompletionListDataCapability();
            var completionResolveData = new CompletionResolveData()
            {
                ResultId = resultId,
            };
            var stringBuilder = new StringBuilder();
            using var _ = ArrayBuilder<LSP.CompletionItem>.GetInstance(out var lspCompletionItems);
            foreach (var item in list.Items)
            {
                var completionItemResolveData = supportsCompletionListData ? null : completionResolveData;
                var lspCompletionItem = await CreateLSPCompletionItemAsync(
                    request, document, item, completionItemResolveData, lspVSClientCapability, commitCharactersRuleCache,
                    completionService, context.ClientName, returnTextEdits, snippetsSupported, stringBuilder, documentText,
                    defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                lspCompletionItems.Add(lspCompletionItem);
            }
            var completionList = new LSP.VSCompletionList
            {
                Items = lspCompletionItems.ToArray(),
                SuggestionMode = list.SuggestionModeItem != null,
                IsIncomplete = isIncomplete,
            };

            if (supportsCompletionListData)
            {
                completionList.Data = completionResolveData;
            }

            if (context.ClientCapabilities.HasCompletionListCommitCharactersCapability())
            {
                PromoteCommonCommitCharactersOntoList(completionList);
            }

            var optimizedCompletionList = new LSP.OptimizedVSCompletionList(completionList);
            return optimizedCompletionList;

            // Local functions
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

            static async Task<LSP.CompletionItem> CreateLSPCompletionItemAsync(
                LSP.CompletionParams request,
                Document document,
                CompletionItem item,
                CompletionResolveData? completionResolveData,
                bool supportsVSExtensions,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]> commitCharacterRulesCache,
                CompletionService completionService,
                string? clientName,
                bool returnTextEdits,
                bool snippetsSupported,
                StringBuilder stringBuilder,
                SourceText? documentText,
                TextSpan? defaultSpan,
                LSP.Range? defaultRange,
                CancellationToken cancellationToken)
            {
                if (supportsVSExtensions)
                {
                    var vsCompletionItem = await CreateCompletionItemAsync<LSP.VSCompletionItem>(
                        request, document, item, completionResolveData, supportsVSExtensions, commitCharacterRulesCache,
                        completionService, clientName, returnTextEdits, snippetsSupported, stringBuilder,
                        documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
                    return vsCompletionItem;
                }
                else
                {
                    var roslynCompletionItem = await CreateCompletionItemAsync<LSP.CompletionItem>(
                        request, document, item, completionResolveData, supportsVSExtensions, commitCharacterRulesCache,
                        completionService, clientName, returnTextEdits, snippetsSupported, stringBuilder,
                        documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    return roslynCompletionItem;
                }
            }

            static async Task<TCompletionItem> CreateCompletionItemAsync<TCompletionItem>(
                LSP.CompletionParams request,
                Document document,
                CompletionItem item,
                CompletionResolveData? completionResolveData,
                bool supportsVSExtensions,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]> commitCharacterRulesCache,
                CompletionService completionService,
                string? clientName,
                bool returnTextEdits,
                bool snippetsSupported,
                StringBuilder stringBuilder,
                SourceText? documentText,
                TextSpan? defaultSpan,
                LSP.Range? defaultRange,
                CancellationToken cancellationToken) where TCompletionItem : LSP.CompletionItem, new()
            {
                // Generate display text
                stringBuilder.Append(item.DisplayTextPrefix);
                stringBuilder.Append(item.DisplayText);
                stringBuilder.Append(item.DisplayTextSuffix);
                var completeDisplayText = stringBuilder.ToString();
                stringBuilder.Clear();

                var completionItem = new TCompletionItem
                {
                    Label = completeDisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = completionResolveData,
                    Preselect = ShouldItemBePreselected(item),
                };

                // Complex text edits (e.g. override and partial method completions) are always populated in the
                // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
                if (item.IsComplexTextEdit)
                {
                    // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                    // We can enable it for regular C# once LSP is used for local completion.
                    if (snippetsSupported)
                    {
                        completionItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
                    }
                }
                // If the feature flag is on, always return a TextEdit.
                else if (returnTextEdits)
                {
                    var textEdit = await GenerateTextEdit(
                        document, item, completionService, documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    completionItem.TextEdit = textEdit;
                }
                // If the feature flag is off, return an InsertText.
                else
                {
                    completionItem.InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : completeDisplayText;
                }

                var commitCharacters = GetCommitCharacters(item, commitCharacterRulesCache, supportsVSExtensions);
                if (commitCharacters != null)
                {
                    completionItem.CommitCharacters = commitCharacters;
                }

                return completionItem;

                static async Task<LSP.TextEdit> GenerateTextEdit(
                    Document document,
                    CompletionItem item,
                    CompletionService completionService,
                    SourceText? documentText,
                    TextSpan? defaultSpan,
                    LSP.Range? defaultRange,
                    CancellationToken cancellationToken)
                {
                    Contract.ThrowIfNull(documentText);
                    Contract.ThrowIfNull(defaultSpan);
                    Contract.ThrowIfNull(defaultRange);

                    var completionChange = await completionService.GetChangeAsync(
                        document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var completionChangeSpan = completionChange.TextChange.Span;

                    var textEdit = new LSP.TextEdit()
                    {
                        NewText = completionChange.TextChange.NewText ?? "",
                        Range = completionChangeSpan == defaultSpan.Value
                            ? defaultRange
                            : ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                    };

                    return textEdit;
                }
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
                {
                    return Array.Empty<string>();
                }

                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // VS will use the default commit characters if no items are specified on the completion item.
                // However, other clients like VSCode do not support this behavior so we must specify
                // commit characters on every completion item - https://github.com/microsoft/vscode/issues/90987
                if (supportsVSExtensions && commitCharacterRules.IsEmpty)
                {
                    return null;
                }

                if (currentRuleCache.TryGetValue(commitCharacterRules, out var cachedCommitCharacters))
                {
                    return cachedCommitCharacters;
                }

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

                var lspCommitCharacters = commitCharacters.Select(c => c.ToString()).ToArray();
                currentRuleCache.Add(item.Rules.CommitCharacterRules, lspCommitCharacters);
                return lspCommitCharacters;
            }

            static void PromoteCommonCommitCharactersOntoList(LSP.VSCompletionList completionList)
            {
                var commitCharacterReferences = new Dictionary<object, int>();
                var mostUsedCount = 0;
                string[]? mostUsedCommitCharacters = null;
                for (var i = 0; i < completionList.Items.Length; i++)
                {
                    var completionItem = completionList.Items[i];
                    var commitCharacters = completionItem.CommitCharacters;
                    if (commitCharacters == null)
                    {
                        continue;
                    }

                    commitCharacterReferences.TryGetValue(commitCharacters, out var existingCount);
                    existingCount++;

                    if (existingCount > mostUsedCount)
                    {
                        // Capture the most used commit character counts so we don't need to re-iterate the array later
                        mostUsedCommitCharacters = commitCharacters;
                        mostUsedCount = existingCount;
                    }

                    commitCharacterReferences[commitCharacters] = existingCount;
                }

                Contract.ThrowIfNull(mostUsedCommitCharacters);

                // Promoted the most used commit characters onto the list and then remove these from child items.
                completionList.CommitCharacters = mostUsedCommitCharacters;
                for (var i = 0; i < completionList.Items.Length; i++)
                {
                    var completionItem = completionList.Items[i];
                    if (completionItem.CommitCharacters == mostUsedCommitCharacters)
                    {
                        completionItem.CommitCharacters = null;
                    }
                }
            }
        }

        private async Task<(CompletionList CompletionList, bool IsIncomplete, long ResultId)?> GetFilteredCompletionListAsync(
            LSP.CompletionParams request,
            SourceText sourceText,
            Document document,
            OptionSet completionOptions,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completionListSpan = completionService.GetDefaultCompletionListSpan(sourceText, position);
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(request.Context, document, position, cancellationToken).ConfigureAwait(false);
            var isTriggerForIncompleteCompletions = request.Context?.TriggerKind == LSP.CompletionTriggerKind.TriggerForIncompleteCompletions;

            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                (CompletionList List, long ResultId)? result;
                if (isTriggerForIncompleteCompletions)
                {
                    result = await GetCompletionListForIncompleteTriggerAsync(request, _lastIncompleteResultId, completionListSpan, document, sourceText, completionOptions, _completionListCache, completionService, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // This is a new completion request, clear out the last result Id for incomplete results.
                    _lastIncompleteResultId = null;
                    result = await CalculateListAsync(request, document, position, completionTrigger, completionOptions, completionService, _completionListCache, cancellationToken).ConfigureAwait(false);
                }

                if (result == null)
                {
                    return null;
                }

                var resultId = result.Value.ResultId;
                var (completionList, isIncomplete) = FilterCompletionList(result.Value.List, completionListSpan, completionTrigger, sourceText, document);

                // The list is incomplete, update our cached resultId so we can avoid recomputing the list for followup TriggerForIncompleteCompletions triggers.
                // Even if the current filtered list is complete, the user may delete some filter text causing the client to re-query us for a previous incomplete response.
                // So we avoid clearing the cached resultId until the client asks us for an entirely new completion list (handled above).
                if (isIncomplete)
                {
                    // Store the source text as what it would be without the current completion list span.  This allows us to easily
                    // ensure that the source texts match between an incomplete response and a new TriggerForIncompleteCompletion request.
                    _lastIncompleteResultId = (sourceText.WithChanges(new TextChange(completionListSpan, string.Empty)), resultId);
                }

                return (completionList, isIncomplete, resultId);
            }
        }

        private static async Task<(CompletionList CompletionList, long ResultId)?> GetCompletionListForIncompleteTriggerAsync(
            LSP.CompletionParams request,
            (SourceText, long)? lastResultId,
            TextSpan completionListSpan,
            Document document,
            SourceText sourceText,
            OptionSet completionOptions,
            CompletionListCache completionListCache,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            // Map back the current source text to what the source text would be without the current completion span.
            // We use this for two purposes:
            //   1.  Compare against the source text we stored with the incomplete resultId to ensure we're not using a stale cached list.
            //   2.  If we do not have a cached list (or it didn't match), we can re-compute the list.
            var textWithoutCompletionListSpan = sourceText.WithChanges(new TextChange(completionListSpan, string.Empty));

            var cachedList = GetCachedList(lastResultId, textWithoutCompletionListSpan, completionListCache);
            if (cachedList == null)
            {
                // We're missing the cached completion list for this incomplete request.
                // We can re-compute what the list would have been had the user just started typing the word.
                // We get this by creating a document that has the filter text removed.
                var originalDocument = document.WithText(sourceText.WithChanges(new TextChange(completionListSpan, string.Empty)));
                // We don't have access to the original trigger, but we know the completion list is already present.
                // It is safe to recompute with the invoked trigger as we will get all the items and filter down based on the current trigger.
                var originalTrigger = new CompletionTrigger(CompletionTriggerKind.Invoke);
                return await CalculateListAsync(request, originalDocument, completionListSpan.Start, originalTrigger, completionOptions, completionService, completionListCache, cancellationToken).ConfigureAwait(false);
            }

            return cachedList;

            static (CompletionList, long)? GetCachedList(
                (SourceText SourceText, long ResultId)? lastResultId,
                SourceText textWithoutCompletionListSpan,
                CompletionListCache completionListCache)
            {
                if (lastResultId.HasValue)
                {
                    if (textWithoutCompletionListSpan.ContentEquals(lastResultId.Value.SourceText))
                    {
                        var cachedList = completionListCache.GetCachedCompletionList(lastResultId.Value.ResultId);
                        if (cachedList != null)
                        {
                            return (cachedList.CompletionList, lastResultId.Value.ResultId);
                        }
                    }
                }

                return null;
            }
        }

        private static async Task<(CompletionList CompletionList, long ResultId)?> CalculateListAsync(
            LSP.CompletionParams request,
            Document document,
            int position,
            CompletionTrigger completionTrigger,
            OptionSet completionOptions,
            CompletionService completionService,
            CompletionListCache completionListCache,
            CancellationToken cancellationToken)
        {
            var completionList = await completionService.GetCompletionsAsync(document, position, completionTrigger, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (completionList == null || completionList.Items.IsEmpty)
            {
                return null;
            }

            // Cache the completion list so we can avoid recomputation in the resolve handler
            var resultId = completionListCache.UpdateCache(request.TextDocument, completionList);

            return (completionList, resultId);
        }

        private static (CompletionList CompletionList, bool IsIncomplete) FilterCompletionList(CompletionList completionList, TextSpan completionListSpan, CompletionTrigger completionTrigger, SourceText sourceText, Document document)
        {
            var filterText = sourceText.GetSubText(completionListSpan).ToString();

            // Use pattern matching to determine which items are most relevant out of the calculated items.
            using var _ = ArrayBuilder<MatchResult<CompletionItem?>>.GetInstance(out var matchResultsBuilder);
            var index = 0;
            var completionHelper = CompletionHelper.GetHelper(document);
            foreach (var item in completionList.Items)
            {
                if (CompletionHelper.TryCreateMatchResult<CompletionItem?>(
                    completionHelper,
                    item,
                    editorCompletionItem: null,
                    filterText,
                    completionTrigger.Kind,
                    GetFilterReason(completionTrigger),
                    ImmutableArray<string>.Empty,
                    includeMatchSpans: false,
                    index,
                    out var matchResult))
                {
                    matchResultsBuilder.Add(matchResult);
                    index++;
                }
            }

            // Next, we sort the list based on the pattern matching result.
            matchResultsBuilder.Sort(MatchResult<CompletionItem?>.SortingComparer);

            // Finally, truncate the list to 1000 items plus any preselected items that occur after the first 1000.
            var filteredList = matchResultsBuilder
                .Take(MaxCompletionListSize)
                .Concat(matchResultsBuilder.Skip(MaxCompletionListSize).Where(match => ShouldItemBePreselected(match.RoslynCompletionItem)))
                .Select(matchResult => matchResult.RoslynCompletionItem)
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

            // Currently the VS client does not remember to re-request, so the completion list only ever shows items from "Som"
            // so we always set the isIncomplete flag to true when the original list size (computed when no filter text was typed) is too large.
            // VS bug here - https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1335142
            var isIncomplete = completionList.Items.Length > newCompletionList.Items.Length;

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

        private static bool ShouldItemBePreselected(CompletionItem completionItem)
        {
            // An item should be preselcted for LSP when the match priority is preselect and the item is hard selected.
            // LSP does not support soft preselection, so we do not preselect in that scenario to avoid interfering with typing.
            return completionItem.Rules.MatchPriority == MatchPriority.Preselect && completionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection;
        }

        internal static ImmutableHashSet<char> GetTriggerCharacters(CompletionProvider provider)
        {
            if (provider is LSPCompletionProvider lspProvider)
            {
                return lspProvider.TriggerCharacters;
            }

            return ImmutableHashSet<char>.Empty;
        }

        internal static async Task<OptionSet> GetCompletionOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            // Filter out snippets as they are not supported in the LSP client
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1139740
            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var completionOptions = documentOptions
                .WithChangedOption(CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude)
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, false)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, false);
            return completionOptions;
        }

        private static LSP.CompletionItemKind GetCompletionKind(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (ProtocolConversions.RoslynTagToCompletionItemKind.TryGetValue(tag, out var completionItemKind))
                {
                    return completionItemKind;
                }
            }

            return LSP.CompletionItemKind.Text;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionHandler _completionHandler;

            public TestAccessor(CompletionHandler completionHandler)
                => _completionHandler = completionHandler;

            public CompletionListCache GetCache()
                => _completionHandler._completionListCache;
        }

        private class CommitCharacterArrayComparer : IEqualityComparer<ImmutableArray<CharacterSetModificationRule>>
        {
            public static readonly CommitCharacterArrayComparer Instance = new();

            private CommitCharacterArrayComparer()
            {
            }

            public bool Equals([AllowNull] ImmutableArray<CharacterSetModificationRule> x, [AllowNull] ImmutableArray<CharacterSetModificationRule> y)
            {
                for (var i = 0; i < x.Length; i++)
                {
                    var xKind = x[i].Kind;
                    var yKind = y[i].Kind;
                    if (xKind != yKind)
                    {
                        return false;
                    }

                    var xCharacters = x[i].Characters;
                    var yCharacters = y[i].Characters;
                    if (xCharacters.Length != yCharacters.Length)
                    {
                        return false;
                    }

                    for (var j = 0; j < xCharacters.Length; j++)
                    {
                        if (xCharacters[j] != yCharacters[j])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public int GetHashCode([DisallowNull] ImmutableArray<CharacterSetModificationRule> obj)
            {
                var combinedHash = Hash.CombineValues(obj);
                return combinedHash;
            }
        }
    }
}
