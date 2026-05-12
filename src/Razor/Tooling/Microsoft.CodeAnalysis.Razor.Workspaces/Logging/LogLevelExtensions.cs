// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal static class LogLevelExtensions
{
    public static bool IsAtLeast(this LogLevel target, LogLevel logLevel)
    {
        return target >= logLevel && target != LogLevel.None;
    }

    public static bool IsAtMost(this LogLevel target, LogLevel logLevel)
    {
        return target <= logLevel || target == LogLevel.None;
    }
}
