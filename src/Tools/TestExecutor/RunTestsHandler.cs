// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestExecutor;

internal class RunTestsHandler : ITestRunEventsHandler
{
    public Action<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>? HandleCompletion { get; init; }

    public Action<TestMessageLevel, string>? HandleLog { get; init; }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        Contract.Assert(HandleLog != null);
        HandleLog(level, message);
    }

    public void HandleRawMessage(string rawMessage)
    {
    }

    public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
    {
        Contract.Assert(HandleCompletion != null);
        HandleCompletion(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
    {
    }

    public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
    {
        // This is not used in vstest 17.2+;
        return -1;
    }
}
