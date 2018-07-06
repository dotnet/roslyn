// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess.ReflectionExtensions;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using WinForms = System.Windows.Forms;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public partial class Editor_InProc2 : TextViewWindow_InProc2
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        public Editor_InProc2(TestServices testServices)
            : base(testServices)
        {
            Verify = new Verifier(this);
        }

        public new Verifier Verify
        {
            get;
        }

        protected override async Task<IWpfTextView> GetActiveTextViewAsync()
            => (await GetActiveTextViewHostAsync()).TextView;

        private async Task<IVsTextView> GetActiveVsTextViewAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var vsTextManager = await GetGlobalServiceAsync<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

        private async Task<IWpfTextViewHost> GetActiveTextViewHostAsync()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            await WaitForApplicationIdleAsync(CancellationToken.None);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var activeVsTextView = (IVsUserData)await GetActiveVsTextViewAsync();

            var hresult = activeVsTextView.GetData(IWpfTextViewId, out var wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

#if false
        public string GetActiveBufferName()
        {
            return GetDTE().ActiveDocument.Name;
        }

        public void WaitForActiveView(string expectedView)
        {
            Retry(GetActiveBufferName, (actual) => actual == expectedView, TimeSpan.FromMilliseconds(100));
        }
#endif

        public async Task ActivateAsync()
        {
            (await GetDTEAsync()).ActiveDocument.Activate();
        }

        public async Task<bool> IsProjectItemDirtyAsync()
            => (await GetDTEAsync()).ActiveDocument.ProjectItem.IsDirty;

        public async Task<string> GetTextAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            return view.TextSnapshot.GetText();
        }

        public async Task SetTextAsync(string text)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var textSnapshot = view.TextSnapshot;
            var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
            view.TextBuffer.Replace(replacementSpan, text);
        }

        public async Task SelectTextAsync(string text)
        {
            await PlaceCaretAsync(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            await PlaceCaretAsync(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        public async Task ReplaceTextAsync(string oldText, string newText)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();

            var textSnapshot = view.TextSnapshot;
            await SelectTextAsync(oldText);
            var replacementSpan = new SnapshotSpan(textSnapshot, view.Selection.Start.Position, view.Selection.End.Position - view.Selection.Start.Position);
            view.TextBuffer.Replace(replacementSpan, newText);
        }

        public async Task<string> GetCurrentLineTextAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var subjectBuffer = view.GetBufferContainingCaret();
            var bufferPosition = view.Caret.Position.BufferPosition;
            var line = bufferPosition.GetContainingLine();

            return line.GetText();
        }

        public async Task<int> GetLineAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            view.Caret.Position.BufferPosition.GetLineAndColumn(out int lineNumber, out int columnIndex);
            return lineNumber;
        }

        public async Task<int> GetColumnAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            view.Caret.Position.BufferPosition.GetLineAndColumn(out int lineNumber, out int columnIndex);
            return columnIndex;
        }

#if false
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

        public string GetSelectedText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var selectedSpan = view.Selection.SelectedSpans[0];
                return subjectBuffer.CurrentSnapshot.GetText(selectedSpan);
            });
#endif

        public async Task MoveCaretAsync(int position)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var subjectBuffer = view.GetBufferContainingCaret();
            var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

            view.Caret.MoveTo(point);
        }

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public async Task<bool> IsSignatureHelpActiveAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForSignatureHelpAsync();

            var view = await GetActiveTextViewAsync();

            var broker = await GetComponentModelServiceAsync<ISignatureHelpBroker>();
            return broker.IsSignatureHelpActive(view);
        }

        public async Task<string[]> GetErrorTagsAsync()
            => await GetTagsAsync<IErrorTag>();

#if false
        public string[] GetHighlightTags()
           => GetTags<ITextMarkerTag>(tag => tag.Type == KeywordHighlightTag.TagId);
#endif

        private string PrintSpan(SnapshotSpan span)
            => $"'{span.GetText()}'[{span.Start.Position}-{span.Start.Position + span.Length}]";

        private async Task<string[]> GetTagsAsync<TTag>(Predicate<TTag> filter = null)
            where TTag : ITag
        {
            bool Filter(TTag tag)
                => true;

            if (filter == null)
            {
                filter = Filter;
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var viewTagAggregatorFactory = await GetComponentModelServiceAsync<IViewTagAggregatorFactoryService>();
            var aggregator = viewTagAggregatorFactory.CreateTagAggregator<TTag>(view);
            var tags = aggregator
                .GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length))
                .Where(t => filter(t.Tag))
                .OrderBy(t => t.Span.GetSpans(view.TextBuffer).Single().Span.Start)
                .ThenBy(t => t.Span.GetSpans(view.TextBuffer).Single().Span.End)
                .Cast<IMappingTagSpan<ITag>>();
            return tags.Select(tag => $"{tag.Tag.ToString()}:{PrintSpan(tag.Span.GetSpans(view.TextBuffer).Single())}").ToArray();
        }

#if false
        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public Signature[] GetSignatures()
            => ExecuteOnActiveView(view =>
            {
                await WaitForSignatureHelpAsync();

                var broker = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broker.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return sessions[0].Signatures.Select(s => new Signature(s)).ToArray();
            });
#endif

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public async Task<Signature> GetCurrentSignatureAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await WaitForSignatureHelpAsync();

            var view = await GetActiveTextViewAsync();
            var broker = await GetComponentModelServiceAsync<ISignatureHelpBroker>();

            var sessions = broker.GetSessions(view);
            if (sessions.Count != 1)
            {
                throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
            }

            return new Signature(sessions[0].SelectedSignature);
        }

        public async Task<bool> IsCaretOnScreenAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var caret = view.Caret;

            return caret.Left >= view.ViewportLeft
                && caret.Right <= view.ViewportRight
                && caret.Top >= view.ViewportTop
                && caret.Bottom <= view.ViewportBottom;
        }

        public async Task<ClassifiedToken[]> GetLightbulbPreviewClassificationsAsync(string menuText)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            var broker = (await GetComponentModelAsync()).GetService<ILightBulbBroker>();
            var classifierAggregatorService = await GetComponentModelServiceAsync<IViewClassifierAggregatorService>();
            return await GetLightbulbPreviewClassificationsAsync(
                menuText,
                broker,
                view,
                classifierAggregatorService);
        }

        private async Task<ClassifiedToken[]> GetLightbulbPreviewClassificationsAsync(
            string menuText,
            ILightBulbBroker broker,
            IWpfTextView view,
            IViewClassifierAggregatorService viewClassifierAggregator)
        {
            await LightBulbHelper.WaitForLightBulbSessionAsync(broker, view).ConfigureAwait(true);

            var bufferType = view.TextBuffer.ContentType.DisplayName;
            if (!broker.IsLightBulbSessionActive(view))
            {
                throw new Exception(string.Format("No Active Smart Tags in View!  Buffer content type={0}", bufferType));
            }

            var activeSession = broker.GetSession(view);
            if (activeSession == null || !activeSession.IsExpanded)
            {
                throw new InvalidOperationException(string.Format("No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={0}", bufferType));
            }

            if (!string.IsNullOrEmpty(menuText))
            {
                if (activeSession.TryGetSuggestedActionSets(out var actionSets) != QuerySuggestedActionCompletionStatus.Completed)
                {
                    actionSets = Array.Empty<SuggestedActionSet>();
                }

                var set = actionSets.SelectMany(s => s.Actions).FirstOrDefault(a => a.DisplayText == menuText);
                if (set == null)
                {
                    throw new InvalidOperationException(
                        string.Format("ISuggestionAction {0} not found.  Buffer content type={1}", menuText, bufferType));
                }

                IWpfTextView preview = null;
                object pane = await set.GetPreviewAsync(CancellationToken.None).ConfigureAwait(true);
                if (pane is System.Windows.Controls.UserControl)
                {
                    var container = ((System.Windows.Controls.UserControl)pane).FindName("PreviewDockPanel") as DockPanel;
                    var host = FindDescendants<UIElement>(container).OfType<IWpfTextViewHost>().LastOrDefault();
                    preview = (host == null) ? null : host.TextView;
                }

                if (preview == null)
                {
                    throw new InvalidOperationException(string.Format("Could not find light bulb preview.  Buffer content type={0}", bufferType));
                }

                activeSession.Collapse();
                var classifier = viewClassifierAggregator.GetClassifier(preview);
                var classifiedSpans = classifier.GetClassificationSpans(new SnapshotSpan(preview.TextBuffer.CurrentSnapshot, 0, preview.TextBuffer.CurrentSnapshot.Length));
                return classifiedSpans.Select(x => new ClassifiedToken(x.Span.GetText().ToString(), x.ClassificationType.Classification)).ToArray();
            }

            activeSession.Collapse();
            return Array.Empty<ClassifiedToken>();
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject rootObject) where T : DependencyObject
        {
            if (rootObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(rootObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(rootObject, i);

                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T descendant in FindDescendants<T>(child))
                        yield return descendant;
                }
            }
        }

#if false
        public void MessageBox(string message)
            => ExecuteOnActiveView(view => System.Windows.MessageBox.Show(message));
#endif

        /// <summary>
        /// Sends key strokes to the active editor in Visual Studio. Various types are supported by this method:
        /// <see cref="string"/> (each character will be sent separately, <see cref="char"/>, <see cref="VirtualKey"/>
        /// and <see cref="KeyPress"/>.
        /// </summary>
        public async Task SendKeysAsync(params object[] keys)
        {
            await ActivateAsync();
            await TestServices.SendKeys.SendAsync(keys);
        }

#if false
        public void VerifyDialog(string dialogAutomationId, bool isOpen)
        {
            var dialogAutomationElement = DialogHelpers.FindDialogByAutomationId((IntPtr)GetDTE().MainWindow.HWnd, dialogAutomationId, isOpen);

            if ((isOpen && dialogAutomationElement == null) ||
                (!isOpen && dialogAutomationElement != null))
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be {(isOpen ? "open" : "closed")}, but it is not.");
            }
        }

        public void DialogSendKeys(string dialogAutomationName, string keys)
        {
            var dialogAutomationElement = DialogHelpers.GetOpenDialogById((IntPtr)GetDTE().MainWindow.HWnd, dialogAutomationName);

            dialogAutomationElement.SetFocus();
            SendKeys.SendWait(keys);
        }

        public void SendKeysToNavigateTo(string keys)
        {
            var dialogAutomationElement = FindNavigateTo();
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the NavigateTo dialog to be open, but it is not.");
            }

            dialogAutomationElement.SetFocus();
            SendKeys.SendWait(keys);
        }

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
        {
            DialogHelpers.PressButton((IntPtr)GetDTE().MainWindow.HWnd, dialogAutomationName, buttonAutomationName);
        }

        private IUIAutomationElement FindDialog(string dialogAutomationName, bool isOpen)
        {
            return Retry(
                () => FindDialogWorker(dialogAutomationName),
                stoppingCondition: automationElement => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250));
        }

        private static IUIAutomationElement FindDialogWorker(string dialogAutomationName)
        {
            var vsAutomationElement = Helper.Automation.ElementFromHandle((IntPtr)GetDTE().MainWindow.HWnd);

            var elementCondition = Helper.Automation.CreateAndConditionFromArray(
                new[]
                {
                    Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.AutomationIdProperty.Id, dialogAutomationName),
                    Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.ControlTypeProperty.Id, ControlType.Window.Id),
                });

            return vsAutomationElement.FindFirst(TreeScope.TreeScope_Descendants, elementCondition);
        }

        private static IUIAutomationElement FindNavigateTo()
        {
            var vsAutomationElement = Helper.Automation.ElementFromHandle((IntPtr)GetDTE().MainWindow.HWnd);
            return vsAutomationElement.FindDescendantByAutomationId("PART_SearchBox");
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
#endif

        public async Task AddWinFormButtonAsync(string buttonName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)(await GetDTEAsync()).ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;
                void ComponentAdded(object sender, ComponentEventArgs e)
                {
                    var control = (System.Windows.Forms.Control)e.Component;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentAdded += ComponentAdded;

                try
                {
                    var mainForm = (WinForms.Form)designerHost.RootComponent;
                    var newControl = (WinForms.Button)designerHost.CreateComponent(typeof(WinForms.Button), buttonName);
                    newControl.Parent = mainForm;
                    await waitHandle;
                }
                finally
                {
                    componentChangeService.ComponentAdded -= ComponentAdded;
                }
            }
        }

        public async Task DeleteWinFormButtonAsync(string buttonName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)(await GetDTEAsync()).ActiveWindow.Object;
                var componentChangeService = (IComponentChangeService)designerHost;
                void ComponentRemoved(object sender, ComponentEventArgs e)
                {
                    var control = (WinForms.Control)e.Component;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentRemoved += ComponentRemoved;

                try
                {
                    designerHost.DestroyComponent(designerHost.Container.Components[buttonName]);

                    await waitHandle;
                }
                finally
                {
                    componentChangeService.ComponentRemoved -= ComponentRemoved;
                }
            }
        }

        public async Task EditWinFormButtonPropertyAsync(string buttonName, string propertyName, string propertyValue, string propertyTypeName = null)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)(await GetDTEAsync()).ActiveWindow.Object;
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

                    await waitHandle;
                }
                finally
                {
                    componentChangeService.ComponentChanged -= ComponentChanged;
                }
            }
        }

        public async Task EditWinFormButtonEventAsync(string buttonName, string eventName, string eventHandlerName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            using (var waitHandle = new ManualResetEvent(false))
            {
                var designerHost = (IDesignerHost)(await GetDTEAsync()).ActiveWindow.Object;
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
                    var button = designerHost.Container.Components[buttonName];
                    var eventBindingService = (IEventBindingService)button.Site.GetService(typeof(IEventBindingService));
                    var events = TypeDescriptor.GetEvents(button);
                    var eventProperty = eventBindingService.GetEventProperty(events.Find(eventName, ignoreCase: true));
                    eventProperty.SetValue(button, eventHandlerName);

                    await waitHandle;
                }
                finally
                {
                    componentChangeService.ComponentChanged -= ComponentChanged;
                }
            }
        }

        public async Task<string> GetWinFormButtonPropertyValueAsync(string buttonName, string propertyName)
        {
            var designerHost = (IDesignerHost)(await GetDTEAsync()).ActiveWindow.Object;
            var button = designerHost.Container.Components[buttonName];
            var properties = TypeDescriptor.GetProperties(button);
            return properties[propertyName].GetValue(button) as string;
        }

        public async Task UndoAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_Undo);
        }

        protected override Task<ITextBuffer> GetBufferContainingCaretAsync(IWpfTextView view)
        {
            return Task.FromResult(view.GetBufferContainingCaret());
        }

        public async Task<TextSpan[]> GetOutliningSpansAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Outlining);

            var view = await GetActiveTextViewAsync();
            var manager = (await GetComponentModelServiceAsync<IOutliningManagerService>()).GetOutliningManager(view);
            var span = new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length);
            var regions = manager.GetAllRegions(span);
            return regions
                .OrderBy(s => s.Extent.GetStartPoint(view.TextSnapshot))
                .Select(r => r.Extent.GetSpan(view.TextSnapshot).Span.ToTextSpan())
                .ToArray();
        }

#if false
        public List<string> GetF1Keywords()
        {
            return InvokeOnUIThread(() =>
            {
                var results = new List<string>();
                GetActiveVsTextView().GetBuffer(out var textLines);
                Marshal.ThrowExceptionForHR(textLines.GetLanguageServiceID(out var languageServiceGuid));
                Marshal.ThrowExceptionForHR(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.QueryService(languageServiceGuid, out var languageService));
                var languageContextProvider = languageService as IVsLanguageContextProvider;

                IVsMonitorUserContext monitorUserContext = GetGlobalService<SVsMonitorUserContext, IVsMonitorUserContext>();
                Marshal.ThrowExceptionForHR(monitorUserContext.CreateEmptyContext(out var emptyUserContext));
                Marshal.ThrowExceptionForHR(GetActiveVsTextView().GetCaretPos(out var line, out var column));
                var span = new TextManager.Interop.TextSpan()
                {
                    iStartLine = line,
                    iStartIndex = column,
                    iEndLine = line,
                    iEndIndex = column
                };
                
                Marshal.ThrowExceptionForHR(languageContextProvider.UpdateLanguageContext(0, textLines, new[] { span }, emptyUserContext));
                Marshal.ThrowExceptionForHR(emptyUserContext.CountAttributes("keyword", VSConstants.S_FALSE, out var count));
                for (int i = 0; i < count; i++)
                {
                    emptyUserContext.GetAttribute(i, "keyword", VSConstants.S_FALSE, out var key, out var value);
                    results.Add(value);
                }

                return results;
            });
        }
#endif

        public async Task GoToDefinitionAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_GoToDefinition);
        }

#if false
        public void GoToImplementation()
            => GetDTE().ExecuteCommand("Edit.GoToImplementation");

		/// <summary>
        /// Gets the spans where a particular tag appears in the active text view.
        /// </summary>
        /// <returns>
        /// Given a list of tag spans [s1, s2, ...], returns a decomposed array for serialization:
        ///     [s1.Start, s1.Length, s2.Start, s2.Length, ...]
        /// </returns>
        public int[] GetTagSpans(string tagId)
            => InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                var tagAggregatorFactory = GetComponentModel().GetService<IViewTagAggregatorFactoryService>();
                var tagAggregator = tagAggregatorFactory.CreateTagAggregator<ITextMarkerTag>(view);
                var matchingTags = tagAggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length)).Where(t => t.Tag.Type == tagId);

                return matchingTags.Select(t => t.Span.GetSpans(view.TextBuffer).Single().Span.ToTextSpan()).SelectMany(t => new List<int> { t.Start, t.Length }).ToArray();
            });

        public void SendExplicitFocus()
            => InvokeOnUIThread(() =>
            {
                var view = GetActiveVsTextView();
                view.SendExplicitFocus();
            });
#endif

        public async Task InvokeSignatureHelpAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_ParameterInfo);
            await Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SignatureHelp);
        }

#if false
        public void InvokeNavigateTo(string text)
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            NavigateToSendKeys(text);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigateTo);
        }
#endif

        public async Task SelectTextInCurrentDocumentAsync(string text)
        {
            await PlaceCaretAsync(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            await PlaceCaretAsync(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        public async Task DeleteTextAsync(string text)
        {
            await SelectTextInCurrentDocumentAsync(text);
            await SendKeysAsync(VirtualKey.Delete);
        }

        public async Task FormatDocumentAsync()
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await SendKeysAsync(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.D, ShiftState.Ctrl));
        }

        public async Task FormatDocumentViaCommandAsync()
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_FormatDocument);
        }

        public async Task FormatSelectionAsync()
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await SendKeysAsync(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.F, ShiftState.Ctrl));
        }

        public async Task PasteAsync(string text)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            Clipboard.SetText(text);
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_Paste);
        }

        public async Task<string> GetSelectedNavBarItemAsync(int comboBoxIndex)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar);

            var view = await GetActiveTextViewAsync();
            return (await GetNavigationBarComboBoxesAsync(view))[comboBoxIndex].SelectedItem?.ToString();
        }

        public async Task<string> GetProjectNavBarSelectionAsync()
        {
            return await GetSelectedNavBarItemAsync(0);
        }

        public async Task<string> GetTypeNavBarSelectionAsync()
        {
            return await GetSelectedNavBarItemAsync(1);
        }

        public async Task<string> GetMemberNavBarSelectionAsync()
        {
            return await GetSelectedNavBarItemAsync(2);
        }

        public async Task<string[]> GetNavBarItemsAsync(int comboBoxIndex)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar);

            var view = await GetActiveTextViewAsync();
            return (await GetNavigationBarComboBoxesAsync(view))[comboBoxIndex]
                .Items
                .OfType<object>()
                .Select(i => i?.ToString() ?? "")
                .ToArray();
        }

        public async Task<string[]> GetProjectNavBarItemsAsync()
        {
            return await GetNavBarItemsAsync(0);
        }

        public async Task<string[]> GetTypeNavBarItemsAsync()
        {
            return await GetNavBarItemsAsync(1);
        }

        public async Task<string[]> GetMemberNavBarItemsAsync()
        {
            return await GetNavBarItemsAsync(2);
        }

        public async Task<int> GetNavbarItemIndexAsync(int index, string itemText)
        {
            int FindItem(ComboBox comboBox)
            {
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    if (comboBox.Items[i].ToString() == itemText)
                    {
                        return i;
                    }
                }

                return -1;
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            return FindItem((await GetNavigationBarComboBoxesAsync(view))[index]);
        }

        public async Task ExpandNavigationBarAsync(int index)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar);

            var view = await GetActiveTextViewAsync();
            var combobox = (await GetNavigationBarComboBoxesAsync(view))[index];
            combobox.Focus();
            combobox.IsDropDownOpen = true;
        }

        public async Task ExpandProjectNavBarAsync()
        {
            await ExpandNavigationBarAsync(0);
        }

        public async Task ExpandTypeNavBarAsync()
        {
            await ExpandNavigationBarAsync(1);
        }

        public async Task ExpandMemberNavBarAsync()
        {
            await ExpandNavigationBarAsync(2);
        }

        public async Task SelectNavBarItemAsync(int comboboxIndex, string selection)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar);

            var itemIndex = await GetNavbarItemIndexAsync(comboboxIndex, selection);
            if (itemIndex < 0)
            {
                throw new ArgumentException($"Could not find {selection} in combobox");
            }

            await ExpandNavigationBarAsync(comboboxIndex);
            await TestServices.SendKeys.SendAsync(VirtualKey.Home);
            for (var i = 0; i < itemIndex; i++)
            {
                await TestServices.SendKeys.SendAsync(VirtualKey.Down);
            }

            await TestServices.SendKeys.SendAsync(VirtualKey.Enter);
        }

        public async Task SelectProjectNavbarItemAsync(string item)
        {
            await SelectNavBarItemAsync(0, item);
        }

        public async Task SelectTypeNavBarItemAsync(string item)
        {
            await SelectNavBarItemAsync(1, item);
        }

        public async Task SelectMemberNavBarItemAsync(string item)
        {
            await SelectNavBarItemAsync(2, item);
        }

        public async Task<bool> IsNavBarEnabledAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var view = await GetActiveTextViewAsync();
            return await GetNavbarAsync(view) != null;
        }

        private async Task<List<ComboBox>> GetNavigationBarComboBoxesAsync(IWpfTextView textView)
        {
            var margin = await GetNavbarAsync(textView);
            var combos = margin.GetFieldValue<List<ComboBox>>("_combos");
            return combos;
        }

        private async Task<UIElement> GetNavbarAsync(IWpfTextView textView)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var control = textView.VisualElement;
            while (control != null)
            {
                if (control.GetType().Name == "WpfMultiViewHost")
                {
                    break;
                }

                control = VisualTreeHelper.GetParent(control) as FrameworkElement;
            }

            var topMarginControl = control.GetPropertyValue<ContentControl>("TopMarginControl");
            var vsDropDownBarAdapterMargin = topMarginControl.Content as UIElement;
            return vsDropDownBarAdapterMargin;
        }
    }
}
