// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    partial class InteractiveWindow_InProc2
    {
        public class Verifier : Verifier<InteractiveWindow_InProc2>
        {
            private static readonly char[] LineSeparators = { '\r', '\n' };

            public Verifier(InteractiveWindow_InProc2 interactiveWindow)
                : base(interactiveWindow)
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
