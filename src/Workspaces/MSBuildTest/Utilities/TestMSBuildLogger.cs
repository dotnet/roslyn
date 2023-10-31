// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Roslyn.Utilities;
internal class TestMSBuildLogger : ILogger
{
    public LoggerVerbosity Verbosity { get; set; }
    public string Parameters { get; set; } = string.Empty;

    public bool WasInitialized = false;

    private readonly List<string> _logLines = new List<string>();

    public void Initialize(IEventSource eventSource)
    {
        WasInitialized = true;
        eventSource.AnyEventRaised += EventSource_AnyEventRaised;
    }

    public void Shutdown()
    {
    }

    public List<string> GetLogLines()
    {
        Contract.ThrowIfFalse(WasInitialized);
        return _logLines;
    }

    private void EventSource_AnyEventRaised(object sender, BuildEventArgs e)
    {
        _logLines.Add(e.Message);
    }
}
