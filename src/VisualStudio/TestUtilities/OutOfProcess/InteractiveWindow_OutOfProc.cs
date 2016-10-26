// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the interactive window in the Visual Studio host.
    /// </summary>
    public abstract class InteractiveWindow_OutOfProc : OutOfProcComponent
    {
        private readonly InteractiveWindow_InProc _inProc;

        internal InteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base (visualStudioInstance)
        {
            _inProc = CreateInProcComponent(visualStudioInstance);
        }

        internal abstract InteractiveWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance);

        public void Initialize()
        {
            _inProc.Initialize();
        }

        /// <summary>
        /// Gets the last output from the REPL.
        /// </summary>
        public string GetLastReplOutput()
        {
            return _inProc.GetLastReplOutput();
        }

        public string GetReplText()
        {
            return _inProc.GetReplText();
        }

        /// <summary>
        /// Gets the contents of the REPL window without the prompt text.
        /// </summary>
        public string GetReplTextWithoutPrompt()
        {
            return _inProc.GetReplTextWithoutPrompt();
        }

        public void ShowWindow(bool waitForPrompt = true)
        {
            _inProc.ShowWindow(waitForPrompt);
        }

        public void Reset(bool waitForPrompt = true)
        {
            _inProc.Reset(waitForPrompt);
        }

        public void SubmitText(string text, bool waitForPrompt = true)
        {
            _inProc.SubmitText(text, waitForPrompt);
        }

        public void WaitForReplOutput(string outputText)
        {
            _inProc.WaitForReplOutput(outputText);
        }

        public void CleanUpInteractiveWindow()
        {
            _inProc.CloseWindow();
        }
    }
}
