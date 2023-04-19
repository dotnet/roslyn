// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the interactive window in the Visual Studio host.
    /// </summary>
    public abstract partial class InteractiveWindow_OutOfProc : TextViewWindow_OutOfProc
    {
        public class Verifier : Verifier<InteractiveWindow_OutOfProc>
        {
            private static readonly char[] LineSeparators = { '\r', '\n' };

            public Verifier(InteractiveWindow_OutOfProc interactiveWindow, VisualStudioInstance instance)
                : base(interactiveWindow, instance)
            {
            }

            public void LastReplInput(string expectedReplInput)
            {
                var lastReplInput = _textViewWindow.GetLastReplInput();
                Assert.Equal(expectedReplInput, lastReplInput);
            }

            public void ReplPromptConsistency(string prompt, string output)
            {
                var replText = _textViewWindow.GetReplText();
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
}
