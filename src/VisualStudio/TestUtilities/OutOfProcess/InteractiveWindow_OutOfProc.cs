// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the interactive window in the Visual Studio host.
    /// </summary>
    public abstract class InteractiveWindow_OutOfProc<TInProcComponent> : OutOfProcComponent<TInProcComponent>
        where TInProcComponent : InteractiveWindow_InProc
    {
        internal InteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base (visualStudioInstance)
        {
        }

        public void Initialize()
        {
            InProc.Initialize();
        }

        /// <summary>
        /// Gets the last output from the REPL.
        /// </summary>
        public string GetLastReplOutput()
        {
            return InProc.GetLastReplOutput();
        }

        public string GetReplText()
        {
            return InProc.GetReplText();
        }

        /// <summary>
        /// Gets the contents of the REPL window without the prompt text.
        /// </summary>
        public string GetReplTextWithoutPrompt()
        {
            return InProc.GetReplTextWithoutPrompt();
        }

        public void ShowWindow(bool waitForPrompt = true)
        {
            InProc.ShowWindow(waitForPrompt);
        }

        public void Reset(bool waitForPrompt = true)
        {
            InProc.Reset(waitForPrompt);
        }

        public void SubmitText(string text, bool waitForPrompt = true)
        {
            InProc.SubmitText(text, waitForPrompt);
        }

        public void WaitForReplOutput(string outputText)
        {
            InProc.WaitForReplOutput(outputText);
        }

        public void CleanUpInteractiveWindow()
        {
            InProc.CloseWindow();
        }
    }
}
