// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;
using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;
using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class EditorInProcess
    {
        public async Task SetTextAsync(string text, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var textSnapshot = view.TextSnapshot;
            var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
            view.TextBuffer.Replace(replacementSpan, text);
        }

        public async Task MoveCaretAsync(int position, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var subjectBuffer = view.GetBufferContainingCaret();
            Assumes.Present(subjectBuffer);

            var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

            view.Caret.MoveTo(point);
        }

        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            dte.ActiveDocument.Activate();
        }

        public async Task<bool> IsUseSuggestionModeOnAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var textView = await GetActiveTextViewAsync(cancellationToken);
            return await IsUseSuggestionModeOnAsync(forDebuggerTextView: IsDebuggerTextView(textView), cancellationToken);
        }

        public async Task<bool> IsUseSuggestionModeOnAsync(bool forDebuggerTextView, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
            var options = editorOptionsFactory.GlobalOptions;

            EditorOptionKey<bool> optionKey;
            bool defaultOption;
            if (forDebuggerTextView)
            {
                optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInDebuggerCompletionOptionName);
                defaultOption = true;
            }
            else
            {
                optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInCompletionOptionName);
                defaultOption = false;
            }

            if (!options.IsOptionDefined(optionKey, localScopeOnly: false))
            {
                return defaultOption;
            }

            return options.GetOptionValue(optionKey);
        }

        public async Task SetUseSuggestionModeAsync(bool value, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var textView = await GetActiveTextViewAsync(cancellationToken);
            await SetUseSuggestionModeAsync(forDebuggerTextView: IsDebuggerTextView(textView), value, cancellationToken);
        }

        public async Task SetUseSuggestionModeAsync(bool forDebuggerTextView, bool value, CancellationToken cancellationToken)
        {
            if (await IsUseSuggestionModeOnAsync(forDebuggerTextView, cancellationToken) != value)
            {
                var dispatcher = await GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
                ErrorHandler.ThrowOnFailure(dispatcher.Exec(typeof(VSConstants.VSStd2KCmdID).GUID, (uint)VSConstants.VSStd2KCmdID.ToggleConsumeFirstCompletionMode, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero));

                if (await IsUseSuggestionModeOnAsync(forDebuggerTextView, cancellationToken) != value)
                {
                    throw new InvalidOperationException($"{WellKnownCommandNames.Edit_ToggleCompletionMode} did not leave the editor in the expected state.");
                }
            }
        }

        private static bool IsDebuggerTextView(ITextView textView)
            => textView.Roles.Contains("DEBUGVIEW");

        #region Navigation bars

        public async Task ExpandNavigationBarAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(combobox), combobox);
            combobox.IsDropDownOpen = true;
        }

        public async Task<ImmutableArray<string>> GetNavigationBarItemsAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
            return combobox.Items.OfType<object>().SelectAsArray(i => $"{i}");
        }

        public async Task<string?> GetNavigationBarSelectionAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
            return combobox.SelectedItem?.ToString();
        }

        public async Task SelectNavigationBarItemAsync(NavigationBarDropdownKind index, string item, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var itemIndex = await GetNavigationBarItemIndexAsync(index, item, cancellationToken);
            if (itemIndex < 0)
            {
                Assert.Contains(item, await GetNavigationBarItemsAsync(index, cancellationToken));
                throw ExceptionUtilities.Unreachable;
            }

            await ExpandNavigationBarAsync(index, cancellationToken);
            await TestServices.Input.SendAsync(VirtualKey.Home);
            for (var i = 0; i < itemIndex; i++)
            {
                await TestServices.Input.SendAsync(VirtualKey.Down);
            }

            await TestServices.Input.SendAsync(VirtualKey.Enter);

            // Navigation and/or code generation following selection is tracked under FeatureAttribute.NavigationBar
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);
        }

        public async Task<int> GetNavigationBarItemIndexAsync(NavigationBarDropdownKind index, string item, CancellationToken cancellationToken)
        {
            var items = await GetNavigationBarItemsAsync(index, cancellationToken);
            return items.IndexOf(item);
        }

        public async Task<bool> IsNavigationBarEnabledAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);
            return (await GetNavigationBarMarginAsync(view, cancellationToken)) is not null;
        }

        private async Task<List<ComboBox>> GetNavigationBarComboBoxesAsync(IWpfTextView textView, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var margin = await GetNavigationBarMarginAsync(textView, cancellationToken);
            return margin.GetFieldValue<List<ComboBox>>("_combos");
        }

        private async Task<UIElement?> GetNavigationBarMarginAsync(IWpfTextView textView, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var editorAdaptersFactoryService = await GetComponentModelServiceAsync<IVsEditorAdaptersFactoryService>(cancellationToken);
            var viewAdapter = editorAdaptersFactoryService.GetViewAdapter(textView);
            Assumes.Present(viewAdapter);

            // Make sure we have the top pane
            //
            // The docs are wrong. When a secondary view exists, it is the secondary view which is on top. The primary
            // view is only on top when there is no secondary view.
            var codeWindow = TryGetCodeWindow(viewAdapter);
            Assumes.Present(codeWindow);

            if (ErrorHandler.Succeeded(codeWindow.GetSecondaryView(out var secondaryViewAdapter)))
            {
                viewAdapter = secondaryViewAdapter;
            }

            var textViewHost = editorAdaptersFactoryService.GetWpfTextViewHost(viewAdapter);
            Assumes.Present(textViewHost);

            var dropDownMargin = textViewHost.GetTextViewMargin("DropDownMargin");
            if (dropDownMargin != null)
            {
                return ((Decorator)dropDownMargin.VisualElement).Child;
            }

            return null;

            static IVsCodeWindow? TryGetCodeWindow(IVsTextView textView)
            {
                if (textView is not IObjectWithSite objectWithSite)
                {
                    return null;
                }

                var riid = typeof(IOleServiceProvider).GUID;
                objectWithSite.GetSite(ref riid, out var ppvSite);
                if (ppvSite == IntPtr.Zero)
                {
                    return null;
                }

                IOleServiceProvider? oleServiceProvider = null;
                try
                {
                    oleServiceProvider = Marshal.GetObjectForIUnknown(ppvSite) as IOleServiceProvider;
                }
                finally
                {
                    Marshal.Release(ppvSite);
                }

                if (oleServiceProvider == null)
                {
                    return null;
                }

                var guidService = typeof(SVsWindowFrame).GUID;
                riid = typeof(IVsWindowFrame).GUID;
                if (ErrorHandler.Failed(oleServiceProvider.QueryService(ref guidService, ref riid, out var ppvObject)) || ppvObject == IntPtr.Zero)
                {
                    return null;
                }

                IVsWindowFrame? frame = null;
                try
                {
                    frame = (IVsWindowFrame)Marshal.GetObjectForIUnknown(ppvObject);
                }
                finally
                {
                    Marshal.Release(ppvObject);
                }

                riid = typeof(IVsCodeWindow).GUID;
                if (ErrorHandler.Failed(frame.QueryViewInterface(ref riid, out ppvObject)) || ppvObject == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    return Marshal.GetObjectForIUnknown(ppvObject) as IVsCodeWindow;
                }
                finally
                {
                    Marshal.Release(ppvObject);
                }
            }
        }

        #endregion

        public async Task DismissLightBulbSessionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
            broker.DismissSession(view);
        }

        public async Task DismissCompletionSessionsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await WaitForCompletionSetAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var broker = await GetComponentModelServiceAsync<ICompletionBroker>(cancellationToken);
            broker.DismissAllSessions(view);
        }

        public async Task ShowLightBulbAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
            var cmdGroup = typeof(VSConstants.VSStd14CmdID).GUID;
            var cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;

            var cmdID = VSConstants.VSStd14CmdID.ShowQuickFixes;
            object? obj = null;
            shell.PostExecCommand(cmdGroup, (uint)cmdID, (uint)cmdExecOpt, ref obj);

            var view = await GetActiveTextViewAsync(cancellationToken);
            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
            await LightBulbHelper.WaitForLightBulbSessionAsync(broker, view, cancellationToken);
        }

        public async Task InvokeCodeActionListAsync(CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler, cancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.DiagnosticService, cancellationToken);

            if (Version.Parse("17.1.31916.450") > await TestServices.Shell.GetVersionAsync(cancellationToken))
            {
                // Workaround for extremely unstable async lightbulb prior to:
                // https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform/pullrequest/361759
                await TestServices.Input.SendAsync(new KeyPress(VirtualKey.Period, ShiftState.Ctrl));
                await Task.Delay(5000, cancellationToken);

                await TestServices.Editor.DismissLightBulbSessionAsync(cancellationToken);
                await Task.Delay(5000, cancellationToken);
            }

            await ShowLightBulbAsync(cancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
        }

        public async Task<bool> IsLightBulbSessionExpandedAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
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

        public async Task<string[]> GetLightBulbActionsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
            return (await GetLightBulbActionsAsync(broker, view, cancellationToken)).Select(a => a.DisplayText).ToArray();
        }

        public async Task<bool> ApplyLightBulbActionAsync(string actionName, FixAllScope? fixAllScope, bool blockUntilComplete, CancellationToken cancellationToken)
        {
            var lightBulbAction = GetLightBulbApplicationAction(actionName, fixAllScope, blockUntilComplete);

            var listenerProvider = await GetComponentModelServiceAsync<IAsynchronousOperationListenerProvider>(cancellationToken);
            var listener = listenerProvider.GetListener(FeatureAttribute.LightBulb);

            var task = JoinableTaskFactory.RunAsync(async () =>
            {
                using var _ = listener.BeginAsyncOperation(nameof(ApplyLightBulbActionAsync));

                await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

                var activeTextView = await GetActiveTextViewAsync(cancellationToken);
                return await lightBulbAction(activeTextView, cancellationToken);
            });

            if (blockUntilComplete)
            {
                var result = await task.JoinAsync(cancellationToken);
                await DismissLightBulbSessionAsync(cancellationToken);
                return result;
            }

            return true;
        }

        private Func<IWpfTextView, CancellationToken, Task<bool>> GetLightBulbApplicationAction(string actionName, FixAllScope? fixAllScope, bool willBlockUntilComplete)
        {
            return async (view, cancellationToken) =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);

                var actions = (await GetLightBulbActionsAsync(broker, view, cancellationToken)).ToArray();
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
                        $"ISuggestedAction {actionName} not found.  Buffer content type={bufferType}\r\nActions: {sb}");
                }

                if (fixAllScope != null)
                {
                    if (!action.HasActionSets)
                    {
                        throw new InvalidOperationException($"Suggested action '{action.DisplayText}' does not support FixAllOccurrences.");
                    }

                    var actionSetsForAction = await action.GetActionSetsAsync(cancellationToken);
                    var fixAllAction = await GetFixAllSuggestedActionAsync(actionSetsForAction, fixAllScope.Value, cancellationToken);
                    if (fixAllAction == null)
                    {
                        throw new InvalidOperationException($"Unable to find FixAll in {fixAllScope} code fix for suggested action '{action.DisplayText}'.");
                    }

                    action = fixAllAction;

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
                        return false;
                    }

                    // Dismiss the lightbulb session as we not invoking the original code fix.
                    broker.DismissSession(view);
                }

                if (action is not SuggestedAction suggestedAction)
                    return true;

                broker.DismissSession(view);
                var threadOperationExecutor = await GetComponentModelServiceAsync<IUIThreadOperationExecutor>(cancellationToken);
                var guardedOperations = await GetComponentModelServiceAsync<IGuardedOperations2>(cancellationToken);
                threadOperationExecutor.Execute(
                    title: "Execute Suggested Action",
                    defaultDescription: Accelerator.StripAccelerators(action.DisplayText, '_'),
                    allowCancellation: true,
                    showProgress: true,
                    action: context =>
                    {
                        guardedOperations.CallExtensionPoint(
                            errorSource: suggestedAction,
                            call: () => suggestedAction.Invoke(context),
                            exceptionGuardFilter: e => e is not OperationCanceledException);
                    });

                return true;
            };
        }

        private async Task<IEnumerable<ISuggestedAction>> GetLightBulbActionsAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!broker.IsLightBulbSessionActive(view))
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new Exception($"No light bulb session in View!  Buffer content type={bufferType}");
            }

            var activeSession = broker.GetSession(view);
            if (activeSession == null)
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={bufferType}");
            }

            var actionSets = await LightBulbHelper.WaitForItemsAsync(broker, view, cancellationToken);
            return await SelectActionsAsync(actionSets, cancellationToken);
        }

        private async Task<IEnumerable<ISuggestedAction>> SelectActionsAsync(IEnumerable<SuggestedActionSet> actionSets, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
                            var nestedActionSets = await action.GetActionSetsAsync(cancellationToken);
                            var nestedActions = await SelectActionsAsync(nestedActionSets, cancellationToken);
                            actions.AddRange(nestedActions);
                        }
                    }
                }
            }

            return actions;
        }

        private async Task<FixAllSuggestedAction?> GetFixAllSuggestedActionAsync(IEnumerable<SuggestedActionSet> actionSets, FixAllScope fixAllScope, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
                        var nestedActionSets = await action.GetActionSetsAsync(cancellationToken);
                        var fixAllCodeAction = await GetFixAllSuggestedActionAsync(nestedActionSets, fixAllScope, cancellationToken);
                        if (fixAllCodeAction != null)
                        {
                            return fixAllCodeAction;
                        }
                    }
                }
            }

            return null;
        }

        public Task PlaceCaretAsync(string marker, int charsOffset, CancellationToken cancellationToken)
            => PlaceCaretAsync(marker, charsOffset, occurrence: 0, extendSelection: false, selectBlock: false, cancellationToken);

        public async Task PlaceCaretAsync(
            string marker,
            int charsOffset,
            int occurrence,
            bool extendSelection,
            bool selectBlock,
            CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            dte.Find.FindWhat = marker;
            dte.Find.MatchCase = true;
            dte.Find.MatchInHiddenText = true;
            dte.Find.Target = EnvDTE.vsFindTarget.vsFindTargetCurrentDocument;
            dte.Find.Action = EnvDTE.vsFindAction.vsFindActionFind;

            var originalPosition = await GetCaretPositionAsync(cancellationToken);
            view.Caret.MoveTo(new SnapshotPoint(view.GetBufferContainingCaret()!.CurrentSnapshot, 0));

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

        public async Task<int> GetCaretPositionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var view = await GetActiveTextViewAsync(cancellationToken);

            var subjectBuffer = view.GetBufferContainingCaret();
            Assumes.Present(subjectBuffer);

            var bufferPosition = view.Caret.Position.BufferPosition;
            return bufferPosition.Position;
        }

        private async Task WaitForCompletionSetAsync(CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, cancellationToken);
        }
    }
}
