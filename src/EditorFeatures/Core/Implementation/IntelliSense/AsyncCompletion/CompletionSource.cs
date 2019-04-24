﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
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
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal class CompletionSource : ForegroundThreadAffinitizedObject, IAsyncCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string TriggerSnapshot = nameof(TriggerSnapshot);
        internal const string CompletionListSpan = nameof(CompletionListSpan);
        internal const string InsertionText = nameof(InsertionText);
        internal const string HasSuggestionItemOptions = nameof(HasSuggestionItemOptions);
        internal const string Description = nameof(Description);
        internal const string InitialTriggerKind = nameof(InitialTriggerKind);
        internal const string PotentialCommitCharacters = nameof(PotentialCommitCharacters);
        internal const string ExcludedCommitCharacters = nameof(ExcludedCommitCharacters);
        internal const string NonBlockingCompletion = nameof(NonBlockingCompletion);
        internal const string TypeImportCompletionEnabled = nameof(TypeImportCompletionEnabled);
        internal const string TargetTypeFilterExperimentEnabled = nameof(TargetTypeFilterExperimentEnabled);

        private static readonly ImmutableArray<ImageElement> s_WarningImageAttributeImagesArray =
            ImmutableArray.Create(new ImageElement(Glyph.CompletionWarning.GetImageId(), EditorFeaturesResources.Warning_image_element));

        private static readonly EditorOptionKey<bool> NonBlockingCompletionEditorOption = new EditorOptionKey<bool>(NonBlockingCompletion);

        private static readonly ConditionalWeakTable<RoslynCompletionItem, VSCompletionItem> s_roslynItemToVsItem =
            new ConditionalWeakTable<RoslynCompletionItem, VSCompletionItem>();

        // Cache all the VS completion filters which essentially make them singletons.
        // Because all items that should be filtered using the same filter button must 
        // use the same reference to the instance of CompletionFilter.
        private static readonly Dictionary<string, AsyncCompletionData.CompletionFilter> s_filterCache =
            new Dictionary<string, AsyncCompletionData.CompletionFilter>();

        private readonly ITextView _textView;
        private readonly bool _isDebuggerTextView;
        private readonly ImmutableHashSet<string> _roles;

        internal CompletionSource(ITextView textView, IThreadingContext threadingContext) : base(threadingContext)
        {
            _textView = textView;
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

            if (!document.Project.Solution.Workspace.Options.GetOption(CompletionOptions.BlockForCompletionItems, service.Language))
            {
                _textView.Options.GlobalOptions.SetOptionValue(NonBlockingCompletionEditorOption, true);
            }

            // In case of calls with multiple completion services for the same view (e.g. TypeScript and C#), those completion services must not be called simultaneously for the same session.
            // Therefore, in each completion session we use a list of commit character for a specific completion service and a specific content type.
            _textView.Properties[PotentialCommitCharacters] = service.GetRules().DefaultCommitCharacters;

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

        private static bool TryInvokeSnippetCompletion(
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

            return true;
        }

        public async Task<AsyncCompletionData.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new AsyncCompletionData.CompletionContext(ImmutableArray<VSCompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();

            var roslynTrigger = Helpers.GetRoslynTrigger(trigger, triggerLocation);

            var workspace = document.Project.Solution.Workspace;

            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                roslynTrigger,
                _roles,
                _isDebuggerTextView ? workspace.Options.WithDebuggerCompletionOptions() : workspace.Options,
                cancellationToken).ConfigureAwait(false);

            if (completionList == null)
            {
                return new AsyncCompletionData.CompletionContext(ImmutableArray<VSCompletionItem>.Empty);
            }

            var itemsBuilder = new ArrayBuilder<VSCompletionItem>(completionList.Items.Length);
            foreach (var roslynItem in completionList.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = Convert(document, roslynItem);
                itemsBuilder.Add(item);
            }

            var items = itemsBuilder.ToImmutableAndFree();

            var suggestionItemOptions = completionList.SuggestionModeItem != null
                    ? new AsyncCompletionData.SuggestionItemOptions(
                        completionList.SuggestionModeItem.DisplayText,
                        completionList.SuggestionModeItem.Properties.TryGetValue(Description, out var description)
                            ? description
                            : string.Empty)
                    : null;

            // Have to store the snapshot to reuse it in some projections related scenarios
            // where data and session in further calls are able to provide other snapshots.
            session.Properties.AddProperty(TriggerSnapshot, triggerLocation.Snapshot);

            // Store around the span this completion list applies to.  We'll use this later
            // to pass this value in when we're committing a completion list item.
            session.Properties.AddProperty(CompletionListSpan, completionList.Span);

            // This is a code supporting original completion scenarios: 
            // Controller.Session_ComputeModel: if completionList.SuggestionModeItem != null, then suggestionMode = true
            // If there are suggestionItemOptions, then later HandleNormalFiltering should set selection to SoftSelection.
            session.Properties.AddProperty(HasSuggestionItemOptions, suggestionItemOptions != null);

            session.Properties.AddProperty(InitialTriggerKind, roslynTrigger.Kind);
            var excludedCommitCharacters = GetExcludedCommitCharacters(completionList.Items);
            if (excludedCommitCharacters.Length > 0)
            {
                session.Properties.AddProperty(ExcludedCommitCharacters, excludedCommitCharacters);
            }

            return new AsyncCompletionData.CompletionContext(
                items,
                suggestionItemOptions,
                suggestionItemOptions == null
                    ? AsyncCompletionData.InitialSelectionHint.RegularSelection
                    : AsyncCompletionData.InitialSelectionHint.SoftSelection);
        }

        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, VSCompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetProperty(RoslynItem, out RoslynCompletionItem roslynItem) ||
                !session.Properties.TryGetProperty(TriggerSnapshot, out ITextSnapshot triggerSnapshot))
            {
                return null;
            }

            var document = triggerSnapshot.GetOpenDocumentInCurrentContextWithChanges();
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

            var elements = IntelliSense.Helpers.BuildClassifiedTextElements(description.TaggedParts).ToArray();
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

        private VSCompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem)
        {
            if (roslynItem.IsCached && s_roslynItemToVsItem.TryGetValue(roslynItem, out var vsItem))
            {
                return vsItem;
            }

            var imageId = roslynItem.Tags.GetFirstGlyph().GetImageId();
            var filters = GetFilters(roslynItem);

            // roslynItem generated by providers can contain an insertionText in a property bag.
            // We will not use it but other providers may need it.
            // We actually will calculate the insertion text once again when called TryCommit.
            if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            var attributeImages = supportedPlatforms != null ? s_WarningImageAttributeImagesArray : ImmutableArray<ImageElement>.Empty;

            var item = new VSCompletionItem(
                displayText: roslynItem.GetEntireDisplayText(),
                source: this,
                icon: new ImageElement(new ImageId(imageId.Guid, imageId.Id), roslynItem.DisplayText),
                filters: filters,
                suffix: roslynItem.InlineDescription, // InlineDescription will be right-aligned in the selection popup
                insertText: insertionText,
                sortText: roslynItem.SortText,
                filterText: roslynItem.FilterText,
                attributeIcons: attributeImages);

            item.Properties.AddProperty(RoslynItem, roslynItem);

            // It doesn't make sense to cache VS item for those Roslyn items created from scratch for each session,
            // since CWT uses object identity for comparison.
            if (roslynItem.IsCached)
            {
                s_roslynItemToVsItem.Add(roslynItem, item);
            }

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

        private ImmutableArray<AsyncCompletionData.CompletionFilter> GetFilters(RoslynCompletionItem item)
        {
            var listBuilder = new ArrayBuilder<AsyncCompletionData.CompletionFilter>();
            foreach (var filter in CompletionItemFilter.AllFilters)
            {
                if (filter.Matches(item))
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

                    listBuilder.Add(itemFilter);
                }
            }

            return listBuilder.ToImmutableAndFree();
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
