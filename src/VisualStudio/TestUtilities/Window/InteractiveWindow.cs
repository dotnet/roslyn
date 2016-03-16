// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class InteractiveWindow
    {
        private const string DteCSharpViewCommand = "View.C#Interactive";
        private const string DteCSharpWindowTitle = "C# Interactive";
        private const string DteReplResetCommand = "InteractiveConsole.Reset";

        private const string ReplPromptText = "> ";
        private const string ReplSubmissionText = ". ";

        private readonly string _dteViewCommand;
        private IntegrationHost _host;
        private InteractiveWindowWrapper _interactiveWindowWrapper;

        internal static InteractiveWindow CreateCSharpInteractiveWindow(IntegrationHost host)
            => new InteractiveWindow(host, DteCSharpViewCommand, DteCSharpWindowTitle);

        internal InteractiveWindow(IntegrationHost host, string dteViewCommand, string dteWindowTitle)
        {
            _host = host;
            _dteViewCommand = dteViewCommand;

            // We have to show the window at least once to ensure the interactive service is loaded.
            Show(waitForPrompt: false);

            var dteWindow = _host.LocateDteWindow(dteWindowTitle);
            dteWindow.Close();

            _interactiveWindowWrapper = _host.ExecuteOnHostProcess<InteractiveWindowWrapper>(typeof(RemotingHelper), nameof(RemotingHelper.CreateCSharpInteractiveWindowWrapper), (BindingFlags.Public | BindingFlags.Static));
        }

        public string LastReplOutput
        {
            get
            {
                var replText = ReplTextWithoutPrompt;
                int lastPromptIndex = replText.LastIndexOf(ReplPromptText);

                replText = replText.Substring(lastPromptIndex, (replText.Length - lastPromptIndex));
                int lastSubmissionIndex = replText.LastIndexOf(ReplSubmissionText);

                if (lastSubmissionIndex > 0)
                {
                    replText = replText.Substring(lastSubmissionIndex, (replText.Length - lastSubmissionIndex));
                }
                else if (!replText.StartsWith(ReplPromptText))
                {
                    return replText;
                }

                int firstNewLineIndex = replText.IndexOf(Environment.NewLine);

                if (firstNewLineIndex <= 0)
                {
                    return replText;
                }

                firstNewLineIndex += Environment.NewLine.Length;
                return replText.Substring(firstNewLineIndex, (replText.Length - firstNewLineIndex));
            }
        }

        public string ReplText
            => _interactiveWindowWrapper.CurrentSnapshotText;

        public string ReplTextWithoutPrompt
        {
            get
            {
                var replText = ReplText;

                // find last prompt and remove
                int lastPromptIndex = replText.LastIndexOf(ReplPromptText);

                if (lastPromptIndex > 0)
                {
                    replText = replText.Substring(0, lastPromptIndex);
                }

                // it's possible for the editor text to contain a trailing newline, remove it
                return replText.EndsWith(Environment.NewLine) ? replText.Substring(0, (replText.Length - Environment.NewLine.Length)) : replText;
            }
        }

        public bool CheckLastReplOutputEndsWith(string expectedText)
            => LastReplOutput.EndsWith(expectedText);

        public bool CheckLastReplOutputEquals(string expectedText)
            => LastReplOutput.Equals(expectedText);

        public bool CheckLastReplOutputContains(string expectedText)
            => LastReplOutput.Contains(expectedText);

        public void Reset(bool waitForPrompt = true)
            => ResetAsync(waitForPrompt).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

        public async Task ResetAsync(bool waitForPrompt = true)
        {
            await _host.ExecuteDteCommandAsync(DteReplResetCommand).ConfigureAwait(continueOnCapturedContext: false);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public void Show(bool waitForPrompt = true)
            => ShowAsync(waitForPrompt).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

        public async Task ShowAsync(bool waitForPrompt = true)
        {
            await _host.ExecuteDteCommandAsync(_dteViewCommand).ConfigureAwait(continueOnCapturedContext: false);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public void SubmitTextToRepl(string text, bool waitForPrompt = true)
            => SubmitTextToReplAsync(text, waitForPrompt).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

        public async Task SubmitTextToReplAsync(string text, bool waitForPrompt = true)
        {
            _interactiveWindowWrapper.Submit(text);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public void WaitForReplPrompt()
            => WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

        public Task WaitForReplPromptAsync()
            => IntegrationHelper.WaitForResultAsync(() => ReplText.EndsWith(ReplPromptText), expectedResult: true);
    }
}
