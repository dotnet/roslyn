// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    /// <summary>
    /// Provides a means of accessing the <see cref="IInteractiveWindow"/> service in the Visual Studio host.
    /// </summary>
    /// <remarks>
    /// This object exists in the Visual Studio host and is marhsalled across the process boundary.
    /// </remarks>
    internal abstract class InteractiveWindow_InProc : TextViewWindow_InProc
    {
        private const string NewLineFollowedByReplSubmissionText = "\n. ";
        private const string ReplSubmissionText = ". ";
        private const string ReplPromptText = "> ";
        private const int DefaultTimeoutInMilliseconds = 10000;

        private readonly string _viewCommand;
        private readonly Guid _windowId;
        private int _timeoutInMilliseconds;
        private IInteractiveWindow _interactiveWindow;

        protected InteractiveWindow_InProc(string viewCommand, Guid windowId)
        {
            _viewCommand = viewCommand;
            _windowId = windowId;
            _timeoutInMilliseconds = DefaultTimeoutInMilliseconds;
        }

        public void Initialize()
        {
            // We have to show the window at least once to ensure the interactive service is loaded.
            ShowWindow(waitForPrompt: false);
            CloseWindow();

            _interactiveWindow = AcquireInteractiveWindow();

            Contract.ThrowIfNull(_interactiveWindow);
        }

        protected abstract IInteractiveWindow AcquireInteractiveWindow();

        public void SetTimeout(int milliseconds)
        {
            _timeoutInMilliseconds = milliseconds;
        }

        public int GetTimeoutInMilliseconds()
        {
            return _timeoutInMilliseconds;
        }

        public bool IsInitializing
            => InvokeOnUIThread(cancellationToken => _interactiveWindow.IsInitializing);

        public string GetReplText()
            => InvokeOnUIThread(cancellationToken => _interactiveWindow.TextView.TextBuffer.CurrentSnapshot.GetText());

        protected override bool HasActiveTextView()
            => InvokeOnUIThread(cancellationToken => _interactiveWindow.TextView) is object;

        protected override IWpfTextView GetActiveTextView()
            => InvokeOnUIThread(cancellationToken => _interactiveWindow.TextView);

        /// <summary>
        /// Gets the contents of the REPL window without the prompt text.
        /// </summary>
        public string GetReplTextWithoutPrompt()
        {
            var replText = GetReplText();

            // find last prompt and remove
            var lastPromptIndex = replText.LastIndexOf(ReplPromptText);

            if (lastPromptIndex > 0)
            {
                replText = replText.Substring(0, lastPromptIndex);
            }

            // it's possible for the editor text to contain a trailing newline, remove it
            return replText.EndsWith(Environment.NewLine)
                ? replText.Substring(0, replText.Length - Environment.NewLine.Length)
                : replText;
        }

        /// <summary>
        /// Gets the last output from the REPL.
        /// </summary>
        public string GetLastReplOutput()
        {
            // TODO: This may be flaky if the last submission contains ReplPromptText

            var replText = GetReplTextWithoutPrompt();
            var lastPromptIndex = replText.LastIndexOf(ReplPromptText);
            if (lastPromptIndex > 0)
                replText = replText.Substring(lastPromptIndex, replText.Length - lastPromptIndex);

            var lastSubmissionIndex = replText.LastIndexOf(NewLineFollowedByReplSubmissionText);

            if (lastSubmissionIndex > 0)
            {
                replText = replText.Substring(lastSubmissionIndex, replText.Length - lastSubmissionIndex);
            }
            else if (!replText.StartsWith(ReplPromptText))
            {
                return replText;
            }

            var firstNewLineIndex = replText.IndexOf(Environment.NewLine);

            if (firstNewLineIndex <= 0)
            {
                return replText;
            }

            firstNewLineIndex += Environment.NewLine.Length;
            return replText.Substring(firstNewLineIndex, replText.Length - firstNewLineIndex);
        }

        /// <summary>
        /// Gets the last input from the REPL.
        /// </summary>
        public string GetLastReplInput()
        {
            // TODO: This may be flaky if the last submission contains ReplPromptText or ReplSubmissionText

            var replText = GetReplText();
            var lastPromptIndex = replText.LastIndexOf(ReplPromptText);
            replText = replText.Substring(lastPromptIndex + ReplPromptText.Length);

            var lastSubmissionTextIndex = replText.LastIndexOf(NewLineFollowedByReplSubmissionText);

            int firstNewLineIndex;
            if (lastSubmissionTextIndex < 0)
            {
                firstNewLineIndex = replText.IndexOf(Environment.NewLine);
            }
            else
            {
                firstNewLineIndex = replText.IndexOf(Environment.NewLine, lastSubmissionTextIndex);
            }

            var lastReplInputWithReplSubmissionText = (firstNewLineIndex <= 0) ? replText : replText.Substring(0, firstNewLineIndex);

            return lastReplInputWithReplSubmissionText.Replace(ReplSubmissionText, string.Empty);
        }

        public void Reset(bool waitForPrompt = true)
        {
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_Reset);

            if (waitForPrompt)
            {
                WaitForReplPrompt();
            }
        }

        public void SubmitText(string text)
        {
            using var cts = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            _interactiveWindow.SubmitAsync(new[] { text }).WithCancellation(cts.Token).Wait();
        }

        public void CloseWindow()
        {
            InvokeOnUIThread(cancellationToken =>
            {
                var shell = GetGlobalService<SVsUIShell, IVsUIShell>();
                if (ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, _windowId, out var windowFrame)))
                {
                    ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
                }
            });
        }

        public void ShowWindow(bool waitForPrompt = true)
        {
            ExecuteCommand(_viewCommand);

            if (waitForPrompt)
            {
                WaitForReplPrompt();
            }
        }

        public void WaitForReplPrompt()
            => WaitForPredicate(GetReplText, value => value.EndsWith(ReplPromptText));

        public void WaitForReplOutput(string outputText)
            => WaitForPredicate(GetReplText, value => value.EndsWith(outputText + Environment.NewLine + ReplPromptText));

        public void ClearScreen()
        {
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ClearScreen);
        }

        public void InsertCode(string text)
        {
            InvokeOnUIThread(cancellationToken => _interactiveWindow.InsertCode(text));
        }

        public void WaitForLastReplOutput(string outputText)
            => WaitForPredicate(GetLastReplOutput, value => value.Contains(outputText));

        public void WaitForLastReplOutputContains(string outputText)
            => WaitForPredicate(GetLastReplOutput, value => value.Contains(outputText));

        public void WaitForLastReplInputContains(string outputText)
            => WaitForPredicate(GetLastReplInput, value => value.Contains(outputText));

        private void WaitForPredicate(Func<string> getValue, Func<string, bool> isExpectedValue)
        {
            var beginTime = DateTime.UtcNow;
            while (!isExpectedValue(getValue()) && DateTime.UtcNow < beginTime.AddMilliseconds(_timeoutInMilliseconds))
            {
                Thread.Sleep(50);
            }

            string value;
            if (!isExpectedValue(value = getValue()))
            {
                throw new Exception($"Unable to find expected content in REPL within {_timeoutInMilliseconds} milliseconds and no exceptions were thrown. Actual content:{Environment.NewLine}[[{value}]]");
            }
        }

        protected override ITextBuffer GetBufferContainingCaret(IWpfTextView view)
        {
            return InvokeOnUIThread(cancellationToken => _interactiveWindow.TextView.TextBuffer);
        }
    }
}
