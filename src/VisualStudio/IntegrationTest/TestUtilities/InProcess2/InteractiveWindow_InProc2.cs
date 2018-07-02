// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public abstract partial class InteractiveWindow_InProc2 : TextViewWindow_InProc2
    {
        private const string NewLineFollowedByReplSubmissionText = "\n. ";
        private const string ReplSubmissionText = ". ";
        private const string ReplPromptText = "> ";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        private readonly string _viewCommand;
        private readonly string _windowTitle;
        private TimeSpan _timeout;
        private IInteractiveWindow _interactiveWindow;

        protected InteractiveWindow_InProc2(TestServices testServices, string viewCommand, string windowTitle)
            : base(testServices)
        {
            _viewCommand = viewCommand;
            _windowTitle = windowTitle;
            _timeout = DefaultTimeout;

            Verify = new Verifier(this);
        }

        public new Verifier Verify
        {
            get;
        }

        public async Task InitializeAsync()
        {
            // We have to show the window at least once to ensure the interactive service is loaded.
            await ShowWindowAsync(waitForPrompt: false);
            await CloseWindowAsync();

            _interactiveWindow = await AcquireInteractiveWindowAsync();
        }

        protected abstract Task<IInteractiveWindow> AcquireInteractiveWindowAsync();

        public void SetTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public TimeSpan GetTimeout()
        {
            return _timeout;
        }

#if false
        public bool IsInitializing
            => _interactiveWindow.IsInitializing;
#endif

        public string GetReplText()
            => _interactiveWindow.TextView.TextBuffer.CurrentSnapshot.GetText();

        public async Task ClearReplTextAsync()
        {
            // Dismiss the pop-up (if any)
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectionCancel);

            // Clear the line
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectionCancel);
        }

        protected override Task<IWpfTextView> GetActiveTextViewAsync()
            => Task.FromResult(_interactiveWindow.TextView);

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

        public async Task ResetAsync(bool waitForPrompt = true)
        {
            await ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_Reset);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync();
            }
        }

        public async Task SubmitTextAsync(string text)
        {
            await _interactiveWindow.SubmitAsync(new[] { text });
        }

        public async Task CloseWindowAsync()
        {
            var dte = await GetDTEAsync();

            foreach (EnvDTE.Window window in dte.Windows)
            {
                if (window.Caption == _windowTitle)
                {
                    window?.Close();
                    break;
                }
            }
        }

        public async Task ShowWindowAsync(bool waitForPrompt = true)
        {
            await ExecuteCommandAsync(_viewCommand);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync();
            }
        }

        public async Task WaitForReplPromptAsync()
            => await WaitForPredicateAsync(GetReplText, value => value.EndsWith(ReplPromptText));

        public async Task WaitForReplOutputAsync(string outputText)
            => await WaitForPredicateAsync(GetReplText, value => value.EndsWith(outputText + Environment.NewLine + ReplPromptText));

        public async Task ClearScreenAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ClearScreen);
        }

        public void InsertCode(string text)
        {
            _interactiveWindow.InsertCode(text);
        }

        public async Task WaitForLastReplOutputAsync(string outputText)
            => await WaitForPredicateAsync(GetLastReplOutput, value => value.Contains(outputText));

        public async Task WaitForLastReplOutputContainsAsync(string outputText)
            => await WaitForPredicateAsync(GetLastReplOutput, value => value.Contains(outputText));

        public async Task WaitForLastReplInputContainsAsync(string outputText)
            => await WaitForPredicateAsync(GetLastReplInput, value => value.Contains(outputText));

        private async Task WaitForPredicateAsync(Func<string> getValue, Func<string, bool> isExpectedValue)
        {
            var beginTime = DateTime.UtcNow;
            string value;
            while (!isExpectedValue(value = getValue()) && DateTime.UtcNow < beginTime.Add(_timeout))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            if (!isExpectedValue(value = getValue()))
            {
                throw new Exception($"Unable to find expected content in REPL within {_timeout.TotalMilliseconds} milliseconds and no exceptions were thrown. Actual content:{Environment.NewLine}[[{value}]]");
            }
        }

        protected override async Task<ITextBuffer> GetBufferContainingCaretAsync(IWpfTextView view)
        {
            var textView = await GetActiveTextViewAsync();
            return textView.TextBuffer;
        }
    }
}
