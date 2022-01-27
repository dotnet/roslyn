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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed class CompletionSource : ForegroundThreadAffinitizedObject, IAsyncExpandingCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string TriggerLocation = nameof(TriggerLocation);
        internal const string ExpandedItemTriggerLocation = nameof(ExpandedItemTriggerLocation);
        internal const string CompletionListSpan = nameof(CompletionListSpan);
        internal const string InsertionText = nameof(InsertionText);
        internal const string HasSuggestionItemOptions = nameof(HasSuggestionItemOptions);
        internal const string Description = nameof(Description);
        internal const string PotentialCommitCharacters = nameof(PotentialCommitCharacters);
        internal const string ExcludedCommitCharacters = nameof(ExcludedCommitCharacters);
        internal const string NonBlockingCompletion = nameof(NonBlockingCompletion);
        internal const string TypeImportCompletionEnabled = nameof(TypeImportCompletionEnabled);
        internal const string TargetTypeFilterExperimentEnabled = nameof(TargetTypeFilterExperimentEnabled);
        internal const string ExpandedItemsTask = nameof(ExpandedItemsTask);

        private static readonly ImmutableArray<ImageElement> s_warningImageAttributeImagesArray =
            ImmutableArray.Create(new ImageElement(Glyph.CompletionWarning.GetImageId(), EditorFeaturesResources.Warning_image_element));

        private static readonly EditorOptionKey<bool> s_nonBlockingCompletionEditorOption = new(NonBlockingCompletion);

        // Use CWT to cache data needed to create VSCompletionItem, so the table would be cleared when Roslyn completion item cache is cleared.
        private static readonly ConditionalWeakTable<RoslynCompletionItem, StrongBox<VSCompletionItemData>> s_roslynItemToVsItemData =
            new();

        private readonly ITextView _textView;
        private readonly bool _isDebuggerTextView;
        private readonly ImmutableHashSet<string> _roles;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
        private readonly VSUtilities.IUIThreadOperationExecutor _operationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;
        private bool _snippetCompletionTriggeredIndirectly;

        internal CompletionSource(
            ITextView textView,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
            IThreadingContext threadingContext,
            VSUtilities.IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListener asyncListener,
            IGlobalOptionService globalOptions)
            : base(threadingContext)
        {
            _textView = textView;
            _streamingPresenter = streamingPresenter;
            _operationExecutor = operationExecutor;
            _asyncListener = asyncListener;
            _globalOptions = globalOptions;
            _isDebuggerTextView = textView is IDebuggerTextView;
            _roles = textView.Roles.ToImmutableHashSet();
        }

        public AsyncCompletionData.CompletionStartData InitializeCompletion(
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            CancellationToken cancellationToken)
        {
            // We take sourceText from document to get a snapshot span.
            // We would like to be sure that nobody changes buffers at the same time.
            AssertIsForeground();

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

            var options = CompletionOptions.From(document.Project);

            // The Editor supports the option per textView.
            // There could be mixed desired behavior per textView and even per same completion session.
            // The right fix would be to send this information as a result of the method. 
            // Then, the Editor would choose the right behavior for mixed cases.
            _textView.Options.GlobalOptions.SetOptionValue(s_nonBlockingCompletionEditorOption, !_globalOptions.GetOption(CompletionViewOptions.BlockForCompletionItems, service.Language));

            // In case of calls with multiple completion services for the same view (e.g. TypeScript and C#), those completion services must not be called simultaneously for the same session.
            // Therefore, in each completion session we use a list of commit character for a specific completion service and a specific content type.
            _textView.Properties[PotentialCommitCharacters] = service.GetRules(options).DefaultCommitCharacters;

            // Reset a flag which means a snippet triggered by ? + Tab.
            // Set it later if met the condition.
            _snippetCompletionTriggeredIndirectly = false;

            CheckForExperimentStatus(_textView, options);

            var sourceText = document.GetTextSynchronously(cancellationToken);

            return ShouldTriggerCompletion(trigger, triggerLocation, sourceText, document, service, options)
                ? new AsyncCompletionData.CompletionStartData(
                    participation: AsyncCompletionData.CompletionParticipation.ProvidesItems,
                    applicableToSpan: new SnapshotSpan(
                        triggerLocation.Snapshot,
                        service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan()))
                : AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;

            // For telemetry reporting purpose
            static void CheckForExperimentStatus(ITextView textView, in CompletionOptions options)
            {
                textView.Properties[TargetTypeFilterExperimentEnabled] = options.TargetTypedCompletionFilter;

                var importCompletionOptionValue = options.ShowItemsFromUnimportedNamespaces;
                var importCompletionExperimentValue = options.TypeImportCompletion;
                var isTypeImportEnababled = importCompletionOptionValue == true || (importCompletionOptionValue == null && importCompletionExperimentValue);
                textView.Properties[TypeImportCompletionEnabled] = isTypeImportEnababled;
            }
        }

        private bool ShouldTriggerCompletion(
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SourceText sourceText,
            Document document,
            CompletionService completionService,
            in CompletionOptions options)
        {
            // The trigger reason guarantees that user wants a completion.
            if (trigger.Reason is AsyncCompletionData.CompletionTriggerReason.Invoke or
                AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique)
            {
                return true;
            }

            // Enter does not trigger completion.
            if (trigger.Reason == AsyncCompletionData.CompletionTriggerReason.Insertion && trigger.Character == '\n')
            {
                return false;
            }

            //The user may be trying to invoke snippets through question-tab.
            // We may provide a completion after that.
            // Otherwise, tab should not be a completion trigger.
            if (trigger.Reason == AsyncCompletionData.CompletionTriggerReason.Insertion && trigger.Character == '\t')
            {
                return TryInvokeSnippetCompletion(completionService, document, sourceText, triggerLocation.Position, options);
            }

            var roslynTrigger = Helpers.GetRoslynTrigger(trigger, triggerLocation);

            // The completion service decides that user may want a completion.
            if (completionService.ShouldTriggerCompletion(document.Project, document.Project.LanguageServices, sourceText, triggerLocation.Position, roslynTrigger, options))
            {
                return true;
            }

            return false;
        }

        private bool TryInvokeSnippetCompletion(
            CompletionService completionService, Document document, SourceText text, int caretPoint, in CompletionOptions options)
        {
            var rules = completionService.GetRules(options);
            // Do not invoke snippet if the corresponding rule is not set in options.
            if (rules.SnippetsRule != SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                return false;
            }

            var syntaxFactsOpt = document.GetLanguageService<ISyntaxFactsService>();
            // Snippets are included if the user types: <quesiton><tab>
            // If at least one condition for snippets do not hold, bail out.
            if (syntaxFactsOpt == null ||
                caretPoint < 3 ||
                text[caretPoint - 2] != '?' ||
                !QuestionMarkIsPrecededByIdentifierAndWhitespace(text, caretPoint - 2, syntaxFactsOpt))
            {
                return false;
            }

            // Because <question><tab> is actually a command to bring up snippets,
            // we delete the last <question> that was typed.
            var textChange = new TextChange(TextSpan.FromBounds(caretPoint - 2, caretPoint), string.Empty);
            document.Project.Solution.Workspace.ApplyTextChanges(document.Id, textChange, CancellationToken.None);

            _snippetCompletionTriggeredIndirectly = true;
            return true;
        }

        public async Task<AsyncCompletionData.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            if (session is null)
                throw new ArgumentNullException(nameof(session));

            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return AsyncCompletionData.CompletionContext.Empty;

            var options = CompletionOptions.From(document.Project);

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
            // triggered, but only include its results if it's completed by the time task #1 is completed, otherwise we don't wait on it and
            // just return items from #1 immediately. Task #2 will still be running in the background (until session is dismissed/committed,)
            // and we'd check back to see if it's completed whenever we have a chance to update the completion list, i.e. when user typed another
            // character, a filter was selected, etc. If so, those items will be added as part of the refresh.
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
            // in time and complete result would be available from the start of the session. However, even in the case only partial result is returned at the start,
            // we still believe this is acceptable given how critical perf is in typing scenario.
            // Additionally, expanded items are usually considered complementary. The need for them only rise occasionally (it's rare when users need to add imports,)
            // and when they are needed, our hypothesis is because of their more intrusive nature (adding an import to the document) users would more likely to
            // contemplate such action thus typing slower before commit and/or spending more time examining the list, which give us some opportunities
            // to still provide those items later before they are truly required.

            // Trigger completion with only "core" providers
            var nonExpandedItemsTask = GetCompletionContextWorkerAsync(document, trigger, triggerLocation,
                options with { ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly }, cancellationToken);

            // Trigger only expanded providers in a separate task if the feature is enabled.
            // The use of an explicit task (instead of calling GetCompletionContextWorkerAsync w/o await)
            // is to make sure the entire computation runs in the background.
            // TODO: handle cancellation!
            var expandedItemsTask = options.ShouldShowItemsFromUnimportNamspaces()
                ? Task.Run(async () => await GetCompletionContextWorkerAsync(document, trigger, triggerLocation,
                    options with { ExpandedCompletionBehavior = ExpandedCompletionMode.ExpandedItemsOnly }, CancellationToken.None).ConfigureAwait(false))
                : null;

            var (nonExpandedContext, nonExpandedCompletionList) = await nonExpandedItemsTask.ConfigureAwait(false);
            AddPropertiesToSession(session, nonExpandedCompletionList, triggerLocation);

            if (expandedItemsTask == null)
                return nonExpandedContext;

            if (!expandedItemsTask.IsCompleted)
            {
                session.Properties[ExpandedItemsTask] = expandedItemsTask;
                return nonExpandedContext;
            }

            // the task of expanded item is completed, so get the result and combine it with result of non-expanded items.
            var (expandedContext, expandedCompletionList) = await expandedItemsTask.ConfigureAwait(false);
            AddPropertiesToSession(session, expandedCompletionList, triggerLocation: null);

            return CombineCompletionContext(nonExpandedContext, expandedContext);

            static AsyncCompletionData.CompletionContext CombineCompletionContext(AsyncCompletionData.CompletionContext context1, AsyncCompletionData.CompletionContext context2)
            {
                if (context1.Items.IsEmpty && context1.SuggestionItemOptions is null)
                    return context2;

                if (context2.Items.IsEmpty && context2.SuggestionItemOptions is null)
                    return context1;

                var _ = ArrayBuilder<VSCompletionItem>.GetInstance(context1.Items.Length + context2.Items.Length, out var itemsBuilder);
                itemsBuilder.AddRange(context1.Items);
                itemsBuilder.AddRange(context2.Items);

                var filterStates = Helpers.CombineFilterStates(context1.Filters, context2.Filters);

                var suggestionItem = context1.SuggestionItemOptions ?? context2.SuggestionItemOptions;
                var hint = suggestionItem == null ? AsyncCompletionData.InitialSelectionHint.RegularSelection : AsyncCompletionData.InitialSelectionHint.SoftSelection;

                return new(itemsBuilder.ToImmutable(), suggestionItem, hint, filterStates);
            }
        }

        public async Task<AsyncCompletionData.CompletionContext> GetExpandedCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionExpander expander,
            AsyncCompletionData.CompletionTrigger intialTrigger,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            // We only want to provide expanded items for Roslyn's expander.
            if (expander == FilterSet.Expander && session.Properties.TryGetProperty(ExpandedItemTriggerLocation, out SnapshotPoint initialTriggerLocation))
            {
                AsyncCompletionLogger.LogExpanderUsage();

                // It's possible we didn't provide expanded items at the beginning of completion session because it was slow even if the feature is enabled.
                // ExpandedItemsTask would be available in this case, so we just need to return the its result.
                // We are doing this in a spinning fashion because the lifetime of expandedItemsTask is associated with the completion session, we don't
                // want to cancel that task when this "GetExpandedCompletionContextAsync" operation is being canceled, so we will still be able to retrieve
                // the results later.
                if (session.Properties.TryGetProperty<Task<(AsyncCompletionData.CompletionContext, CompletionList)>>(ExpandedItemsTask, out var expandedItemsTask))
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (expandedItemsTask.IsCompleted)
                        {
                            var (expandedContext, expandedCompletionList) = await expandedItemsTask.ConfigureAwait(false);
                            session.Properties.RemoveProperty(ExpandedItemsTask);
                            AddPropertiesToSession(session, expandedCompletionList, triggerLocation: null);
                            return expandedContext;
                        }

                        await Task.Delay(20, cancellationToken).ConfigureAwait(false);
                    }
                }

                // We only reach here when expanded items are disabled, but user requested them explicitly via expander.
                // In this case, enable expanded items and trigger the completion only for them.
                var document = initialTriggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    var options = CompletionOptions.From(document.Project) with
                    {
                        ShowItemsFromUnimportedNamespaces = true,
                        ExpandedCompletionBehavior = ExpandedCompletionMode.ExpandedItemsOnly
                    };

                    var (context, completionList) = await GetCompletionContextWorkerAsync(document, intialTrigger, initialTriggerLocation, options, cancellationToken).ConfigureAwait(false));
                    AddPropertiesToSession(session, completionList, triggerLocation: null);

                    return context;
                }
            }

            return AsyncCompletionData.CompletionContext.Empty;
        }

        private async Task<(AsyncCompletionData.CompletionContext, CompletionList)> GetCompletionContextWorkerAsync(
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
                    ShowXmlDocCommentCompletion = false
                };
            }

            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var roslynTrigger = _snippetCompletionTriggeredIndirectly
                ? new CompletionTrigger(CompletionTriggerKind.Snippets)
                : Helpers.GetRoslynTrigger(trigger, triggerLocation);

            var completionList = await completionService.GetCompletionsAsync(
                document, triggerLocation, options, roslynTrigger, _roles, cancellationToken).ConfigureAwait(false);

            var filterSet = new FilterSet();
            using var _ = ArrayBuilder<VSCompletionItem>.GetInstance(completionList.Items.Length, out var itemsBuilder);

            foreach (var roslynItem in completionList.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = Convert(document, roslynItem, filterSet, triggerLocation);
                itemsBuilder.Add(item);
            }

            var filters = filterSet.GetFilterStatesInSet();
            var items = itemsBuilder.ToImmutable();

            if (completionList.SuggestionModeItem is null)
                return (new(items, suggestionItemOptions: null, selectionHint: AsyncCompletionData.InitialSelectionHint.RegularSelection, filters), completionList);

            var suggestionItemOptions = new AsyncCompletionData.SuggestionItemOptions(
                completionList.SuggestionModeItem.DisplayText,
                completionList.SuggestionModeItem.Properties.TryGetValue(Description, out var description) ? description : string.Empty);

            return (new(items, suggestionItemOptions, selectionHint: AsyncCompletionData.InitialSelectionHint.SoftSelection, filters), completionList);
        }

        private static void AddPropertiesToSession(IAsyncCompletionSession session, CompletionList completionList, SnapshotPoint? triggerLocation)
        {
            // Store around the span this completion list applies to.  We'll use this later
            // to pass this value in when we're committing a completion list item.
            // It's OK to overwrite this value when expanded items are requested.
            session.Properties[CompletionListSpan] = completionList.Span;

            // This is a code supporting original completion scenarios: 
            // Controller.Session_ComputeModel: if completionList.SuggestionModeItem != null, then suggestionMode = true
            // If there are suggestionItemOptions, then later HandleNormalFiltering should set selection to SoftSelection.
            if (!session.Properties.TryGetProperty(HasSuggestionItemOptions, out bool hasSuggestionItemOptionsBefore) || !hasSuggestionItemOptionsBefore)
            {
                session.Properties[HasSuggestionItemOptions] = completionList.SuggestionModeItem != null;
            }

            var excludedCommitCharacters = GetExcludedCommitCharacters(completionList.Items);
            if (excludedCommitCharacters.Length > 0)
            {
                if (session.Properties.TryGetProperty(ExcludedCommitCharacters, out ImmutableArray<char> excludedCommitCharactersBefore))
                {
                    excludedCommitCharacters = excludedCommitCharacters.Union(excludedCommitCharactersBefore).ToImmutableArray();
                }

                session.Properties[ExcludedCommitCharacters] = excludedCommitCharacters;
            }

            // We need to remember the trigger location for when a completion service claims expanded items are available
            // since the initial trigger we are able to get from IAsyncCompletionSession might not be the same (e.g. in projection scenarios)
            // so when they are requested via expander later, we can retrieve it.
            // Technically we should save the trigger location for each individual service that made such claim, but in reality only Roslyn's
            // completion service uses expander, so we can get away with not making such distinction.
            if (triggerLocation.HasValue && !session.Properties.ContainsProperty(ExpandedItemTriggerLocation))
            {
                session.Properties[ExpandedItemTriggerLocation] = triggerLocation.Value;
            }
        }

        public async Task<object?> GetDescriptionAsync(IAsyncCompletionSession session, VSCompletionItem item, CancellationToken cancellationToken)
        {
            if (session is null)
                throw new ArgumentNullException(nameof(session));
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (!item.Properties.TryGetProperty(RoslynItem, out RoslynCompletionItem roslynItem) ||
                !item.Properties.TryGetProperty(TriggerLocation, out SnapshotPoint triggerLocation))
            {
                return null;
            }

            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var service = document.GetLanguageService<CompletionService>();
            if (service == null)
                return null;

            var options = CompletionOptions.From(document.Project);
            var displayOptions = SymbolDescriptionOptions.From(document.Project);
            var description = await service.GetDescriptionAsync(document, roslynItem, options, displayOptions, cancellationToken).ConfigureAwait(false);
            if (description == null)
                return null;

            var context = new IntellisenseQuickInfoBuilderContext(
                document, ThreadingContext, _operationExecutor, _asyncListener, _streamingPresenter);

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
        private readonly record struct VSCompletionItemData
            (string DisplayText, ImageElement Icon, ImmutableArray<AsyncCompletionData.CompletionFilter> Filters,
                int FilterSetData, ImmutableArray<ImageElement> AttributeIcons, string InsertionText);

        private VSCompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            FilterSet filterSet,
            SnapshotPoint initialTriggerLocation)
        {
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
                if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
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

            item.Properties.AddProperty(RoslynItem, roslynItem);
            item.Properties.AddProperty(TriggerLocation, initialTriggerLocation);

            return item;
        }

        private static ImmutableArray<char> GetExcludedCommitCharacters(ImmutableArray<RoslynCompletionItem> roslynItems)
        {
            var hashSet = new HashSet<char>();
            foreach (var roslynItem in roslynItems)
            {
                foreach (var rule in roslynItem.Rules.FilterCharacterRules)
                {
                    if (rule.Kind == CharacterSetModificationKind.Add)
                    {
                        foreach (var c in rule.Characters)
                        {
                            hashSet.Add(c);
                        }
                    }
                }
            }

            return hashSet.ToImmutableArray();
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
