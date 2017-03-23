// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the interactive window in the Visual Studio host.
    /// </summary>
    public abstract class InteractiveWindow_OutOfProc : TextViewWindow_OutOfProc
    {
        private readonly InteractiveWindow_InProc _interactiveWindowInProc;

        internal InteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base (visualStudioInstance)
        {
            _interactiveWindowInProc = (InteractiveWindow_InProc)_textViewWindowInProc;
        }

        public void Initialize()
            => _interactiveWindowInProc.Initialize();

        /// <summary>
        /// Gets the last output from the REPL.
        /// </summary>
        public string GetLastReplOutput()
            => _interactiveWindowInProc.GetLastReplOutput();

        /// <summary>
        /// Gets the last input from the REPL.
        /// </summary>
        public string GetLastReplInput()
            => _interactiveWindowInProc.GetLastReplInput();

        public string GetReplText()
            => _interactiveWindowInProc.GetReplText();

        /// <summary>
        /// Gets the contents of the REPL window without the prompt text.
        /// </summary>
        public string GetReplTextWithoutPrompt()
            => _interactiveWindowInProc.GetReplTextWithoutPrompt();

        public void ShowWindow(bool waitForPrompt = true)
            => _interactiveWindowInProc.ShowWindow(waitForPrompt);

        public void Reset(bool waitForPrompt = true)
            => _interactiveWindowInProc.Reset(waitForPrompt);

        public void SubmitText(string text, bool waitForPrompt = true)
            => _interactiveWindowInProc.SubmitText(text, waitForPrompt);

        public void WaitForReplOutput(string outputText)
            => _interactiveWindowInProc.WaitForReplOutput(outputText);

        public void WaitForReplOutputContains(string outputText)
            => _interactiveWindowInProc.WaitForReplOutputContains(outputText);

        public void CloseInteractiveWindow()
            => _interactiveWindowInProc.CloseWindow();

        public void ClearScreen()
            => _interactiveWindowInProc.ClearScreen();

        public void InsertCode(string text)
            => _interactiveWindowInProc.InsertCode(text);

        public bool IsRunning
            => _interactiveWindowInProc.IsRunning;

        public void ExecuteActionAndWaitReadyForInput(Action action)
        {
            //   To be sure we do not overlap with the previous call
            while (_interactiveWindowInProc.IsRunning)
            {
                Task.Delay(50);
            }

            _interactiveWindowInProc.SubscribeForReadyForInput();

            bool readyForInputFlag = false;

            EventHandler flagResetAction = null;
            flagResetAction = (object sender, EventArgs args) =>
            {
                _interactiveWindowInProc.ReadyForInput -= flagResetAction;
                readyForInputFlag = true;
            };

            action();
            while (!readyForInputFlag)
            {
                Task.Delay(50);
            }

            _interactiveWindowInProc.UnsubscribeFromReadyForInput();
        }
    }
}