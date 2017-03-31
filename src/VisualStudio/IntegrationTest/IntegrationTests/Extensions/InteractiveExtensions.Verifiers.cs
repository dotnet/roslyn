// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive
{
    public static partial class InteractiveExtensions
    {
        private static readonly char[] LineSeparators = { '\r', '\n' };

        public static void WaitForReplOutput(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForReplOutput(outputText);

        public static void WaitForLastReplOutputContains(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplOutputContains(outputText);

        public static void WaitForLastReplOutput(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplOutput(outputText);

        public static void WaitForLastReplInputContains(this AbstractInteractiveWindowTest test, string outputText)
              => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplInputContains(outputText);

        public static void VerifyLastReplInput(this AbstractInteractiveWindowTest test, string expectedReplInput)
        {
            var lastReplInput = test.InteractiveWindow.GetLastReplInput();
            Assert.Equal(expectedReplInput, lastReplInput);
        }

        public  static void VerifyReplPromptConsistency(this AbstractInteractiveWindowTest test, string prompt, string output)
        {
            var replText = test.InteractiveWindow.GetReplText();
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
    }
}