// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the interactive window in the Visual Studio host.</summary>
    public class InteractiveWindow
    {
        private const string DteCSharpViewCommand = "View.C#Interactive";
        private const string DteCSharpWindowTitle = "C# Interactive";
        private const string DteReplResetCommand = "InteractiveConsole.Reset";

        private const string ReplPromptText = "> ";
        private const string ReplSubmissionText = ". ";

        private readonly string _dteViewCommand;
        private readonly VisualStudioInstance _host;
        private readonly InteractiveWindowWrapper _interactiveWindowWrapper;

        /// <summary>Creates a <see cref="InteractiveWindow"/> instance that can interact with the C# interactive window in the Visual Studio host.</summary>
        internal static InteractiveWindow CreateCSharpInteractiveWindow(VisualStudioInstance host)
            => new InteractiveWindow(host, DteCSharpViewCommand, DteCSharpWindowTitle);

        internal InteractiveWindow(VisualStudioInstance host, string dteViewCommand, string dteWindowTitle)
        {
            _host = host;
            _dteViewCommand = dteViewCommand;

            // We have to show the window at least once to ensure the interactive service is loaded.
            ShowAsync(waitForPrompt: false).GetAwaiter().GetResult();

            var dteWindow = _host.LocateDteWindowAsync(dteWindowTitle).GetAwaiter().GetResult();
            dteWindow.Close();

            // Return a wrapper to the actual interactive window service that exists in the host process
            _interactiveWindowWrapper = _host.ExecuteOnHostProcess<InteractiveWindowWrapper>(typeof(RemotingHelper), nameof(RemotingHelper.CreateCSharpInteractiveWindowWrapper), (BindingFlags.Public | BindingFlags.Static));
        }

        /// <summary>Gets the last output from the REPL.</summary>
        public string LastReplOutput
        {
            get
            {
                // TODO: This may be flaky if the last submission contains ReplPromptText

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

        /// <summary>Gets the contents of the REPL window.</summary>
        public string ReplText
            => _interactiveWindowWrapper.CurrentSnapshotText;

        /// <summary>Gets the contents of the REPL window without the prompt text.</summary>
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

        public async Task ResetAsync(bool waitForPrompt = true)
        {
            await _host.ExecuteDteCommandAsync(DteReplResetCommand).ConfigureAwait(continueOnCapturedContext: false);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public async Task ShowAsync(bool waitForPrompt = true)
        {
            await _host.ExecuteDteCommandAsync(_dteViewCommand).ConfigureAwait(continueOnCapturedContext: false);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public async Task SubmitTextToReplAsync(string text, bool waitForPrompt = true)
        {
            _interactiveWindowWrapper.Submit(text);

            if (waitForPrompt)
            {
                await WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public Task WaitForReplPromptAsync()
            => IntegrationHelper.WaitForResultAsync(() => ReplText.EndsWith(ReplPromptText), expectedResult: true);
    }
}
