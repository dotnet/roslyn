// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.Logging;

internal sealed partial class ActivityLogLoggerProvider
{
    private sealed class Logger(RazorActivityLog activityLog, string categoryName) : ILogger
    {
        private readonly RazorActivityLog _activityLog = activityLog;
        private readonly string _categoryName = categoryName;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel.IsAtLeast(LogLevel.Warning);

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            switch (logLevel)
            {
                case LogLevel.Error or LogLevel.Critical:
                    _activityLog.LogError(GetLogMessage(message, exception));
                    break;

                case LogLevel.Warning:
                    _activityLog.LogWarning(GetLogMessage(message, exception));
                    break;
            }

            string GetLogMessage(string message, Exception? exception)
            {
                using var _ = StringBuilderPool.GetPooledObject(out var builder);
                builder.Append($"[{_categoryName}] {message}");

                if (exception is not null)
                {
                    builder.AppendLine();
                    builder.Append(exception);
                }

                return builder.ToString();
            }
        }
    }
}
