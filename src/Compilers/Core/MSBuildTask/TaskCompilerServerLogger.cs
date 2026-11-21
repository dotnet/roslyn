// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.BuildTasks;

/// <summary>
/// Routes operational diagnostics to both the MSBuild task's output and the inner compiler server logger.
/// Detailed tracing is routed only to the inner logger.
/// </summary>
internal sealed class TaskCompilerServerLogger(
    TaskLoggingHelper taskLogger,
    ICompilerServerLogger inner)
    : ICompilerServerLogger
{
    private readonly TaskLoggingHelper _taskLogger = taskLogger;
    private readonly ICompilerServerLogger _inner = inner;

    public bool IsEnabled(CompilerServerLogKind kind)
        => kind == CompilerServerLogKind.Operational || _inner.IsEnabled(kind);

    public void Log(CompilerServerLogKind kind, string message)
    {
        if (_inner.IsEnabled(kind))
        {
            _inner.Log(kind, message);
        }

        if (kind == CompilerServerLogKind.Operational)
        {
            _taskLogger.LogMessage(message);
        }
    }
}
