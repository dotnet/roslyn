// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Editor_InProc : InProcComponent
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private Editor_InProc() { }

        public static Editor_InProc Create()
            => new Editor_InProc();

        private static IWpfTextView GetActiveTextView()
            => GetActiveTextViewHost().TextView;

        private static IVsTextView GetActiveVsTextView()
        {
            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

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

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle();

            var activeVsTextView = (IVsUserData)GetActiveVsTextView();

            var hresult = activeVsTextView.GetData(IWpfTextViewId, out var wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        /// <summary>
        /// Non-blocking version of <see cref="ExecuteOnActiveView"/>
        /// </summary>
        private static void BeginInvokeExecuteOnActiveView(Action<IWpfTextView> action)
            => BeginInvokeOnUIThread(GetExecuteOnActionViewCallback(action));

        private static void ExecuteOnActiveView(Action<IWpfTextView> action)
            => InvokeOnUIThread(GetExecuteOnActionViewCallback(action));

        private static Action GetExecuteOnActionViewCallback(Action<IWpfTextView> action)
            => () =>
            {
                var view = GetActiveTextView();
                action(view);
            };

        private static T ExecuteOnActiveView<T>(Func<IWpfTextView, T> action)
            => InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                return action(view);
            });

        public void Activate()
            => GetDTE().ActiveDocument.Activate();

        public string GetText()
            => ExecuteOnActiveView(view => view.TextSnapshot.GetText());

        public void SetText(string text)
            => ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            });

        public string GetCurrentLineText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });

        public int GetCaretPosition()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;

                return bufferPosition.Position;
            });

        public string GetLineTextBeforeCaret()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(0, bufferPosition.Position - line.Start);
            });

        public string GetLineTextAfterCaret()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(bufferPosition.Position - line.Start);
            });

        public void MoveCaret(int position)
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });

        public void PlaceCaret(string marker, int charsOffset, int occurrence, bool extendSelection, bool selectBlock)
            => ExecuteOnActiveView(view =>
            {
                var dte = GetDTE();
                dte.Find.FindWhat = marker;
                dte.Find.MatchCase = true;
                dte.Find.MatchInHiddenText = true;
                dte.Find.Target = EnvDTE.vsFindTarget.vsFindTargetCurrentDocument;
                dte.Find.Action = EnvDTE.vsFindAction.vsFindActionFind;

                var originalPosition = GetCaretPosition();
                view.Caret.MoveTo(new Microsoft.VisualStudio.Text.SnapshotPoint(view.GetBufferContainingCaret().CurrentSnapshot, 0));

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

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public bool IsSignatureHelpActive()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ISignatureHelpBroker>();
                return broker.IsSignatureHelpActive(view);
            });

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public Signature[] GetSignatures()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broker.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return sessions[0].Signatures.Select(s => new Signature(s)).ToArray();
            });

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public Signature GetCurrentSignature()
            => ExecuteOnActiveView(view =>
            {
                var broken = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broken.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return new Signature(sessions[0].SelectedSignature);
            });

        public bool IsCaretOnScreen()
            => ExecuteOnActiveView(view =>
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

        public void ShowLightBulb()
            => InvokeOnUIThread(() => GetDTE().ExecuteCommand("View.ShowSmartTag"));

        public void WaitForLightBulbSession()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                LightBulbHelper.WaitForLightBulbSession(broker, view);
            });

        public void DismissLightBulbSession()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                broker.DismissSession(view);
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

        public void MessageBox(string message)
            => ExecuteOnActiveView(view => System.Windows.MessageBox.Show(message));

        public void VerifyDialog(string dialogAutomationId, bool isOpen)
        {
            var dialogAutomationElement = DialogHelpers.FindDialog(GetDTE().MainWindow.HWnd, dialogAutomationId, isOpen);

            if ((isOpen && dialogAutomationElement == null) ||
                (!isOpen && dialogAutomationElement != null))
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be {(isOpen ? "open" : "closed")}, but it is not.");
            }
        }

        public void DialogSendKeys(string dialogAutomationName, string keys)
        {
            var dialogAutomationElement = DialogHelpers.GetOpenDialog(GetDTE().MainWindow.HWnd, dialogAutomationName);

            dialogAutomationElement.SetFocus();
            SendKeys.SendWait(keys);
        }

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
        {
            DialogHelpers.PressButton(GetDTE().MainWindow.HWnd, dialogAutomationName, buttonAutomationName);
        }

        private AutomationElement FindDialog(string dialogAutomationName, bool isOpen)
        {
            return Retry(
                () => FindDialogWorker(dialogAutomationName),
                stoppingCondition: automationElement => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250));
        }

        private static AutomationElement FindDialogWorker(string dialogAutomationName)
        {
            var vsAutomationElement = AutomationElement.FromHandle(new IntPtr(GetDTE().MainWindow.HWnd));

            Condition elementCondition = new AndCondition(
                new PropertyCondition(AutomationElement.AutomationIdProperty, dialogAutomationName),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            return vsAutomationElement.FindFirst(TreeScope.Descendants, elementCondition);
        }

        private T Retry<T>(Func<T> action, Func<T, bool> stoppingCondition, TimeSpan delay)
        {
            var beginTime = DateTime.UtcNow;
            var retval = default(T);

            do
            {
                try
                {
                    retval = action();
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.

                    Thread.Sleep(delay);
                    continue;
                }

                if (stoppingCondition(retval))
                {
                    return retval;
                }
                else
                {
                    Thread.Sleep(delay);
                }
            }
            while (true);
        }

        public void AddWinFormButton(string buttonName)
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;
                void ComponentAdded(object sender, ComponentEventArgs e)
                {
                    var control = e.Component as Control;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentAdded += ComponentAdded;

                try
                {
                    var mainForm = (Form)designerHost.RootComponent;
                    InvokeOnUIThread(() =>
                    {
                        var newControl = (Button)designerHost.CreateComponent(typeof(Button), buttonName);
                        newControl.Parent = mainForm;
                    });
                    waitHandle.WaitOne();
                }
                finally
                {
                    componentChangeService.ComponentAdded -= ComponentAdded;
                }
            }
        }

        public void DeleteWinFormButton(string buttonName)
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;
                void ComponentRemoved(object sender, ComponentEventArgs e)
                {
                    var control = e.Component as Control;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentRemoved += ComponentRemoved;

                try
                {
                    InvokeOnUIThread(() =>
                    {
                        designerHost.DestroyComponent(designerHost.Container.Components[buttonName]);
                    });
                    waitHandle.WaitOne();
                }
                finally
                {
                    componentChangeService.ComponentRemoved -= ComponentRemoved;
                }
            }
        }

        public void EditWinFormButtonProperty(string buttonName, string propertyName, string propertyValue, string propertyTypeName = null)
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;

                object GetEnumPropertyValue(string typeName, string value)
                {
                    var type = Type.GetType(typeName);
                    var converter = new EnumConverter(type);
                    return converter.ConvertFromInvariantString(value);
                }

                bool EqualToPropertyValue(object newValue)
                {
                    if (propertyTypeName == null)
                    {
                        return (newValue as string)?.Equals(propertyValue) == true;
                    }
                    else
                    {
                        var enumPropertyValue = GetEnumPropertyValue(propertyTypeName, propertyValue);
                        return newValue?.Equals(enumPropertyValue) == true;
                    }
                }

                void ComponentChanged(object sender, ComponentChangedEventArgs e)
                {
                    if (e.Member.Name == propertyName && EqualToPropertyValue(e.NewValue))
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentChanged += ComponentChanged;

                try
                {
                    InvokeOnUIThread(() =>
                    {
                        var button = designerHost.Container.Components[buttonName];
                        var properties = TypeDescriptor.GetProperties(button);
                        var property = properties[propertyName];
                        if (propertyTypeName == null)
                        {
                            property.SetValue(button, propertyValue);
                        }
                        else
                        {
                            var enumPropertyValue = GetEnumPropertyValue(propertyTypeName, propertyValue);
                            property.SetValue(button, enumPropertyValue);
                        }
                    });
                    waitHandle.WaitOne();
                }
                finally
                {
                    componentChangeService.ComponentChanged -= ComponentChanged;
                }
            }
        }

        public void EditWinFormButtonEvent(string buttonName, string eventName, string eventHandlerName)
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;
                void ComponentChanged(object sender, ComponentChangedEventArgs e)
                {
                    if (e.Member.Name == eventName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentChanged += ComponentChanged;

                try
                {
                    InvokeOnUIThread(() =>
                    {
                        var button = designerHost.Container.Components[buttonName];
                        var eventBindingService = (IEventBindingService)button.Site.GetService(typeof(IEventBindingService));
                        var events = TypeDescriptor.GetEvents(button);
                        var eventProperty = eventBindingService.GetEventProperty(events.Find(eventName, ignoreCase: true));
                        eventProperty.SetValue(button, eventHandlerName);
                    });
                    waitHandle.WaitOne();
                }
                finally
                {
                    componentChangeService.ComponentChanged -= ComponentChanged;
                }
            }
        }

        public string GetWinFormButtonPropertyValue(string buttonName, string propertyName)
        {
            var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
            var button = designerHost.Container.Components[buttonName];
            var properties = TypeDescriptor.GetProperties(button);
            return properties[propertyName].GetValue(button) as string;
        }

        public void Undo()
            => GetDTE().ExecuteCommand("Edit.Undo");
    }
}
