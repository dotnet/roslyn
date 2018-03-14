// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal abstract class TextViewWindow_InProc : InProcComponent
    {
        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public string[] GetCompletionItems()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ICompletionBroker>();

                var sessions = broker.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
                }

                var selectedCompletionSet = sessions[0].SelectedCompletionSet;

                return selectedCompletionSet.Completions.Select(c => c.DisplayText).ToArray();
            });

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public string GetCurrentCompletionItem()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ICompletionBroker>();

                var sessions = broker.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
                }

                var selectedCompletionSet = sessions[0].SelectedCompletionSet;
                return selectedCompletionSet.SelectionStatus.Completion.DisplayText;
            });

        public void ShowLightBulb()
            => InvokeOnUIThread(() => GetDTE().ExecuteCommand(WellKnownCommandNames.View_ShowSmartTag));

        public void WaitForLightBulbSession()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                LightBulbHelper.WaitForLightBulbSession(broker, view);
            });

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public bool IsCompletionActive()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ICompletionBroker>();
                return broker.IsCompletionActive(view);
            });

        protected abstract ITextBuffer GetBufferContainingCaret(IWpfTextView view);

        public string[] GetCurrentClassifications()
            => InvokeOnUIThread(() =>
            {
                IClassifier classifier = null;
                try
                {
                    var textView = GetActiveTextView();
                    var selectionSpan = textView.Selection.StreamSelectionSpan.SnapshotSpan;
                    if (selectionSpan.Length == 0)
                    {
                        var textStructureNavigatorSelectorService = GetComponentModelService<ITextStructureNavigatorSelectorService>();
                        selectionSpan = textStructureNavigatorSelectorService
                            .GetTextStructureNavigator(textView.TextBuffer)
                            .GetExtentOfWord(selectionSpan.Start).Span;
                    }

                    var classifierAggregatorService = GetComponentModelService<IViewClassifierAggregatorService>();
                    classifier = classifierAggregatorService.GetClassifier(textView);
                    var classifiedSpans = classifier.GetClassificationSpans(selectionSpan);
                    return classifiedSpans.Select(x => x.ClassificationType.Classification).ToArray();
                }
                finally
                {
                    if (classifier is IDisposable classifierDispose)
                    {
                        classifierDispose.Dispose();
                    }
                }
            });

        public void PlaceCaret(
            string marker,
            int charsOffset,
            int occurrence,
            bool extendSelection,
            bool selectBlock)
            => ExecuteOnActiveView(view =>
            {
                var dte = GetDTE();
                dte.Find.FindWhat = marker;
                dte.Find.MatchCase = true;
                dte.Find.MatchInHiddenText = true;
                dte.Find.Target = EnvDTE.vsFindTarget.vsFindTargetCurrentDocument;
                dte.Find.Action = EnvDTE.vsFindAction.vsFindActionFind;

                var originalPosition = GetCaretPosition();
                view.Caret.MoveTo(new SnapshotPoint(GetBufferContainingCaret(view).CurrentSnapshot, 0));

                if (occurrence > 0)
                {
                    var result = EnvDTE.vsFindResult.vsFindResultNotFound;
                    for (var i = 0; i < occurrence; i++)
                    {
                        result = dte.Find.Execute();
                    }

                    if (result != EnvDTE.vsFindResult.vsFindResultFound)
                    {
                        throw new Exception("Occurrence " + occurrence + " of marker '" + marker + "' not found in text: " + view.TextSnapshot.GetText());
                    }
                }
                else
                {
                    var result = dte.Find.Execute();
                    if (result != EnvDTE.vsFindResult.vsFindResultFound)
                    {
                        throw new Exception("Marker '" + marker + "' not found in text: " + view.TextSnapshot.GetText());
                    }
                }

                if (charsOffset > 0)
                {
                    for (var i = 0; i < charsOffset - 1; i++)
                    {
                        view.Caret.MoveToNextCaretPosition();
                    }

                    view.Selection.Clear();
                }

                if (charsOffset < 0)
                {
                    // On the first negative charsOffset, move to anchor-point position, as if the user hit the LEFT key
                    view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, view.Selection.AnchorPoint.Position.Position));

                    for (var i = 0; i < -charsOffset - 1; i++)
                    {
                        view.Caret.MoveToPreviousCaretPosition();
                    }

                    view.Selection.Clear();
                }

                if (extendSelection)
                {
                    var newPosition = view.Selection.ActivePoint.Position.Position;
                    view.Selection.Select(new VirtualSnapshotPoint(view.TextSnapshot, originalPosition), new VirtualSnapshotPoint(view.TextSnapshot, newPosition));
                    view.Selection.Mode = selectBlock ? TextSelectionMode.Box : TextSelectionMode.Stream;
                }
            });

        public int GetCaretPosition()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = GetBufferContainingCaret(view);
                var bufferPosition = view.Caret.Position.BufferPosition;
                return bufferPosition.Position;
            });

        protected T ExecuteOnActiveView<T>(Func<IWpfTextView, T> action)
            => InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                return action(view);
            });

        protected void ExecuteOnActiveView(Action<IWpfTextView> action)
            => InvokeOnUIThread(GetExecuteOnActionViewCallback(action));

        protected Action GetExecuteOnActionViewCallback(Action<IWpfTextView> action)
            => () =>
            {
                var view = GetActiveTextView();
                action(view);
            };

        public string GetQuickInfo()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<IAsyncQuickInfoBroker>();

                var session = broker.GetSession(view);
                return QuickInfoToStringConverter.GetStringFromBulkContent(session.Content);
            });

        public void VerifyTags(string tagTypeName, int expectedCount)
            => ExecuteOnActiveView(view =>
        {
            Type type = WellKnownTagNames.GetTagTypeByName(tagTypeName);
            bool filterTag(IMappingTagSpan<ITag> tag) { return tag.Tag.GetType().Equals(type); }
            var service = GetComponentModelService<IViewTagAggregatorFactoryService>();
            var aggregator = service.CreateTagAggregator<ITag>(view);
            var allTags = aggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length));
            var tags = allTags.Where(filterTag).Cast<IMappingTagSpan<ITag>>();
            var actualCount = tags.Count();

            if (expectedCount != actualCount)
            {
                var tagsTypesString = string.Join(",", allTags.Select(tag => tag.Tag.ToString()));
                throw new Exception($"Failed to verify {tagTypeName} tags. Expected count: {expectedCount}, Actual count: {actualCount}. All tags: {tagsTypesString}");
            }
        });

        public bool IsLightBulbSessionExpanded()
       => ExecuteOnActiveView(view =>
       {
           var broker = GetComponentModel().GetService<ILightBulbBroker>();

           if (!broker.IsLightBulbSessionActive(view))
           {
               return false;
           }

           var session = broker.GetSession(view);
           if (session == null || !session.IsExpanded)
           {
               return false;
           }

           return true;
       });

        public string[] GetLightBulbActions()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                return GetLightBulbActions(broker, view).Select(a => a.DisplayText).ToArray();
            });

        private IEnumerable<ISuggestedAction> GetLightBulbActions(ILightBulbBroker broker, IWpfTextView view)
        {
            if (!broker.IsLightBulbSessionActive(view))
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new Exception(string.Format("No light bulb session in View!  Buffer content type={0}", bufferType));
            }

            var activeSession = broker.GetSession(view);
            if (activeSession == null || !activeSession.IsExpanded)
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new InvalidOperationException(string.Format("No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={0}", bufferType));
            }

            if (activeSession.TryGetSuggestedActionSets(out var actionSets) != QuerySuggestedActionCompletionStatus.Completed)
            {
                actionSets = Array.Empty<SuggestedActionSet>();
            }

            return SelectActions(actionSets);
        }

        public void ApplyLightBulbAction(string actionName, FixAllScope? fixAllScope, bool blockUntilComplete)
        {
            var lightBulbAction = GetLightBulbApplicationAction(actionName, fixAllScope);
            if (blockUntilComplete)
            {
                ExecuteOnActiveView(lightBulbAction);
            }
            else
            {
                BeginInvokeExecuteOnActiveView(lightBulbAction);
            }
        }

        /// <summary>
        /// Non-blocking version of <see cref="ExecuteOnActiveView"/>
        /// </summary>
        private void BeginInvokeExecuteOnActiveView(Action<IWpfTextView> action)
            => BeginInvokeOnUIThread(GetExecuteOnActionViewCallback(action));

        private Action<IWpfTextView> GetLightBulbApplicationAction(string actionName, FixAllScope? fixAllScope)
        {
            return view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();

                var actions = GetLightBulbActions(broker, view).ToArray();
                var action = actions.FirstOrDefault(a => a.DisplayText == actionName);

                if (action == null)
                {
                    var sb = new StringBuilder();
                    foreach (var item in actions)
                    {
                        sb.AppendLine("Actual ISuggestedAction: " + item.DisplayText);
                    }

                    var bufferType = view.TextBuffer.ContentType.DisplayName;
                    throw new InvalidOperationException(
                        string.Format("ISuggestedAction {0} not found.  Buffer content type={1}\r\nActions: {2}", actionName, bufferType, sb.ToString()));
                }

                if (fixAllScope != null)
                {
                    if (!action.HasActionSets)
                    {
                        throw new InvalidOperationException($"Suggested action '{action.DisplayText}' does not support FixAllOccurrences.");
                    }

                    var actionSetsForAction = HostWaitHelper.PumpingWaitResult(action.GetActionSetsAsync(CancellationToken.None));
                    action = GetFixAllSuggestedAction(actionSetsForAction, fixAllScope.Value);
                    if (action == null)
                    {
                        throw new InvalidOperationException($"Unable to find FixAll in {fixAllScope.ToString()} code fix for suggested action '{action.DisplayText}'.");
                    }

                    if (string.IsNullOrEmpty(actionName))
                    {
                        return;
                    }

                    // Dismiss the lightbulb session as we not invoking the original code fix.
                    broker.DismissSession(view);
                }

                action.Invoke(CancellationToken.None);
            };
        }

        private IEnumerable<ISuggestedAction> SelectActions(IEnumerable<SuggestedActionSet> actionSets)
        {
            var actions = new List<ISuggestedAction>();

            if (actionSets != null)
            {
                foreach (var actionSet in actionSets)
                {
                    if (actionSet.Actions != null)
                    {
                        foreach (var action in actionSet.Actions)
                        {
                            actions.Add(action);
                            actions.AddRange(SelectActions(HostWaitHelper.PumpingWaitResult(action.GetActionSetsAsync(CancellationToken.None))));
                        }
                    }
                }
            }

            return actions;
        }

        private static FixAllSuggestedAction GetFixAllSuggestedAction(IEnumerable<SuggestedActionSet> actionSets, FixAllScope fixAllScope)
        {
            foreach (var actionSet in actionSets)
            {
                foreach (var action in actionSet.Actions)
                {
                    if (action is FixAllSuggestedAction fixAllSuggestedAction)
                    {
                        var fixAllCodeAction = fixAllSuggestedAction.CodeAction as FixSomeCodeAction;
                        if (fixAllCodeAction?.FixAllState?.Scope == fixAllScope)
                        {
                            return fixAllSuggestedAction;
                        }
                    }

                    if (action.HasActionSets)
                    {
                        var nestedActionSets = HostWaitHelper.PumpingWaitResult(action.GetActionSetsAsync(CancellationToken.None));
                        fixAllSuggestedAction = GetFixAllSuggestedAction(nestedActionSets, fixAllScope);
                        if (fixAllSuggestedAction != null)
                        {
                            return fixAllSuggestedAction;
                        }
                    }
                }
            }

            return null;
        }

        public void DismissLightBulbSession()   
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                broker.DismissSession(view);
            });

        protected abstract IWpfTextView GetActiveTextView();
    }
}
