// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal partial class InteractiveWindowVerifierInProcess : ITextViewWindowVerifierInProcess
{
    private static readonly char[] s_lineSeparators = ['\r', '\n'];

    TestServices ITextViewWindowVerifierInProcess.TestServices => TestServices;

    ITextViewWindowInProcess ITextViewWindowVerifierInProcess.TextViewWindow => TestServices.InteractiveWindow;

    public async Task ReplPromptConsistencyAsync(string prompt, string output, CancellationToken cancellationToken)
    {
        var replText = await TestServices.InteractiveWindow.GetReplTextAsync(cancellationToken);
        var replTextLines = replText.Split(s_lineSeparators, StringSplitOptions.RemoveEmptyEntries);

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
