// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHostLogger(TextWriter output)
{
    private const string InformationLevel = "info";
    private const string WarningLevel = "warn";
    private const string CriticalLevel = "crit";
    private const string LogLevelPadding = ": ";

    public void LogInformation(string message)
        => LogMessage(InformationLevel, message);

    public void LogWarning(string message)
        => LogMessage(WarningLevel, message);

    public void LogCritical(string message)
        => LogMessage(CriticalLevel, message);

    private void LogMessage(string level, string message)
    {
        output.Write(level);
        output.Write(LogLevelPadding);
        output.Write(message);
        output.Write(Environment.NewLine);
    }
}
