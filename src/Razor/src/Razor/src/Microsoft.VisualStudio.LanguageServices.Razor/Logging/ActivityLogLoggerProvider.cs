// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.Logging;

/// <summary>
/// An <see cref="ILoggerProvider"/> that logs any warnings or errors to the Visual Studio Activity Log.
/// </summary>
[ExportLoggerProvider(minimumLogLevel: LogLevel.Warning)]
[method: ImportingConstructor]
internal sealed partial class ActivityLogLoggerProvider(RazorActivityLog activityLog) : ILoggerProvider
{
    private readonly RazorActivityLog _activityLog = activityLog;

    public ILogger CreateLogger(string categoryName)
        => new Logger(_activityLog, categoryName);
}
