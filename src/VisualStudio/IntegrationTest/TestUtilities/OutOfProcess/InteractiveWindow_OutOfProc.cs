// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the interactive window in the Visual Studio host.
    /// </summary>
    public abstract partial class InteractiveWindow_OutOfProc : TextViewWindow_OutOfProc
    {
        private readonly InteractiveWindow_InProc _interactiveWindowInProc;
        private readonly VisualStudioInstance _instance;

        public new Verifier Verify { get; }

        internal InteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _interactiveWindowInProc = (InteractiveWindow_InProc)_textViewWindowInProc;
            Verify = new Verifier(this, visualStudioInstance);
        }

        public void Initialize()
            => _interactiveWindowInProc.Initialize();

        /// <summary>
        /// Gets the last input from the REPL.
        /// </summary>
        public string GetLastReplInput()
            => _interactiveWindowInProc.GetLastReplInput();

        public string GetReplText()
            => _interactiveWindowInProc.GetReplText();

        public void ClearReplText()
        {
            // Dismiss the pop-up (if any)
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);

            // Clear the line
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
        }

        public void ShowWindow(bool waitForPrompt = true)
            => _interactiveWindowInProc.ShowWindow(waitForPrompt);

        public void Reset(bool waitForPrompt = true)
            => _interactiveWindowInProc.Reset(waitForPrompt);

        public void SubmitText(string text)
            => _interactiveWindowInProc.SubmitText(text);

        public void WaitForReplOutput(string outputText)
            => _interactiveWindowInProc.WaitForReplOutput(outputText);

        public void WaitForLastReplOutputContains(string outputText)
            => _interactiveWindowInProc.WaitForLastReplOutputContains(outputText);

        public void WaitForLastReplOutput(string outputText)
            => _interactiveWindowInProc.WaitForLastReplOutput(outputText);

        public void WaitForLastReplInputContains(string outputText)
            => _interactiveWindowInProc.WaitForLastReplInputContains(outputText);

        public void CloseInteractiveWindow()
            => _interactiveWindowInProc.CloseWindow();

        public void ClearScreen()
            => _interactiveWindowInProc.ClearScreen();

        public void InsertCode(string text)
            => _interactiveWindowInProc.InsertCode(text);
    }
}
