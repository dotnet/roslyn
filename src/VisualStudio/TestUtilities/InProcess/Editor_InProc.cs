﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.VisualStudio.Test.Utilities.Common;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class Editor_InProc : InProcComponent
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private Editor_InProc() { }

        public static Editor_InProc Create()
        {
            return new Editor_InProc();
        }

        private static IWpfTextView GetActiveTextView()
        {
            return GetActiveTextViewHost().TextView;
        }

        private static IVsTextView GetActiveVsTextView()
        {
            IVsTextView vsTextView = null;

            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle();

            var activeVsTextView = (IVsUserData)GetActiveVsTextView();

            object wpfTextViewHost;
            var hresult = activeVsTextView.GetData(IWpfTextViewId, out wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        private static void ExecuteOnActiveView(Action<IWpfTextView> action)
        {
            InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                action(view);
            });
        }

        private static T ExecuteOnActiveView<T>(Func<IWpfTextView, T> action)
        {
            return InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                return action(view);
            });
        }

        public void Activate()
        {
            GetDTE().ActiveDocument.Activate();
        }

        public string GetText()
        {
            return ExecuteOnActiveView(view =>
            {
                return view.TextSnapshot.GetText();
            });
        }

        public void SetText(string text)
        {
            ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            });
        }

        public string GetCurrentLineText()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });
        }

        public int GetCaretPosition()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;

                return bufferPosition.Position;
            });
        }

        public string GetLineTextBeforeCaret()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(0, bufferPosition.Position - line.Start);
            });
        }

        public string GetLineTextAfterCaret()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(bufferPosition.Position - line.Start);
            });
        }

        public void MoveCaret(int position)
        {
            ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });
        }

        public string[] GetCompletionItems()
        {
            return ExecuteOnActiveView(view =>
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
        }

        public string GetCurrentCompletionItem()
        {
            return ExecuteOnActiveView(view =>
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
        }

        public bool IsCompletionActive()
        {
            return ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ICompletionBroker>();

                return broker.IsCompletionActive(view);
            });
        }

        public Signature[] GetSignatures()
        {
            return ExecuteOnActiveView(view =>
            {
                var broken = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broken.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return sessions[0].Signatures.Select(s => new Signature(s)).ToArray();
            });
        }

        public Signature GetCurrentSignature()
        {
            return ExecuteOnActiveView(view =>
            {
                var broken = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broken.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return new Signature(sessions[0].SelectedSignature);
            });
        }

        public bool IsCaretOnScreen()
        {
            return ExecuteOnActiveView(view =>
            {
                var editorPrimitivesFactoryService = GetComponentModelService<IEditorPrimitivesFactoryService>();
                var viewPrimitivies = editorPrimitivesFactoryService.GetViewPrimitives(view);

                var advancedView = viewPrimitivies.View.AdvancedTextView;
                var caret = advancedView.Caret;

                return caret.Left >= advancedView.ViewportLeft
                    && caret.Right <= advancedView.ViewportRight
                    && caret.Top >= advancedView.ViewportTop
                    && caret.Bottom <= advancedView.ViewportBottom;
            });
        }

        public void ShowLightBulb()
        {
            InvokeOnUIThread(() => GetDTE().ExecuteCommand("View.ShowSmartTag"));
        }

        public void WaitForLightBulbSession()
        {
            ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                LightBulbHelper.WaitForLightBulbSession(broker, view);
            });
        }

        public void DismissLightBulbSession()
        {
            ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                broker.DismissSession(view);
            });
        }

        public bool IsLightBulbSessionExpanded()
        {
            return ExecuteOnActiveView(view =>
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
        }

        public string[] GetLightBulbActions()
        {
            return ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                return GetLightBulbActions(broker, view).Select(a => a.DisplayText).ToArray();
            });
        }

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

            IEnumerable<SuggestedActionSet> actionSets;
            if (activeSession.TryGetSuggestedActionSets(out actionSets) != QuerySuggestedActionCompletionStatus.Completed)
            {
                actionSets = Array.Empty<SuggestedActionSet>();
            }

            return SelectActions(actionSets);
        }

        public void ApplyLightBulbAction(string actionName, FixAllScope? fixAllScope)
        {
            ExecuteOnActiveView(view =>
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
            });
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
                    var fixAllSuggestedAction = action as FixAllSuggestedAction;
                    if (fixAllSuggestedAction != null)
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

        public void MessageBox(string message)
        {
            ExecuteOnActiveView(view =>
            {
                System.Windows.MessageBox.Show(message);
            });
        }
    }
}
