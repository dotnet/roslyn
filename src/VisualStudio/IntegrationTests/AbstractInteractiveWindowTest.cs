// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        protected readonly CSharpInteractiveWindow_OutOfProc _interactiveWindow;

        protected AbstractInteractiveWindowTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            _interactiveWindow = _visualStudio.Instance.CSharpInteractiveWindow;
            ClearInteractiveWindow();
        }

        protected void ClearInteractiveWindow()
        {
            _interactiveWindow.Initialize();
            _interactiveWindow.ShowWindow();
            _interactiveWindow.Reset();
        }

        protected void SubmitText(string text, bool waitForPrompt = true)
        {
            _interactiveWindow.SubmitText(text, waitForPrompt);
        }

        protected void VerifyLastReplOutput(string expectedReplOutput)
        {
            var lastReplOutput = _interactiveWindow.GetLastReplOutput();
            Assert.Equal(expectedReplOutput, lastReplOutput);
        }

        protected void VerifyLastReplOutputContains(string expectedReplOutput)
        {
            var lastReplOutput = _interactiveWindow.GetLastReplOutput();
            Assert.Contains(expectedReplOutput, lastReplOutput);
        }

        protected void VerifyLastReplOutputEndsWith(string expectedReplOutput)
        {
            var lastReplOutput = _interactiveWindow.GetLastReplOutput();
            Assert.EndsWith(expectedReplOutput, lastReplOutput);
        }

        protected void WaitForReplOutput(string outputText)
        {
            _interactiveWindow.WaitForReplOutput(outputText);
        }
    }
}
