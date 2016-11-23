// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        private const string Edit_SelectionCancelCommand = "Edit.SelectionCancel";

        private static readonly char[] LineSeparators = { '\r', '\n' };

        protected readonly CSharpInteractiveWindow_OutOfProc InteractiveWindow;

        protected AbstractInteractiveWindowTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            InteractiveWindow = VisualStudio.Instance.CSharpInteractiveWindow;
            ClearInteractiveWindow();
        }

        protected void ClearInteractiveWindow()
        {
            InteractiveWindow.Initialize();
            InteractiveWindow.ShowWindow();
            InteractiveWindow.Reset();
        }

        protected void ClearReplText()
        {
            // Dismiss the pop-up (if any)
            VisualStudio.Instance.ExecuteCommand(Edit_SelectionCancelCommand);

            // Clear the line
            VisualStudio.Instance.ExecuteCommand(Edit_SelectionCancelCommand);
        }

        protected void Reset(bool waitForPrompt = true)
        {
            InteractiveWindow.Reset(waitForPrompt: true);
        }

        protected void SubmitText(string text, bool waitForPrompt = true)
        {
            InteractiveWindow.SubmitText(text, waitForPrompt);
        }

        protected void VerifyLastReplOutput(string expectedReplOutput)
        {
            var lastReplOutput = InteractiveWindow.GetLastReplOutput();
            Assert.Equal(expectedReplOutput, lastReplOutput);
        }

        protected void VerifyLastReplOutputContains(string expectedReplOutput)
        {
            var lastReplOutput = InteractiveWindow.GetLastReplOutput();
            Assert.Contains(expectedReplOutput, lastReplOutput);
        }

        protected void VerifyLastReplOutputEndsWith(string expectedReplOutput)
        {
            var lastReplOutput = InteractiveWindow.GetLastReplOutput();
            Assert.EndsWith(expectedReplOutput, lastReplOutput);
        }

        protected void VerifyReplPromptConsistency(string prompt, string output)
        {
            var replText = InteractiveWindow.GetReplText();
            var replTextLines = replText.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var replTextLine in replTextLines)
            {
                if (!replTextLine.Contains(prompt))
                {
                    continue;
                }

                // The prompt must be at the beginning of the line
                Assert.StartsWith(prompt, replTextLine);

                var promptIndex = replTextLine.IndexOf(prompt, prompt.Length);

                // A 'subsequent' prompt is only allowed on a line containing #prompt
                if (promptIndex >= 0)
                {
                    Assert.StartsWith(prompt + "#prompt", replTextLine);
                    Assert.False(replTextLine.IndexOf(prompt, promptIndex + prompt.Length) >= 0);
                }

                // There must be no output on a prompt line.
                Assert.DoesNotContain(output, replTextLine);
            }
        }

        protected void WaitForReplOutput(string outputText)
        {
            InteractiveWindow.WaitForReplOutput(outputText);
        }
    }
}
