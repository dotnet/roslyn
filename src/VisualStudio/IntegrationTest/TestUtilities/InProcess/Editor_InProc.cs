// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using UIAutomationClient;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used through .NET Remoting.")]
    internal partial class Editor_InProc : TextViewWindow_InProc
    {
        internal static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private readonly SendKeys_InProc _sendKeys;

        private Editor_InProc()
        {
            _sendKeys = new SendKeys_InProc(VisualStudio_InProc.Create());
        }

        public static Editor_InProc Create()
            => new Editor_InProc();

        protected override bool HasActiveTextView()
            => ErrorHandler.Succeeded(TryGetActiveTextViewHost().hr);

        protected override IWpfTextView GetActiveTextView()
            => GetActiveTextViewHost().TextView;

        private static IVsTextView GetActiveVsTextView()
        {
            var (textView, hr) = TryGetActiveVsTextView();
            Marshal.ThrowExceptionForHR(hr);
            return textView;
        }

        private static (IVsTextView textView, int hr) TryGetActiveVsTextView()
        {
            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();
            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            return (vsTextView, hresult);
        }

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            var (textViewHost, hr) = TryGetActiveTextViewHost();
            Marshal.ThrowExceptionForHR(hr);
            Contract.ThrowIfNull(textViewHost);

            return textViewHost;
        }

        private static (IWpfTextViewHost? textViewHost, int hr) TryGetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle(Helper.HangMitigatingTimeout);

            var (activeVsTextView, hr) = TryGetActiveVsTextView();
            if (!ErrorHandler.Succeeded(hr))
            {
                return (null, hr);
            }

            var hresult = ((IVsUserData)activeVsTextView).GetData(IWpfTextViewId, out var wpfTextViewHost);
            return ((IWpfTextViewHost)wpfTextViewHost, hresult);
        }

        public bool IsUseSuggestionModeOn()
        {
            return ExecuteOnActiveView(textView =>
            {
                var featureServiceFactory = GetComponentModelService<IFeatureServiceFactory>();
                var subjectBuffer = GetBufferContainingCaret(textView);

                var options = textView.Options.GlobalOptions;
                EditorOptionKey<bool> optionKey;
                bool defaultOption;
                if (IsDebuggerTextView(textView))
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
            });

            bool IsDebuggerTextView(IWpfTextView textView)
                => textView.Roles.Contains("DEBUGVIEW");
        }

        public void SetUseSuggestionMode(bool value)
        {
            if (IsUseSuggestionModeOn() != value)
            {
                ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

                if (IsUseSuggestionModeOn() != value)
                {
                    throw new InvalidOperationException($"{WellKnownCommandNames.Edit_ToggleCompletionMode} did not leave the editor in the expected state.");
                }
            }

            if (!value)
            {
                // For blocking completion mode, make sure we don't have responsive completion interfering when
                // integration tests run slowly.
                ExecuteOnActiveView(view =>
                {
                    var options = view.Options.GlobalOptions;
                    options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, false);

                    var latencyGuardOptionKey = new EditorOptionKey<bool>("EnableTypingLatencyGuard");
                    options.SetOptionValue(latencyGuardOptionKey, false);
                });
            }
        }

        public string GetActiveBufferName()
        {
            return GetDTE().ActiveDocument.Name;
        }

        public void WaitForActiveView(string expectedView)
        {
            using var cts = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            Retry(_ => GetActiveBufferName(), (actual, _) => actual == expectedView, TimeSpan.FromMilliseconds(100), cts.Token);
        }

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

        public void SelectText(string text)
        {
            PlaceCaret(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            PlaceCaret(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        public void ReplaceText(string oldText, string newText)
            => ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                SelectText(oldText);
                var replacementSpan = new SnapshotSpan(textSnapshot, view.Selection.Start.Position, view.Selection.End.Position - view.Selection.Start.Position);
                view.TextBuffer.Replace(replacementSpan, newText);
            });

        public string GetCurrentLineText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });

        public int GetLine()
            => ExecuteOnActiveView(view =>
            {
                view.Caret.Position.BufferPosition.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                return lineNumber;
            });

        public int GetColumn()
            => ExecuteOnActiveView(view =>
            {
                view.Caret.Position.BufferPosition.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                return characterIndex;
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

        public string GetSelectedText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                Contract.ThrowIfNull(subjectBuffer);

                var selectedSpan = view.Selection.SelectedSpans[0];
                return subjectBuffer.CurrentSnapshot.GetText(selectedSpan);
            });

        public void MoveCaret(int position)
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                Contract.ThrowIfNull(subjectBuffer);

                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
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

        public string[] GetErrorTags()
            => GetTags<IErrorTag>();

        public string[] GetHighlightTags()
           => GetTags<ITextMarkerTag>(tag => tag.Type == KeywordHighlightTag.TagId);

        private string PrintSpan(SnapshotSpan span)
                => $"'{span.GetText().Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n")}'[{span.Start.Position}-{span.Start.Position + span.Length}]";

        private string[] GetTags<TTag>(Predicate<TTag>? filter = null)
            where TTag : ITag
        {
            bool Filter(TTag tag)
                => true;

            if (filter == null)
            {
                filter = Filter;
            }

            return ExecuteOnActiveView(view =>
            {
                var viewTagAggregatorFactory = GetComponentModelService<IViewTagAggregatorFactoryService>();
                var aggregator = viewTagAggregatorFactory.CreateTagAggregator<TTag>(view);
                var tags = aggregator
                  .GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length))
                  .Where(t => filter(t.Tag))
                  .Cast<IMappingTagSpan<ITag>>();
                return tags.Select(tag => $"{tag.Tag}:{PrintSpan(tag.Span.GetSpans(view.TextBuffer).Single())}").ToArray();
            });
        }

        /// <remarks>
        /// This method does not wait for async operations before
        /// querying the editor
        /// </remarks>
        public Signature GetCurrentSignature()
            => ExecuteOnActiveView(view =>
            {
                var broker = GetComponentModelService<ISignatureHelpBroker>();

                var sessions = broker.GetSessions(view);
                if (sessions.Count != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
                }

                return new Signature(sessions[0].SelectedSignature);
            });

        public bool IsCaretOnScreen()
            => ExecuteOnActiveView(view =>
            {
                var caret = view.Caret;

                return caret.Left >= view.ViewportLeft
                    && caret.Right <= view.ViewportRight
                    && caret.Top >= view.ViewportTop
                    && caret.Bottom <= view.ViewportBottom;
            });

        public ClassifiedToken[] GetLightbulbPreviewClassifications(string menuText)
        {
            return JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                var view = GetActiveTextView();
                var broker = GetComponentModel().GetService<ILightBulbBroker>();
                var classifierAggregatorService = GetComponentModelService<IViewClassifierAggregatorService>();
                return await GetLightbulbPreviewClassificationsAsync(
                    menuText,
                    broker,
                    view,
                    classifierAggregatorService).ConfigureAwait(false);
            });
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

                IWpfTextView? preview = null;
                var pane = await set.GetPreviewAsync(CancellationToken.None).ConfigureAwait(true);
                if (pane is System.Windows.Controls.UserControl)
                {
                    var container = ((System.Windows.Controls.UserControl)pane).FindName("PreviewDockPanel") as DockPanel;
                    var host = container?.FindDescendants<UIElement>().OfType<IWpfTextViewHost>().LastOrDefault();
                    preview = host?.TextView;
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

        public void VerifyDialog(string dialogAutomationId, bool isOpen)
        {
            var dialogAutomationElement = DialogHelpers.FindDialogByAutomationId(GetDTE().MainWindow.HWnd, dialogAutomationId, isOpen);

            if ((isOpen && dialogAutomationElement == null) ||
                (!isOpen && dialogAutomationElement != null))
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be {(isOpen ? "open" : "closed")}, but it is not.");
            }
        }

        public void DialogSendKeys(string dialogAutomationName, object[] keys)
        {
            var dialogAutomationElement = DialogHelpers.GetOpenDialogById(GetDTE().MainWindow.HWnd, dialogAutomationName);

            dialogAutomationElement.SetFocus();
            _sendKeys.Send(keys);
        }

        public void SendKeysToNavigateTo(object[] keys)
        {
            var dialogAutomationElement = FindNavigateTo();
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the NavigateTo dialog to be open, but it is not.");
            }

            dialogAutomationElement.SetFocus();
            _sendKeys.Send(keys);
        }

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
        {
            DialogHelpers.PressButton(GetDTE().MainWindow.HWnd, dialogAutomationName, buttonAutomationName);
        }

        private static IUIAutomationElement FindNavigateTo()
        {
            var vsAutomationElement = Helper.Automation.ElementFromHandle(GetDTE().MainWindow.HWnd);
            return vsAutomationElement.FindDescendantByAutomationId("PART_SearchBox");
        }

        private T Retry<T>(Func<CancellationToken, T> action, Func<T, CancellationToken, bool> stoppingCondition, TimeSpan delay, CancellationToken cancellationToken)
        {
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                T retval;
                try
                {
                    retval = action(cancellationToken);
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.

                    Thread.Sleep(delay);
                    continue;
                }

                if (stoppingCondition(retval, cancellationToken))
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
                    var control = (System.Windows.Forms.Control)e.Component;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentAdded += ComponentAdded;

                try
                {
                    var mainForm = (Form)designerHost.RootComponent;
                    InvokeOnUIThread(cancellationToken =>
                    {
                        var newControl = (System.Windows.Forms.Button)designerHost.CreateComponent(typeof(System.Windows.Forms.Button), buttonName);
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
                    var control = (System.Windows.Forms.Control)e.Component;
                    if (control.Name == buttonName)
                    {
                        waitHandle.Set();
                    }
                }

                componentChangeService.ComponentRemoved += ComponentRemoved;

                try
                {
                    InvokeOnUIThread(cancellationToken =>
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

        public void EditWinFormButtonProperty(string buttonName, string propertyName, string propertyValue, string? propertyTypeName = null)
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
                    InvokeOnUIThread(cancellationToken =>
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
                    InvokeOnUIThread(cancellationToken =>
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

        public string? GetWinFormButtonPropertyValue(string buttonName, string propertyName)
        {
            var designerHost = (IDesignerHost)GetDTE().ActiveWindow.Object;
            var button = designerHost.Container.Components[buttonName];
            var properties = TypeDescriptor.GetProperties(button);
            return properties[propertyName].GetValue(button) as string;
        }

        public void FormatDocumentViaCommand()
            => ExecuteCommand(WellKnownCommandNames.Edit_FormatDocument);

        public void Paste()
            => ExecuteCommand(WellKnownCommandNames.Edit_Paste);

        public void Undo()
            => ExecuteCommand(WellKnownCommandNames.Edit_Undo);

        public void Redo()
            => GetDTE().ExecuteCommand(WellKnownCommandNames.Edit_Redo);

        protected override ITextBuffer GetBufferContainingCaret(IWpfTextView view)
        {
            var caretBuffer = view.GetBufferContainingCaret();
            if (caretBuffer is null)
            {
                throw new InvalidOperationException($"Unable to find the buffer containing the caret. Ensure the Editor is activated berfore calling.");
            }

            return caretBuffer;
        }

        public string[] GetOutliningSpans()
        {
            return ExecuteOnActiveView(view =>
            {
                var manager = GetComponentModelService<IOutliningManagerService>().GetOutliningManager(view);
                var span = new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length);
                var regions = manager.GetAllRegions(span);
                return regions
                    .OrderBy(s => s.Extent.GetStartPoint(view.TextSnapshot))
                    .Select(r => PrintSpan(r.Extent.GetSpan(view.TextSnapshot)))
                    .ToArray();
            });
        }

        /// <summary>
        /// Gets the spans where a particular tag appears in the active text view.
        /// </summary>
        /// <returns>
        /// Given a list of tag spans [s1, s2, ...], returns a decomposed array for serialization:
        ///     [s1.Start, s1.Length, s2.Start, s2.Length, ...]
        /// </returns>
        public int[] GetTagSpans(string tagId)
            => InvokeOnUIThread(cancellationToken =>
            {
                var view = GetActiveTextView();
                var tagAggregatorFactory = GetComponentModel().GetService<IViewTagAggregatorFactoryService>();
                var tagAggregator = tagAggregatorFactory.CreateTagAggregator<ITextMarkerTag>(view);
                var matchingTags = tagAggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length)).Where(t => t.Tag.Type == tagId);

                return matchingTags.Select(t => t.Span.GetSpans(view.TextBuffer).Single().Span.ToTextSpan()).SelectMany(t => new List<int> { t.Start, t.Length }).ToArray();
            });

        public void SendExplicitFocus()
            => InvokeOnUIThread(cancellationToken =>
            {
                var view = GetActiveVsTextView();
                view.SendExplicitFocus();
            });

        public void WaitForEditorOperations(TimeSpan timeout)
        {
            var joinableTaskCollection = InvokeOnUIThread(cancellationToken =>
            {
                var shell = GetGlobalService<SVsShell, IVsShell>();
                if (shell.IsPackageLoaded(DefGuidList.guidEditorPkg, out var editorPackage) == VSConstants.S_OK)
                {
                    var asyncPackage = (AsyncPackage)editorPackage;
                    var collection = asyncPackage.GetPropertyValue<JoinableTaskCollection>("JoinableTaskCollection");
                    return collection;
                }

                return null;
            });

            if (joinableTaskCollection is not null)
            {
                using var cts = new CancellationTokenSource(timeout);
                joinableTaskCollection.JoinTillEmptyAsync(cts.Token).Wait(cts.Token);
            }
        }
    }
}
