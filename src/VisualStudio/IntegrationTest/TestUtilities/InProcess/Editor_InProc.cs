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
using System.Windows.Forms;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;
using ControlType = System.Windows.Automation.ControlType;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class Editor_InProc : TextViewWindow_InProc
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private Editor_InProc() { }

        public static Editor_InProc Create()
            => new Editor_InProc();

        protected override IWpfTextView GetActiveTextView()
            => GetActiveTextViewHost().TextView;

        private static IVsTextView GetActiveVsTextView()
        {
            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

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

        public string GetActiveBufferName()
        {
            return GetDTE().ActiveDocument.Name;
        }

        public void WaitForActiveView(string expectedView)
        {
            Retry(GetActiveBufferName, (actual) => actual == expectedView, TimeSpan.FromMilliseconds(100));
        }

        public void Activate()
            => GetDTE().ActiveDocument.Activate();

        public bool IsProjectItemDirty()
            => GetDTE().ActiveDocument.ProjectItem.IsDirty;

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

        public string GetCurrentLineText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });

        public string GetSelectedText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var selectedSpan = view.Selection.SelectedSpans[0];
                return subjectBuffer.CurrentSnapshot.GetText(selectedSpan);
            });

        public void MoveCaret(int position)
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });

        public string[] GetHighlightTags()
           => GetTags<ITextMarkerTag>(tag => tag.Type == KeywordHighlightTag.TagId);

        private string PrintSpan(SnapshotSpan span)
                => $"'{span.GetText()}'[{span.Start.Position}-{span.Start.Position + span.Length}]";

        private string[] GetTags<TTag>(Predicate<TTag> filter = null)
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
                return tags.Select(tag => $"{tag.Tag.ToString()}:{PrintSpan(tag.Span.GetSpans(view.TextBuffer).Single())}").ToArray();
            });
        }

        public ClassifiedToken[] GetLightbulbPreviewClassifications(string menuText)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

        protected override ITextBuffer GetBufferContainingCaret(IWpfTextView view)
        {
            return view.GetBufferContainingCaret();
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

        public void GoToDefinition()
            => GetDTE().ExecuteCommand("Edit.GoToDefinition");

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
    }
}
