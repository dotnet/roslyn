// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
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
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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

        public string GetLineTextBeforeCaret()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text[..(bufferPosition.Position - line.Start)];
            });

        public string GetLineTextAfterCaret()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text[(bufferPosition.Position - line.Start)..];
            });

        public void MoveCaret(int position)
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                Contract.ThrowIfNull(subjectBuffer);

                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
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
#pragma warning disable CS0618 // Type or member is obsolete
                if (activeSession.TryGetSuggestedActionSets(out var actionSets) != QuerySuggestedActionCompletionStatus.Completed)
                {
                    actionSets = Array.Empty<SuggestedActionSet>();
                }
#pragma warning restore CS0618 // Type or member is obsolete

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
