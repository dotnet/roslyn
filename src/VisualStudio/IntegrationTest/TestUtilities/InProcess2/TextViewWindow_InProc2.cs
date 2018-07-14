// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;
using QuickInfoToStringConverter = Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess.QuickInfoToStringConverter;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public abstract partial class TextViewWindow_InProc2 : InProcComponent2
    {
        protected TextViewWindow_InProc2(TestServices testServices)
            : base(testServices)
        {
            Verify = new Verifier<TextViewWindow_InProc2>(this);
        }

        public Verifier<TextViewWindow_InProc2> Verify
        {
            get;
        }

        protected VisualStudioWorkspace_InProc2 Workspace => TestServices.Workspace;

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public async Task<string[]> GetCompletionItemsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForCompletionSetAsync();

            var view = await GetActiveTextViewAsync();
            var broker = await GetComponentModelServiceAsync<ICompletionBroker>();

            var sessions = broker.GetSessions(view);
            if (sessions.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
            }

            var selectedCompletionSet = sessions[0].SelectedCompletionSet;

            return selectedCompletionSet.Completions.Select(c => c.DisplayText).ToArray();
        }

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public async Task<string> GetCurrentCompletionItemAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForCompletionSetAsync();

            var view = await GetActiveTextViewAsync();
            var broker = await GetComponentModelServiceAsync<ICompletionBroker>();

            var sessions = broker.GetSessions(view);
            if (sessions.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
            }

            var selectedCompletionSet = sessions[0].SelectedCompletionSet;
            return selectedCompletionSet.SelectionStatus.Completion.DisplayText;
        }

        public async Task ShowLightBulbAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = await GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();
            var cmdGroup = typeof(VSConstants.VSStd2KCmdID).GUID;
            var cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;

            const VSConstants.VSStd2KCmdID ECMD_SMARTTASKS = (VSConstants.VSStd2KCmdID)147;
            var cmdID = ECMD_SMARTTASKS;
            object obj = null;
            shell.PostExecCommand(cmdGroup, (uint)cmdID, (uint)cmdExecOpt, ref obj);
        }

        public async Task WaitForLightBulbSessionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var broker = (await GetComponentModelAsync()).GetService<ILightBulbBroker>();
            await LightBulbHelper.WaitForLightBulbSessionAsync(broker, view, cancellationToken);
        }

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public async Task<bool> IsCompletionActiveAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForCompletionSetAsync();

            var view = await GetActiveTextViewAsync();
            var broker = await GetComponentModelServiceAsync<ICompletionBroker>();
            return broker.IsCompletionActive(view);
        }

        protected abstract Task<ITextBuffer> GetBufferContainingCaretAsync(IWpfTextView view);

        public async Task<string[]> GetCurrentClassificationsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            IClassifier classifier = null;
            try
            {
                var textView = await GetActiveTextViewAsync();
                var selectionSpan = textView.Selection.StreamSelectionSpan.SnapshotSpan;
                if (selectionSpan.Length == 0)
                {
                    var textStructureNavigatorSelectorService = await GetComponentModelServiceAsync<ITextStructureNavigatorSelectorService>();
                    selectionSpan = textStructureNavigatorSelectorService
                        .GetTextStructureNavigator(textView.TextBuffer)
                        .GetExtentOfWord(selectionSpan.Start).Span;
                }

                var classifierAggregatorService = await GetComponentModelServiceAsync<IViewClassifierAggregatorService>();
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
        }

        public async Task PlaceCaretAsync(
            string marker,
            int charsOffset = 0,
            int occurrence = 0,
            bool extendSelection = false,
            bool selectBlock = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var dte = await GetDTEAsync();
            dte.Find.FindWhat = marker;
            dte.Find.MatchCase = true;
            dte.Find.MatchInHiddenText = true;
            dte.Find.Target = EnvDTE.vsFindTarget.vsFindTargetCurrentDocument;
            dte.Find.Action = EnvDTE.vsFindAction.vsFindActionFind;

            var originalPosition = await GetCaretPositionAsync();
            view.Caret.MoveTo(new SnapshotPoint((await GetBufferContainingCaretAsync(view)).CurrentSnapshot, 0));

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
        }

        public async Task<int> GetCaretPositionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var subjectBuffer = await GetBufferContainingCaretAsync(view);
            var bufferPosition = view.Caret.Position.BufferPosition;
            return bufferPosition.Position;
        }

        public async Task<string> GetQuickInfoAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForQuickInfoAsync();

            var view = await GetActiveTextViewAsync();
#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
            var broker = await GetComponentModelServiceAsync<IQuickInfoBroker>();
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094

            var sessions = broker.GetSessions(view);
            if (sessions.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly one QuickInfo session, but found {sessions.Count}");
            }

            return QuickInfoToStringConverter.GetStringFromBulkContent(sessions[0].QuickInfoContent);
        }

        public async Task VerifyTagsAsyn(string tagTypeName, int expectedCount)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var type = WellKnownTagNames.GetTagTypeByName(tagTypeName);
            bool filterTag(IMappingTagSpan<ITag> tag)
            {
                return tag.Tag.GetType().Equals(type);
            }
            var service = await GetComponentModelServiceAsync<IViewTagAggregatorFactoryService>();
            var aggregator = service.CreateTagAggregator<ITag>(view);
            var allTags = aggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length));
            var tags = allTags.Where(filterTag).Cast<IMappingTagSpan<ITag>>();
            var actualCount = tags.Count();

            if (expectedCount != actualCount)
            {
                var tagsTypesString = string.Join(",", allTags.Select(tag => tag.Tag.ToString()));
                throw new Exception($"Failed to verify {tagTypeName} tags. Expected count: {expectedCount}, Actual count: {actualCount}. All tags: {tagsTypesString}");
            }
        }

        public async Task<bool> IsLightBulbSessionExpandedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var broker = (await GetComponentModelAsync()).GetService<ILightBulbBroker>();
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
        }

        public async Task<string[]> GetLightBulbActionsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var broker = (await GetComponentModelAsync()).GetService<ILightBulbBroker>();
            return (await GetLightBulbActionsAsync(broker, view)).Select(a => a.DisplayText).ToArray();
        }

        private async Task<IEnumerable<ISuggestedAction>> GetLightBulbActionsAsync(ILightBulbBroker broker, IWpfTextView view)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

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

            return await SelectActionsAsync(actionSets);
        }

        public async Task ApplyLightBulbActionAsync(string actionName, FixAllScope? fixAllScope, bool willBlockUntilComplete)
        {
            var lightBulbAction = GetLightBulbApplicationAction(actionName, fixAllScope, willBlockUntilComplete);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var activeTextView = await GetActiveTextViewAsync();
            await lightBulbAction(activeTextView);
        }

        private Func<IWpfTextView, Task> GetLightBulbApplicationAction(string actionName, FixAllScope? fixAllScope, bool willBlockUntilComplete)
        {
            return async view =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                var broker = (await GetComponentModelAsync()).GetService<ILightBulbBroker>();

                var actions = (await GetLightBulbActionsAsync(broker, view)).ToArray();
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

                    var actionSetsForAction = await action.GetActionSetsAsync(CancellationToken.None);
                    action = await GetFixAllSuggestedActionAsync(actionSetsForAction, fixAllScope.Value);
                    if (action == null)
                    {
                        throw new InvalidOperationException($"Unable to find FixAll in {fixAllScope.ToString()} code fix for suggested action '{action.DisplayText}'.");
                    }

                    if (willBlockUntilComplete
                        && action is FixAllSuggestedAction fixAllSuggestedAction
                        && fixAllSuggestedAction.CodeAction is FixSomeCodeAction fixSomeCodeAction)
                    {
                        // Ensure the preview changes dialog will not be shown. Since the operation 'willBlockUntilComplete',
                        // the caller would not be able to interact with the preview changes dialog, and the tests would
                        // either timeout or deadlock.
                        fixSomeCodeAction.GetTestAccessor().ShowPreviewChangesDialog = false;
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

        private async Task<IEnumerable<ISuggestedAction>> SelectActionsAsync(IEnumerable<SuggestedActionSet> actionSets)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

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
                            var nestedActionSets = await action.GetActionSetsAsync(CancellationToken.None);
                            var nestedActions = await SelectActionsAsync(nestedActionSets);
                            actions.AddRange(nestedActions);
                        }
                    }
                }
            }

            return actions;
        }

        private async Task<FixAllSuggestedAction> GetFixAllSuggestedActionAsync(IEnumerable<SuggestedActionSet> actionSets, FixAllScope fixAllScope)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

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
                        var nestedActionSets = await action.GetActionSetsAsync(CancellationToken.None);
                        fixAllSuggestedAction = await GetFixAllSuggestedActionAsync(nestedActionSets, fixAllScope);
                        if (fixAllSuggestedAction != null)
                        {
                            return fixAllSuggestedAction;
                        }
                    }
                }
            }

            return null;
        }

        public async Task DismissLightBulbSessionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>();
            broker.DismissSession(view);
        }

        protected abstract Task<IWpfTextView> GetActiveTextViewAsync();

        public async Task InvokeCompletionListAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_ListMembers);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet);
        }

        public async Task InvokeCodeActionListAsync(CancellationToken cancellationToken)
        {
            await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler);
            await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.DiagnosticService);

            await ShowLightBulbAsync();
            await WaitForLightBulbSessionAsync(cancellationToken);
            await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Invokes the light bulb without waiting for diagnostics
        /// Compare to <see cref="InvokeCodeActionListAsync"/>
        /// </summary>
        public async Task InvokeCodeActionListWithoutWaitingAsync(CancellationToken cancellationToken)
        {
            await ShowLightBulbAsync();
            await WaitForLightBulbSessionAsync(cancellationToken);
        }

        public async Task InvokeQuickInfoAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_QuickInfo);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.QuickInfo);
        }
    }
}
