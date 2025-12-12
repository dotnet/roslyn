// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.BuildTasks;

/// <summary>
/// Logs to both the MSBuild task's output and the inner compiler server logger.
/// </summary>
internal sealed class TaskCompilerServerLogger(
    TaskLoggingHelper taskLogger,
    ICompilerServerLogger inner)
    : ICompilerServerLogger
{
    private readonly TaskLoggingHelper _taskLogger = taskLogger;
    private readonly ICompilerServerLogger _inner = inner;

    public bool IsLogging => true;

    public void Log(string message)
    {
        _inner.Log(message);
        _taskLogger.LogMessage(message);
    }
}
