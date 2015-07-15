// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Extensibility.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void ComputeModel(
                ICompletionService completionService,
                CompletionTriggerInfo triggerInfo,
                IEnumerable<ICompletionProvider> completionProviders,
                bool isDebugger)
            {
                AssertIsForeground();

                // If we've already computed a model then we can just ignore this request and not
                // generate any tasks.
                if (this.Computation.InitialUnfilteredModel != null)
                {
                    return;
                }

                new ModelComputer(this, completionService, triggerInfo, completionProviders, isDebugger).Do();
            }

            private class ModelComputer : ForegroundThreadAffinitizedObject
            {
                private static readonly Func<string, List<CompletionItem>> s_createList = _ => new List<CompletionItem>();

                private readonly Session _session;
                private readonly ICompletionService _completionService;
                private readonly OptionSet _options;
                private readonly CompletionTriggerInfo _triggerInfo;
                private readonly SnapshotPoint _subjectBufferCaretPosition;
                private readonly SourceText _text;
                private readonly IEnumerable<ICompletionProvider> _completionProviders;
                private readonly Dictionary<string, List<CompletionItem>> _displayNameToItemsMap = new Dictionary<string, List<CompletionItem>>();

                private Document _documentOpt;
                private bool _includeBuilder;
                private CompletionItem _builder;
                private readonly DisconnectedBufferGraph _disconnectedBufferGraph;

                public ModelComputer(
                    Session session,
                    ICompletionService completionService,
                    CompletionTriggerInfo triggerInfo,
                    IEnumerable<ICompletionProvider> completionProviders,
                    bool isDebugger)
                {
                    _session = session;
                    _completionService = completionService;
                    _options = session.Controller.SubjectBuffer.TryGetOptions();
                    _triggerInfo = triggerInfo;
                    _subjectBufferCaretPosition = session.Controller.TextView.GetCaretPoint(session.Controller.SubjectBuffer).Value;
                    _completionProviders = completionProviders;

                    _text = _subjectBufferCaretPosition.Snapshot.AsText();

                    _includeBuilder = session.Controller.SubjectBuffer.GetOption(Options.EditorCompletionOptions.UseSuggestionMode);

                    _disconnectedBufferGraph = new DisconnectedBufferGraph(session.Controller.SubjectBuffer, session.Controller.TextView.TextBuffer);
                }

                public void Do()
                {
                    AssertIsForeground();
                    _session.Computation.ChainTaskAndNotifyControllerWhenFinished(
                        (model, cancellationToken) => model != null ? Task.FromResult(model) : DoInBackgroundAsync(cancellationToken));
                }

                private async Task<Model> DoInBackgroundAsync(CancellationToken cancellationToken)
                {
                    using (Logger.LogBlock(FunctionId.Completion_ModelComputer_DoInBackground, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (_completionService == null || _options == null)
                        {
                            // both completionService and options can be null if given buffer is not registered to workspace yet.
                            // could happen in razor more frequently
                            return null;
                        }

                        // get partial solution from background thread.
                        _documentOpt = await _text.GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken).ConfigureAwait(false);

                        // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                        // to the extension crashing.
                        var groups = await GetGroupsAsync(_completionService, _triggerInfo, cancellationToken).ConfigureAwait(false);
                        if (groups == null)
                        {
                            return null;
                        }

                        groups.Do(AddGroupToMap);
                        if (_displayNameToItemsMap.Count == 0)
                        {
                            return null;
                        }

                        var totalItems = _displayNameToItemsMap.Values.Flatten().ToList();
                        totalItems.Sort();

                        var trackingSpan = await _completionService.GetDefaultTrackingSpanAsync(_documentOpt, _subjectBufferCaretPosition, cancellationToken).ConfigureAwait(false);
                        return Model.CreateModel(
                            _disconnectedBufferGraph,
                            trackingSpan,
                            totalItems,
                            selectedItem: totalItems.First(),
                            isHardSelection: false,
                            isUnique: false,
                            useSuggestionCompletionMode: _includeBuilder,
                            builder: _builder,
                            triggerInfo: _triggerInfo,
                            completionService: _completionService,
                            workspace: _documentOpt != null ? _documentOpt.Project.Solution.Workspace : null);
                    }
                }

                private async Task<IEnumerable<CompletionItemGroup>> GetGroupsAsync(ICompletionService completionService, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
                {
                    if (_documentOpt == null && completionService is ITextCompletionService)
                    {
                        var textCompletionService = (ITextCompletionService)completionService;
                        return await textCompletionService.GetGroupsAsync(_text, _subjectBufferCaretPosition, triggerInfo, _completionProviders, _options, cancellationToken).ConfigureAwait(false);
                    }
                    else if (_documentOpt != null)
                    {
                        return await completionService.GetGroupsAsync(_documentOpt, _subjectBufferCaretPosition, triggerInfo, _completionProviders, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return null;
                    }
                }

                private void AddGroupToMap(CompletionItemGroup group)
                {
                    if (group != null)
                    {
                        foreach (var item in group.Items.WhereNotNull())
                        {
                            // New items that match an existing item will replace it.  
                            ReplaceExistingItem(item);
                        }

                        _builder = _builder ?? group.Builder;
                    }
                }

                private void ReplaceExistingItem(
                    CompletionItem item)
                {
                    // See if we have an item with 
                    var sameNamedItems = _displayNameToItemsMap.GetOrAdd(item.DisplayText, s_createList);
                    for (int i = 0; i < sameNamedItems.Count; i++)
                    {
                        var existingItem = sameNamedItems[i];

                        if (ItemsMatch(item, existingItem))
                        {
                            sameNamedItems[i] = Disambiguate(item, existingItem);
                            return;
                        }
                    }

                    sameNamedItems.Add(item);
                }

                private CompletionItem Disambiguate(CompletionItem item, CompletionItem existingItem)
                {
                    // We've constructed the export order of completion providers so 
                    // that snippets are exported after everything else. That way,
                    // when we choose a single item per display text, snippet 
                    // glyphs appear by snippets. This breaks preselection of items
                    // whose display text is also a snippet (workitem 852578),
                    // the snippet item doesn't have its preselect bit set.
                    // We'll special case this by not preferring later items
                    // if they are snippets and the other candidate is preselected.
                    if (existingItem.Preselect && item.CompletionProvider is ISnippetCompletionProvider)
                    {
                        return existingItem;
                    }

                    // If one is a keyword, and the other is some other item that inserts the same text as the keyword,
                    // keep the keyword
                    var keywordItem = existingItem as KeywordCompletionItem ?? item as KeywordCompletionItem;
                    if (keywordItem != null)
                    {
                        return keywordItem;
                    }

                    return item;
                }

                private bool ItemsMatch(CompletionItem item1, CompletionItem item2)
                {
                    Contract.Assert(item1.DisplayText == item2.DisplayText);

                    return _session._completionRules.ItemsMatch(item1, item2).Value;
                }
            }
        }
    }
}
