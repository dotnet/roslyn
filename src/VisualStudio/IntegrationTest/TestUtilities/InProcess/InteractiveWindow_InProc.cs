// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    /// <summary>
    /// Provides a means of accessing the <see cref="IInteractiveWindow"/> service in the Visual Studio host.
    /// </summary>
    /// <remarks>
    /// This object exists in the Visual Studio host and is marhsalled across the process boundary.
    /// </remarks>
    internal abstract class InteractiveWindow_InProc : TextViewWindow_InProc, IDisposable
    {
        private const string NewLineFollowedByReplSubmissionText = "\n. ";
        private const string ReplSubmissionText = ". ";
        private const string ReplPromptText = "> ";

        private readonly string _viewCommand;
        private readonly string _windowTitle;
        private IInteractiveWindow _interactiveWindow;

        protected InteractiveWindow_InProc(string viewCommand, string windowTitle)
        {
            _viewCommand = viewCommand;
            _windowTitle = windowTitle;
        }

        public void Initialize()
        {
            // We have to show the window at least once to ensure the interactive service is loaded.
            ShowWindow(waitForPrompt: false);
            CloseWindow();

            _interactiveWindow = AcquireInteractiveWindow();
            _interactiveWindow.ReadyForInput += InteractiveWindow_ReadyForInput;
        }

        public event Action ReadyForInput; 

        public void Dispose()
        {
            _interactiveWindow.ReadyForInput -= InteractiveWindow_ReadyForInput;
        }

        protected abstract IInteractiveWindow AcquireInteractiveWindow();

        public bool IsInitializing
            => _interactiveWindow.IsInitializing;

        public string GetReplText()
            => _interactiveWindow.TextView.TextBuffer.CurrentSnapshot.GetText();

        protected override IWpfTextView GetActiveTextView()
            => _interactiveWindow.TextView;

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

            string lastReplInputWithReplSubmissionText = (firstNewLineIndex <= 0) ? replText : replText.Substring(0, firstNewLineIndex);

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

        public void SubmitText(string text, bool waitForPrompt = true)
        {
            _interactiveWindow.SubmitAsync(new[] { text }).Wait();

            if (waitForPrompt)
            {
                WaitForReplPrompt();
            }
        }

        public void CloseWindow()
        {
            var dte = GetDTE();

            foreach (EnvDTE.Window window in dte.Windows)
            {
                if (window.Caption == _windowTitle)
                {
                    window?.Close();
                    break;
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                return _interactiveWindow.IsRunning;
            }
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
            => WaitForReplPromptAsync().Wait();

        private async Task WaitForReplPromptAsync()
        {
            while (!GetReplText().EndsWith(ReplPromptText))
            {
                await Task.Delay(50);
            }
        }

        public void WaitForReplOutput(string outputText)
            => WaitForReplOutputAsync(outputText).Wait();

        public void ClearScreen()
        {
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ClearScreen);
        }

        public void InsertCode(string text)
        {
            _interactiveWindow.InsertCode(text);
        }

        private async Task WaitForReplOutputAsync(string outputText)
        {
            while (!GetReplText().EndsWith(outputText + Environment.NewLine + ReplPromptText))
            {
                await Task.Delay(50);
            }
        }

        public void WaitForReplOutputContains(string outputText)
            => WaitForReplOutputContainsAsync(outputText).Wait();

        private async Task WaitForReplOutputContainsAsync(string outputText)
        {
            while (!GetReplText().Contains(outputText))
            {
                await Task.Delay(50);
            }
        }

        protected override ITextBuffer GetBufferContainingCaret(IWpfTextView view)
        {
            return _interactiveWindow.TextView.TextBuffer;
        }

        protected void InteractiveWindow_ReadyForInput()
        {
            ReadyForInput();
        }
    }
}