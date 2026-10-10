// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal interface ILogger
{
    void Log(LogLevel logLevel, string message, Exception? exception);

    bool IsEnabled(LogLevel logLevel);
}
