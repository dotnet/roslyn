// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionContext = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed class CompletionSource : IAsyncExpandingCompletionSource
    {
        internal const string PotentialCommitCharacters = nameof(PotentialCommitCharacters);
        internal const string NonBlockingCompletion = nameof(NonBlockingCompletion);

        // Don't change this property! Editor code currently has a dependency on it.
        internal const string ExcludedCommitCharacters = nameof(ExcludedCommitCharacters);
        internal const string ExcludedCommitCharactersMap = nameof(ExcludedCommitCharactersMap);

        private static readonly ImmutableArray<ImageElement> s_warningImageAttributeImagesArray =
            ImmutableArray.Create(new ImageElement(Glyph.CompletionWarning.GetImageId(), EditorFeaturesResources.Warning_image_element));

        private static readonly EditorOptionKey<bool> s_nonBlockingCompletionEditorOption = new(NonBlockingCompletion);

        // Use CWT to cache data needed to create VSCompletionItem, so the table would be cleared when Roslyn completion item cache is cleared.
        private static readonly ConditionalWeakTable<RoslynCompletionItem, StrongBox<VSCompletionItemData>> s_roslynItemToVsItemData =
            new();

        // Cancellation series we use to stop background task for expanded items when exclusive items are returned by core providers.
        private readonly CancellationSeries _expandedItemsTaskCancellationSeries = new();

        private readonly ITextView _textView;
        private readonly bool _isDebuggerTextView;
        private readonly ImmutableHashSet<string> _roles;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
        private readonly IThreadingContext _threadingContext;
        private readonly VSUtilities.IUIThreadOperationExecutor _operationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly EditorOptionsService _editorOptionsService;
        private bool _snippetCompletionTriggeredIndirectly;

        internal CompletionSource(
            ITextView textView,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
            IThreadingContext threadingContext,
            VSUtilities.IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListener asyncListener,
            EditorOptionsService editorOptionsService)
        {
            _textView = textView;
            _streamingPresenter = streamingPresenter;
            _threadingContext = threadingContext;
            _operationExecutor = operationExecutor;
            _asyncListener = asyncListener;
            _editorOptionsService = editorOptionsService;
            _isDebuggerTextView = textView is IDebuggerTextView;
            _roles = textView.Roles.ToImmutableHashSet();
        }

        public AsyncCompletionData.CompletionStartData InitializeCompletion(
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            try
            {
                // We take sourceText from document to get a snapshot span.
                // We would like to be sure that nobody changes buffers at the same time.
                _threadingContext.ThrowIfNotOnUIThread();

                if (_textView.Selection.Mode == TextSelectionMode.Box)
                {
                    // No completion with multiple selection
                    return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
                }

                var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
                }

                var service = document.GetLanguageService<CompletionService>();
                if (service == null)
                {
                    return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
                }

                var options = _editorOptionsService.GlobalOptions.GetCompletionOptions(document.Project.Language);

                // The Editor supports the option per textView.
                // There could be mixed desired behavior per textView and even per same completion session.
                // The right fix would be to send this information as a result of the method. 
                // Then, the Editor would choose the right behavior for mixed cases.
                var blockForCompletionItem = _editorOptionsService.GlobalOptions.GetOption(CompletionViewOptionsStorage.BlockForCompletionItems, service.Language);
                _textView.Options.GlobalOptions.SetOptionValue(s_nonBlockingCompletionEditorOption, !blockForCompletionItem);

                // In case of calls with multiple completion services for the same view (e.g. TypeScript and C#), those completion services must not be called simultaneously for the same session.
                // Therefore, in each completion session we use a list of commit character for a specific completion service and a specific content type.
                _textView.Properties[PotentialCommitCharacters] = service.GetRules(options).DefaultCommitCharacters;

                // Reset a flag which means a snippet triggered by ? + Tab.
                // Set it later if met the condition.
                _snippetCompletionTriggeredIndirectly = false;

                var sourceText = document.GetTextSynchronously(cancellationToken);

                return ShouldTriggerCompletion(trigger, triggerLocation, sourceText, document, service, options)
                    ? new AsyncCompletionData.CompletionStartData(
                        participation: AsyncCompletionData.CompletionParticipation.ProvidesItems,
                        applicableToSpan: new SnapshotSpan(
                            triggerLocation.Snapshot,
                            service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan()))
                    : AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
            }
            finally
            {
                AsyncCompletionLogger.LogSourceInitializationTicksDataPoint(stopwatch.Elapsed);
            }
        }

        private bool ShouldTriggerCompletion(
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SourceText sourceText,
            Document document,
            CompletionService completionService,
            CompletionOptions options)
        {
            //The user may be trying to invoke snippets through question-tab.
            // We may provide a completion after that.
            // Otherwise, tab should not be a completion trigger.
            if (trigger.Reason == AsyncCompletionData.CompletionTriggerReason.Insertion && trigger.Character == '\t')
            {
                return TryInvokeSnippetCompletion(triggerLocation.Snapshot.TextBuffer, triggerLocation.Position, sourceText, document.Project.Services, completionService.GetRules(options));
            }

            var roslynTrigger = Helpers.GetRoslynTrigger(trigger, triggerLocation);

            // The completion service decides that user may want a completion.
            return completionService.ShouldTriggerCompletion(
                document.Project, document.Project.Services, sourceText, triggerLocation.Position, roslynTrigger, options, document.Project.Solution.Options, _roles);
        }

        private bool TryInvokeSnippetCompletion(
            ITextBuffer buffer, int caretPoint, SourceText text, LanguageServices services, CompletionRules rules)
        {
            // Do not invoke snippet if the corresponding rule is not set in options.
            if (rules.SnippetsRule != SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                return false;
            }

            var syntaxFacts = services.GetService<ISyntaxFactsService>();
            // Snippets are included if the user types: <question><tab>
            // If at least one condition for snippets do not hold, bail out.
            if (syntaxFacts == null ||
                caretPoint < 3 ||
                text[caretPoint - 2] != '?' ||
                !QuestionMarkIsPrecededByIdentifierAndWhitespace(text, caretPoint - 2, syntaxFacts))
            {
                return false;
            }

            // Because <question><tab> is actually a command to bring up snippets,
            // we delete the last <question> that was typed.
            buffer.ApplyChange(new TextChange(TextSpan.FromBounds(caretPoint - 2, caretPoint), string.Empty));

            _snippetCompletionTriggeredIndirectly = true;
            return true;
        }

        public async Task<VSCompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            var totalStopWatch = SharedStopwatch.StartNew();
            try
            {
                if (session is null)
                    throw new ArgumentNullException(nameof(session));

                var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return VSCompletionContext.Empty;

                // The computation of completion items is divided into two tasks:
                //
                // 1. "Core" items (i.e. non-expanded) which should be included in the list regardless of the selection of expander.
                //    Right now this includes all items except those from unimported namespaces.
                //    
                // 2. Expanded items which only show in the completion list when expander is selected, or by default if the corresponding
                //    features are enabled. Right now only items from unimported namespaces are associated with expander. 
                //
                // #1 is the essence of completion so we'd always wait until its task is completed and return the results. However, because we have
                // a really tight perf budget in completion, and computing those items in #2 could be expensive especially in a large solution
                // (e.g. requires syntax/symbol indices and/or runs in OOP,) we decide to kick off the computation in parallel when completion is
                // triggered, but only include its results if:
                //
                //      (a) it's completed by the time task #1 is completed and
                //      (b) including them won't interfere with users' ability to browse the list (e.g. when the list is too long since filter text is short)
                //
                // Otherwise we don't wait on it and return items from #1 immediately. Task #2 will still be running in the background
                // (until session is dismissed/committed) and we'd check back to see if it's completed whenever we have a chance to update the completion list,
                // i.e. when user typed another character, a filter was selected, etc. If so, those items will be added as part of the refresh.
                //
                // The reason of adopting this approach is we want to minimize typing delays. There are two ways user might perceive a delay in typing.
                // First, they could see a delay between typing a character and completion list being displayed if they want to examine the items available.
                // Second, they might be typing continuously w/o paying attention to completion list, and simply expect the completion to do the "right thing"
                // when a commit char is typed (e.g. commit "cancellationToken" when typing 'can$TAB$'). However, the commit could be delayed if completion is
                // still waiting on the computation of all available items, which manifests as UI delays and in worst case timeouts in commit which results in
                // unexpected behavior (e.g. typing 'can$TAB$' results in a 250ms UI freeze and still ends up with "can" instead of "cancellationToken".)
                //
                // This approach would ensure the computation of #2 will not be the cause of such delays, with the obvious trade off of potentially not providing
                // expanded items until later (or never) in a completion session even if the feature is enabled. Note that in most cases we'd expect task #2 to finish
                // in time and complete result would be available when it's most likely needed (see `ShouldHideExpandedItems` helper in ItemManager for details.)
                // However, even in the case only partial result is returned at the start, we still believe this is acceptable given how critical perf is in typing scenario.
                // Additionally, expanded items are usually considered complementary. The need for them only rise occasionally (it's rare when users need to add imports,)
                // and when they are needed, our hypothesis is because of their more intrusive nature (adding an import to the document) users would more likely to
                // contemplate such action thus typing slower before commit and/or spending more time examining the list, which give us some opportunities
                // to still provide those items later before they are truly required.

                var showCompletionItemFilters = _editorOptionsService.GlobalOptions.GetOption(CompletionViewOptionsStorage.ShowCompletionItemFilters, document.Project.Language);
                var options = _editorOptionsService.GlobalOptions.GetCompletionOptions(document.Project.Language) with
                {
                    PerformSort = false,
                    UpdateImportCompletionCacheInBackground = true,
                    TargetTypedCompletionFilter = showCompletionItemFilters // Compute targeted types if filter is enabled
                };

                var sessionData = CompletionSessionData.GetOrCreateSessionData(session);

                if (!options.ShouldShowItemsFromUnimportedNamespaces)
                {
                    // No need to trigger expanded providers at all if the feature is disabled, just trigger core providers and return;
                    var (context, list) = await GetCompletionContextWorkerAsync(session, document, trigger, triggerLocation,
                        options with { ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly }, cancellationToken).ConfigureAwait(false);

                    UpdateSessionData(session, sessionData, list, triggerLocation);
                    return context;
                }
                else
                {
                    // Kicking off the task for expanded items, so it runs in parallel with regular providers.
                    // Otherwise, the computation of unimported items won't start until we return those regular items to editor,
                    // which combined with our behavior of not showing expanded items until ready (and only adding them during
                    // completion list refresh) means increased chance that users won't see those items for the first few characters typed.
                    // This does mean we might do unnecessary work if any regular provider is `exclusive`, but such cases are relatively infrequent
                    // and we'd like to have expanded items available when they are needed. As these results come back potentially after
                    // presentation (and sorting) of the non-expanded results, we need these results to come back already sorted.
                    var expandedItemsTaskCancellationToken = _expandedItemsTaskCancellationSeries.CreateNext(cancellationToken);
                    var expandedItemsTask = Task.Run(() => GetCompletionContextWorkerAsync(session, document, trigger, triggerLocation,
                                                                        options with { ExpandedCompletionBehavior = ExpandedCompletionMode.ExpandedItemsOnly, PerformSort = true },
                                                                        expandedItemsTaskCancellationToken),
                                                     expandedItemsTaskCancellationToken);

                    // Now trigger and wait for core providers to return;
                    var (nonExpandedContext, nonExpandedCompletionList) = await GetCompletionContextWorkerAsync(session, document, trigger, triggerLocation,
                            options with { ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly }, cancellationToken).ConfigureAwait(false);

                    UpdateSessionData(session, sessionData, nonExpandedCompletionList, triggerLocation);

                    if (sessionData.IsExclusive)
                    {
                        // If the core items are exclusive, we won't ever include expanded items.
                        // This would cancel expandedItemsTask.
                        _ = _expandedItemsTaskCancellationSeries.CreateNext(CancellationToken.None);
                    }
                    else
                    {
                        sessionData.ExpandedItemsTask = expandedItemsTask;
                    }

                    AsyncCompletionLogger.LogImportCompletionGetContext();
                    return nonExpandedContext;
                }
            }
            finally
            {
                AsyncCompletionLogger.LogSourceGetContextTicksDataPoint(totalStopWatch.Elapsed, isCanceled: cancellationToken.IsCancellationRequested);
            }
        }

        public async Task<VSCompletionContext> GetExpandedCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionExpander expander,
            AsyncCompletionData.CompletionTrigger initialTrigger,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            var sessionData = CompletionSessionData.GetOrCreateSessionData(session);

            // We only want to provide expanded items for Roslyn's expander
            if (!sessionData.IsExclusive && expander == FilterSet.Expander && sessionData.ExpandedItemTriggerLocation.HasValue)
            {
                var initialTriggerLocation = sessionData.ExpandedItemTriggerLocation.Value;
                AsyncCompletionLogger.LogExpanderUsage();

                // It's possible we didn't provide expanded items at the beginning of completion session because it was slow even if the feature is enabled.
                // ExpandedItemsTask would be available in this case, so we just need to return its result.
                if (sessionData.ExpandedItemsTask is not null)
                {
                    // Make sure the task is removed when returning expanded items,
                    // so duplicated items won't be added in subsequent list updates.
                    var task = sessionData.ExpandedItemsTask;
                    sessionData.ExpandedItemsTask = null;

                    var (expandedContext, expandedCompletionList) = await task.ConfigureAwait(false);
                    UpdateSessionData(session, sessionData, expandedCompletionList, initialTriggerLocation);
                    return expandedContext;
                }

                if (sessionData.CombinedSortedList is null)
                {
                    // We only reach here when expanded items are disabled, but user requested them explicitly via expander.
                    // In this case, enable expanded items and trigger the completion only for them.
                    var document = initialTriggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document != null)
                    {
                        // User selected expander explicitly, which means we need to collect and return
                        // items from unimported namespace (and only those items) regardless of whether it's enabled.
                        var options = _editorOptionsService.GlobalOptions.GetCompletionOptions(document.Project.Language) with
                        {
                            ShowItemsFromUnimportedNamespaces = true,
                            ExpandedCompletionBehavior = ExpandedCompletionMode.ExpandedItemsOnly
                        };

                        var (context, completionList) = await GetCompletionContextWorkerAsync(session, document, initialTrigger, initialTriggerLocation, options, cancellationToken).ConfigureAwait(false);
                        UpdateSessionData(session, sessionData, completionList, initialTriggerLocation);

                        return context;
                    }
                }
            }

            return VSCompletionContext.Empty;
        }

        private async Task<(VSCompletionContext, CompletionList)> GetCompletionContextWorkerAsync(
            IAsyncCompletionSession session,
            Document document,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            CompletionOptions options,
            CancellationToken cancellationToken)
        {
            if (_isDebuggerTextView)
            {
                options = options with
                {
                    FilterOutOfScopeLocals = false,
                    ShowXmlDocCommentCompletion = false,
                    // Adding import is not allowed in debugger view
                    CanAddImportStatement = false,
                };
            }

            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var roslynTrigger = _snippetCompletionTriggeredIndirectly
                ? new CompletionTrigger(CompletionTriggerKind.Snippets)
                : Helpers.GetRoslynTrigger(trigger, triggerLocation);

            var completionList = await completionService.GetCompletionsAsync(
                document, triggerLocation, options, document.Project.Solution.Options, roslynTrigger, _roles, cancellationToken).ConfigureAwait(false);

            var filterSet = new FilterSet(document.Project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic);
            var completionItemList = session.CreateCompletionList(
                completionList.ItemsList.Select(i => Convert(document, i, filterSet, triggerLocation, cancellationToken)));

            var filters = filterSet.GetFilterStatesInSet();

            if (completionList.SuggestionModeItem is null)
                return (new(completionItemList, suggestionItemOptions: null, selectionHint: AsyncCompletionData.InitialSelectionHint.RegularSelection, filters, isIncomplete: false, null), completionList);

            var suggestionItemOptions = new AsyncCompletionData.SuggestionItemOptions(
                completionList.SuggestionModeItem.DisplayText,
                completionList.SuggestionModeItem.TryGetProperty(CommonCompletionItem.DescriptionProperty, out var description) ? description : string.Empty);

            return (new(completionItemList, suggestionItemOptions, selectionHint: AsyncCompletionData.InitialSelectionHint.SoftSelection, filters, isIncomplete: false, null), completionList);
        }

        private static void UpdateSessionData(IAsyncCompletionSession session, CompletionSessionData sessionData, CompletionList completionList, SnapshotPoint triggerLocation)
        {
            sessionData.IsExclusive |= completionList.IsExclusive;

            // Store around the span this completion list applies to.  We'll use this later
            // to pass this value in when we're committing a completion list item.
            // It's OK to overwrite this value when expanded items are requested.
            sessionData.CompletionListSpan = completionList.Span;

            // This is a code supporting original completion scenarios: 
            // Controller.Session_ComputeModel: if completionList.SuggestionModeItem != null, then suggestionMode = true
            // If there are suggestionItemOptions, then later HandleNormalFiltering should set selection to SoftSelection.
            sessionData.HasSuggestionItemOptions |= completionList.SuggestionModeItem != null;

            var excludedCommitCharactersFromList = GetExcludedCommitCharacters(completionList.ItemsList);
            if (session.Properties.TryGetProperty(ExcludedCommitCharactersMap, out MultiDictionary<char, RoslynCompletionItem> excludedCommitCharactersMap))
            {
                foreach (var kvp in excludedCommitCharactersFromList)
                {
                    foreach (var item in kvp.Value)
                    {
                        excludedCommitCharactersMap.Add(kvp.Key, item);
                    }
                }
            }
            else
            {
                excludedCommitCharactersMap = excludedCommitCharactersFromList;
            }

            session.Properties[ExcludedCommitCharactersMap] = excludedCommitCharactersMap;
            session.Properties[ExcludedCommitCharacters] = excludedCommitCharactersMap.Keys.ToImmutableArray();

            // We need to remember the trigger location for when a completion service claims expanded items are available
            // since the initial trigger we are able to get from IAsyncCompletionSession might not be the same (e.g. in projection scenarios)
            // so when they are requested via expander later, we can retrieve it.
            // Technically we should save the trigger location for each individual service that made such claim, but in reality only Roslyn's
            // completion service uses expander, so we can get away with not making such distinction.
            if (!sessionData.ExpandedItemTriggerLocation.HasValue)
            {
                sessionData.ExpandedItemTriggerLocation = triggerLocation;
            }
        }

        public async Task<object?> GetDescriptionAsync(IAsyncCompletionSession session, VSCompletionItem item, CancellationToken cancellationToken)
        {
            if (session is null)
                throw new ArgumentNullException(nameof(session));
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (!CompletionItemData.TryGetData(item, out var itemData) || !itemData.TriggerLocation.HasValue)
                return null;

            var snapshot = itemData.TriggerLocation.Value.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var service = document.GetLanguageService<CompletionService>();
            if (service == null)
                return null;

            var completionOptions = _editorOptionsService.GlobalOptions.GetCompletionOptions(document.Project.Language);
            var displayOptions = _editorOptionsService.GlobalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var description = await service.GetDescriptionAsync(document, itemData.RoslynItem, completionOptions, displayOptions, cancellationToken).ConfigureAwait(false);
            if (description == null)
                return null;

            var lineFormattingOptions = snapshot.TextBuffer.GetLineFormattingOptions(_editorOptionsService, explicitFormat: false);
            var context = new IntellisenseQuickInfoBuilderContext(
                document, displayOptions.ClassificationOptions, lineFormattingOptions, _threadingContext, _operationExecutor, _asyncListener, _streamingPresenter);

            var elements = IntelliSense.Helpers.BuildInteractiveTextElements(description.TaggedParts, context).ToArray();
            if (elements.Length == 0)
                return new ClassifiedTextElement();

            if (elements.Length == 1)
                return elements[0];

            return new ContainerElement(ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding, elements);
        }

        /// <summary>
        /// We'd like to cache VS Completion item directly to avoid allocation completely. However it holds references
        /// to transient objects, which would cause memory leak (among other potential issues) if cached. 
        /// So as a compromise,  we cache data that can be calculated from Roslyn completion item to avoid repeated 
        /// calculation cost for cached Roslyn completion items.
        /// FilterSetData is the bit vector value from the FilterSet of this item.
        /// </summary>
        private readonly record struct VSCompletionItemData(
            string DisplayText,
            ImageElement Icon,
            ImmutableArray<AsyncCompletionData.CompletionFilter> Filters,
            int FilterSetData,
            ImmutableArray<ImageElement> AttributeIcons,
            string InsertionText);

        private VSCompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            FilterSet filterSet,
            SnapshotPoint initialTriggerLocation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VSCompletionItemData itemData;
            if (roslynItem.Flags.IsCached() && s_roslynItemToVsItemData.TryGetValue(roslynItem, out var boxedItemData))
            {
                itemData = boxedItemData.Value;
                filterSet.CombineData(itemData.FilterSetData);
            }
            else
            {
                var imageId = roslynItem.Tags.GetFirstGlyph().GetImageId();
                var (filters, filterSetData) = filterSet.GetFiltersAndAddToSet(roslynItem);

                // roslynItem generated by providers can contain an insertionText in a property bag.
                // We will not use it but other providers may need it.
                // We actually will calculate the insertion text once again when called TryCommit.
                if (!SymbolCompletionItem.TryGetInsertionText(roslynItem, out var insertionText))
                {
                    insertionText = roslynItem.DisplayText;
                }

                var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution);
                var attributeImages = supportedPlatforms != null ? s_warningImageAttributeImagesArray : ImmutableArray<ImageElement>.Empty;

                itemData = new VSCompletionItemData(
                    DisplayText: roslynItem.GetEntireDisplayText(),
                    Icon: new ImageElement(new ImageId(imageId.Guid, imageId.Id), roslynItem.DisplayText),
                    Filters: filters,
                    FilterSetData: filterSetData,
                    AttributeIcons: attributeImages,
                    InsertionText: insertionText);

                // It doesn't make sense to cache VS item data for those Roslyn items created from scratch for each session,
                // since CWT uses object identity for comparison.
                if (roslynItem.Flags.IsCached())
                {
                    s_roslynItemToVsItemData.Add(roslynItem, new StrongBox<VSCompletionItemData>(itemData));
                }
            }

            var item = new VSCompletionItem(
                displayText: itemData.DisplayText,
                source: this,
                icon: itemData.Icon,
                filters: itemData.Filters,
                suffix: roslynItem.InlineDescription, // InlineDescription will be right-aligned in the selection popup
                insertText: itemData.InsertionText,
                sortText: roslynItem.SortText,
                filterText: roslynItem.FilterText,
                automationText: roslynItem.AutomationText ?? roslynItem.DisplayText,
                attributeIcons: itemData.AttributeIcons);

            CompletionItemData.AddData(item, roslynItem, initialTriggerLocation);
            return item;
        }

        /// <summary>
        /// Build a map from added filter characters to corresponding items.
        /// CommitManager needs this information to decide whether it should commit selected item.
        /// </summary>
        private static MultiDictionary<char, RoslynCompletionItem> GetExcludedCommitCharacters(IReadOnlyList<RoslynCompletionItem> roslynItems)
        {
            var map = new MultiDictionary<char, RoslynCompletionItem>();
            foreach (var roslynItem in roslynItems)
            {
                foreach (var rule in roslynItem.Rules.FilterCharacterRules)
                {
                    if (rule.Kind == CharacterSetModificationKind.Add)
                    {
                        foreach (var c in rule.Characters)
                        {
                            map.Add(c, roslynItem);
                        }
                    }
                }
            }

            return map;
        }

        internal static bool QuestionMarkIsPrecededByIdentifierAndWhitespace(
            SourceText text, int questionPosition, ISyntaxFactsService syntaxFacts)
        {
            var startOfLine = text.Lines.GetLineFromPosition(questionPosition).Start;

            // First, skip all the whitespace.
            var current = startOfLine;
            while (current < questionPosition && char.IsWhiteSpace(text[current]))
            {
                current++;
            }

            if (current < questionPosition && syntaxFacts.IsIdentifierStartCharacter(text[current]))
            {
                current++;
            }
            else
            {
                return false;
            }

            while (current < questionPosition && syntaxFacts.IsIdentifierPartCharacter(text[current]))
            {
                current++;
            }

            return current == questionPosition;
        }
    }
}
