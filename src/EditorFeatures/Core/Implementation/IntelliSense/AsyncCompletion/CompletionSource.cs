// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal class CompletionSource : ForegroundThreadAffinitizedObject, IAsyncExpandingCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string CompletionListSpan = nameof(CompletionListSpan);
        internal const string InsertionText = nameof(InsertionText);
        internal const string HasSuggestionItemOptions = nameof(HasSuggestionItemOptions);
        internal const string Description = nameof(Description);
        internal const string PotentialCommitCharacters = nameof(PotentialCommitCharacters);
        internal const string ExcludedCommitCharacters = nameof(ExcludedCommitCharacters);
        internal const string NonBlockingCompletion = nameof(NonBlockingCompletion);
        internal const string TypeImportCompletionEnabled = nameof(TypeImportCompletionEnabled);
        internal const string TargetTypeFilterExperimentEnabled = nameof(TargetTypeFilterExperimentEnabled);

        private static readonly ImmutableArray<ImageElement> s_WarningImageAttributeImagesArray =
            ImmutableArray.Create(new ImageElement(Glyph.CompletionWarning.GetImageId(), EditorFeaturesResources.Warning_image_element));

        private static readonly EditorOptionKey<bool> NonBlockingCompletionEditorOption = new EditorOptionKey<bool>(NonBlockingCompletion);

        // Use CWT to cache data needed to create VSCompletionItem, so the table would be cleared when Roslyn completion item cache is cleared.
        private static readonly ConditionalWeakTable<RoslynCompletionItem, StrongBox<VSCompletionItemData>> s_roslynItemToVsItemData =
            new ConditionalWeakTable<RoslynCompletionItem, StrongBox<VSCompletionItemData>>();

        private readonly ITextView _textView;
        private readonly bool _isDebuggerTextView;
        private readonly ImmutableHashSet<string> _roles;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
        private bool _snippetCompletionTriggeredIndirectly;

        internal CompletionSource(ITextView textView, Lazy<IStreamingFindUsagesPresenter> streamingPresenter, IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _textView = textView;
            _streamingPresenter = streamingPresenter;
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

            // The Editor supports the option per textView.
            // There could be mixed desired behavior per textView and even per same completion session.
            // The right fix would be to send this information as a result of the method. 
            // Then, the Editor would choose the right behavior for mixed cases.
            _textView.Options.GlobalOptions.SetOptionValue(NonBlockingCompletionEditorOption, !document.Project.Solution.Workspace.Options.GetOption(CompletionOptions.BlockForCompletionItems, service.Language));

            // In case of calls with multiple completion services for the same view (e.g. TypeScript and C#), those completion services must not be called simultaneously for the same session.
            // Therefore, in each completion session we use a list of commit character for a specific completion service and a specific content type.
            _textView.Properties[PotentialCommitCharacters] = service.GetRules().DefaultCommitCharacters;

            // Reset a flag which means a snippet triggerred by ? + Tab.
            // Set it later if met the condition.
            _snippetCompletionTriggeredIndirectly = false;

            CheckForExperimentStatus(_textView, document);

            var sourceText = document.GetTextSynchronously(cancellationToken);

            return ShouldTriggerCompletion(trigger, triggerLocation, sourceText, document, service)
                ? new AsyncCompletionData.CompletionStartData(
                    participation: AsyncCompletionData.CompletionParticipation.ProvidesItems,
                    applicableToSpan: new SnapshotSpan(
                        triggerLocation.Snapshot,
                        service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan()))
                : AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;

            // For telemetry reporting purpose
            static void CheckForExperimentStatus(ITextView textView, Document document)
            {
                var workspace = document.Project.Solution.Workspace;

                var experimentationService = workspace.Services.GetService<IExperimentationService>();
                textView.Properties[TargetTypeFilterExperimentEnabled] = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.TargetTypedCompletionFilter);

                var importCompletionOptionValue = workspace.Options.GetOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, document.Project.Language);
                var importCompletionExperimentValue = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion);
                var isTypeImportEnababled = importCompletionOptionValue == true || (importCompletionOptionValue == null && importCompletionExperimentValue);
                textView.Properties[TypeImportCompletionEnabled] = isTypeImportEnababled;
            }
        }

        private bool ShouldTriggerCompletion(
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SourceText sourceText,
            Document document,
            CompletionService completionService)
        {
            // The trigger reason guarantees that user wants a completion.
            if (trigger.Reason == AsyncCompletionData.CompletionTriggerReason.Invoke ||
                trigger.Reason == AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique)
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
                return TryInvokeSnippetCompletion(completionService, document, sourceText, triggerLocation.Position);
            }

            var roslynTrigger = Helpers.GetRoslynTrigger(trigger, triggerLocation);

            // The completion service decides that user may want a completion.
            if (completionService.ShouldTriggerCompletion(sourceText, triggerLocation.Position, roslynTrigger))
            {
                return true;
            }

            return false;
        }

        private bool TryInvokeSnippetCompletion(
            CompletionService completionService, Document document, SourceText text, int caretPoint)
        {
            var rules = completionService.GetRules();
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

        public Task<AsyncCompletionData.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            return GetCompletionContextWorkerAsync(session, trigger, triggerLocation, applicableToSpan, isExpanded: false, cancellationToken);
        }

        public async Task<AsyncCompletionData.CompletionContext> GetExpandedCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionExpander expander,
            AsyncCompletionData.CompletionTrigger intialTrigger,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            // We only want to provide expanded items for Roslyn's expander.
            if ((object)expander == FilterSet.Expander)
            {
                if (Helpers.TryGetInitialTriggerLocation(session, out var initialTriggerLocation))
                {
                    return await GetCompletionContextWorkerAsync(session, intialTrigger, initialTriggerLocation, applicableToSpan, isExpanded: true, cancellationToken).ConfigureAwait(false);
                }
            }

            return AsyncCompletionData.CompletionContext.Empty;
        }

        private async Task<AsyncCompletionData.CompletionContext> GetCompletionContextWorkerAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            bool isExpanded,
            CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return AsyncCompletionData.CompletionContext.Empty;
            }

            var completionService = document.GetLanguageService<CompletionService>();

            var roslynTrigger = Helpers.GetRoslynTrigger(trigger, triggerLocation);
            if (_snippetCompletionTriggeredIndirectly)
            {
                roslynTrigger = new CompletionTrigger(CompletionTriggerKind.Snippets);
            }

            var workspace = document.Project.Solution.Workspace;

            var options = _isDebuggerTextView ? workspace.Options.WithDebuggerCompletionOptions() : workspace.Options;
            options = options.WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, isExpanded);

            var (completionList, expandItemsAvailable) = await completionService.GetCompletionsInternalAsync(
                document,
                triggerLocation,
                roslynTrigger,
                _roles,
                options,
                cancellationToken).ConfigureAwait(false);

            ImmutableArray<VSCompletionItem> items;
            AsyncCompletionData.SuggestionItemOptions suggestionItemOptions;
            var filterSet = new FilterSet();

            if (completionList == null)
            {
                items = ImmutableArray<VSCompletionItem>.Empty;
                suggestionItemOptions = null;
            }
            else
            {
                var itemsBuilder = new ArrayBuilder<VSCompletionItem>(completionList.Items.Length);
                foreach (var roslynItem in completionList.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = Convert(document, roslynItem, filterSet);
                    itemsBuilder.Add(item);
                }

                items = itemsBuilder.ToImmutableAndFree();

                suggestionItemOptions = completionList.SuggestionModeItem != null
                        ? new AsyncCompletionData.SuggestionItemOptions(
                            completionList.SuggestionModeItem.DisplayText,
                            completionList.SuggestionModeItem.Properties.TryGetValue(Description, out var description)
                                ? description
                                : string.Empty)
                        : null;

                // Store around the span this completion list applies to.  We'll use this later
                // to pass this value in when we're committing a completion list item.
                // It's OK to overwrite this value when expanded items are requested.
                session.Properties[CompletionListSpan] = completionList.Span;

                // This is a code supporting original completion scenarios: 
                // Controller.Session_ComputeModel: if completionList.SuggestionModeItem != null, then suggestionMode = true
                // If there are suggestionItemOptions, then later HandleNormalFiltering should set selection to SoftSelection.
                if (!session.Properties.TryGetProperty(HasSuggestionItemOptions, out bool hasSuggestionItemOptionsBefore) || !hasSuggestionItemOptionsBefore)
                {
                    session.Properties[HasSuggestionItemOptions] = suggestionItemOptions != null;
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
            }

            // It's possible that some providers can provide expanded items, in which case we will need to show expander as unselected.
            return new AsyncCompletionData.CompletionContext(
                items,
                suggestionItemOptions,
                suggestionItemOptions == null
                    ? AsyncCompletionData.InitialSelectionHint.RegularSelection
                    : AsyncCompletionData.InitialSelectionHint.SoftSelection,
                filterSet.GetFilterStatesInSet(addUnselectedExpander: expandItemsAvailable));
        }

        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, VSCompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetProperty(RoslynItem, out RoslynCompletionItem roslynItem) ||
                !Helpers.TryGetInitialTriggerLocation(session, out var triggerLocation))
            {
                return null;
            }

            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var service = document.GetLanguageService<CompletionService>();
            if (service == null)
            {
                return null;
            }

            var description = await service.GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            var elements = IntelliSense.Helpers.BuildInteractiveTextElements(description.TaggedParts, document, _streamingPresenter).ToArray();
            if (elements.Length == 0)
            {
                return new ClassifiedTextElement();
            }
            else if (elements.Length == 1)
            {
                return elements[0];
            }
            else
            {
                return new ContainerElement(ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding, elements);
            }
        }

        /// <summary>
        /// We'd like to cache VS Completion item dircetly to avoid allocation completely. However it holds references
        /// to transient objects, which would cause memory leak (among other potential issues) if cached. 
        /// So as a compromise,  we cache data that can be calculated from Roslyn completion item to avoid repeated 
        /// calculation cost for cached Roslyn completion items.
        /// </summary>
        private readonly struct VSCompletionItemData
        {
            public VSCompletionItemData(string displayText, ImageElement icon, ImmutableArray<AsyncCompletionData.CompletionFilter> filters, int filterSetData, ImmutableArray<ImageElement> attributeIcons, string insertionText)
            {
                DisplayText = displayText;
                Icon = icon;
                Filters = filters;
                FilterSetData = filterSetData;
                AttributeIcons = attributeIcons;
                InsertionText = insertionText;
            }

            public string DisplayText { get; }

            public ImageElement Icon { get; }

            public ImmutableArray<AsyncCompletionData.CompletionFilter> Filters { get; }

            /// <summary>
            /// This is the bit vector value from the FilterSet of this item.
            /// </summary>
            public int FilterSetData { get; }

            public ImmutableArray<ImageElement> AttributeIcons { get; }

            public string InsertionText { get; }
        }

        private VSCompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            FilterSet filterSet)
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

                var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
                var attributeImages = supportedPlatforms != null ? s_WarningImageAttributeImagesArray : ImmutableArray<ImageElement>.Empty;

                itemData = new VSCompletionItemData(
                    displayText: roslynItem.GetEntireDisplayText(),
                    icon: new ImageElement(new ImageId(imageId.Guid, imageId.Id), roslynItem.DisplayText),
                    filters: filters,
                    filterSetData: filterSetData,
                    attributeIcons: attributeImages,
                    insertionText: insertionText);

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

            return item;
        }

        private ImmutableArray<char> GetExcludedCommitCharacters(ImmutableArray<RoslynCompletionItem> roslynItems)
        {
            var hashSet = new HashSet<char>();
            foreach (var roslynItem in roslynItems)
            {
                foreach (var rule in roslynItem.Rules?.FilterCharacterRules)
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

        /// <summary>
        /// Provides an efficient way to compute a set of completion filters associated with a collection of completion items.
        /// Presence of expander and filter in the set have different meanings. Set contains a filter means the filter is
        /// available but unselected, whereas it means available and selected for an expander. Note that even though VS supports 
        /// having multiple expanders, we only support one.
        /// </summary>
        internal class FilterSet
        {
            // Cache all the VS completion filters which essentially make them singletons.
            // Because all items that should be filtered using the same filter button must 
            // use the same reference to the instance of CompletionFilter.
            private static readonly Dictionary<string, AsyncCompletionData.CompletionFilter> s_filterCache =
                new Dictionary<string, AsyncCompletionData.CompletionFilter>();

            private BitVector32 _vector;
            private static readonly ImmutableArray<int> s_filterMasks;
            private static readonly int s_expanderMask;
            private static AsyncCompletionData.CompletionExpander _expander = null;

            public static ImmutableArray<CompletionItemFilter> Filters => CompletionItemFilter.AllFilters;

            public static AsyncCompletionData.CompletionExpander Expander
            {
                get
                {
                    if (_expander == null)
                    {
                        var addImageId = Shared.Extensions.GlyphExtensions.GetImageCatalogImageId(KnownImageIds.ExpandScope);
                        _expander = new AsyncCompletionData.CompletionExpander(
                            EditorFeaturesResources.Expander_display_text,
                            accessKey: "a",
                            new ImageElement(addImageId, EditorFeaturesResources.Expander_image_element));
                    }

                    return _expander;
                }
            }

            public int Data => _vector.Data;

            static FilterSet()
            {
                var length = Filters.Length;
                Debug.Assert(length <= 32);

                var previousMask = 0;
                var builder = ArrayBuilder<int>.GetInstance(length);
                for (var i = 0; i < length; ++i)
                {
                    previousMask = BitVector32.CreateMask(previousMask);
                    builder.Add(previousMask);
                }

                s_filterMasks = builder.ToImmutableAndFree();

                s_expanderMask = BitVector32.CreateMask(previousMask);
            }

            public FilterSet(int data = 0)
            {
                _vector = new BitVector32(data);
            }

            public (ImmutableArray<AsyncCompletionData.CompletionFilter> filters, int data) GetFiltersAndAddToSet(RoslynCompletionItem item)
            {
                var listBuilder = new ArrayBuilder<AsyncCompletionData.CompletionFilter>();
                var vectorForSingleItem = new BitVector32();

                if (item.Flags.IsExpanded())
                {
                    listBuilder.Add(Expander);
                    vectorForSingleItem[s_expanderMask] = _vector[s_expanderMask] = true;
                }

                for (var i = 0; i < Filters.Length; ++i)
                {
                    var filter = Filters[i];
                    if (filter.Matches(item))
                    {
                        listBuilder.Add(GetOrCreateFilter(filter));

                        var filterMask = s_filterMasks[i];
                        vectorForSingleItem[filterMask] = _vector[filterMask] = true;
                    }
                }

                return (listBuilder.ToImmutableAndFree(), vectorForSingleItem.Data);
            }

            public void CombineData(int filterSetData)
            {
                _vector[filterSetData] = true;
            }

            public ImmutableArray<AsyncCompletionData.CompletionFilterWithState> GetFilterStatesInSet(bool addUnselectedExpander)
            {
                var listBuilder = new ArrayBuilder<AsyncCompletionData.CompletionFilterWithState>();

                // An unselected expander is only added if `addUnselectedExpander == true` and the expander is not in the set.
                if (_vector[s_expanderMask])
                {
                    listBuilder.Add(new AsyncCompletionData.CompletionFilterWithState(Expander, isAvailable: true, isSelected: true));
                }
                else if (addUnselectedExpander)
                {
                    listBuilder.Add(new AsyncCompletionData.CompletionFilterWithState(Expander, isAvailable: true, isSelected: false));
                }

                for (var i = 0; i < Filters.Length; ++i)
                {
                    if (_vector[s_filterMasks[i]])
                    {
                        var vsFilter = GetOrCreateFilter(Filters[i]);
                        listBuilder.Add(new AsyncCompletionData.CompletionFilterWithState(vsFilter, isAvailable: true, isSelected: false));
                    }
                }

                return listBuilder.ToImmutableAndFree();
            }

            private ImmutableArray<AsyncCompletionData.CompletionFilter> GetFilters(RoslynCompletionItem item)
                => CompletionItemFilter.AllFilters.WhereAsArray(f => f.Matches(item)).SelectAsArray(f => GetOrCreateFilter(f));

            internal static AsyncCompletionData.CompletionFilter GetOrCreateFilter(CompletionItemFilter filter)
            {
                if (!s_filterCache.TryGetValue(filter.DisplayText, out var itemFilter))
                {
                    var imageId = filter.Tags.GetFirstGlyph().GetImageId();
                    itemFilter = new AsyncCompletionData.CompletionFilter(
                        filter.DisplayText,
                        filter.AccessKey.ToString(),
                        new ImageElement(new ImageId(imageId.Guid, imageId.Id), EditorFeaturesResources.Filter_image_element));
                    s_filterCache[filter.DisplayText] = itemFilter;
                }

                return itemFilter;
            }
        }
    }
}
