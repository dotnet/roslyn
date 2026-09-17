// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Daemon;

internal static class TelemetryLevelResolver
{
    private const string CopilotTelemetryLevelEnvironmentVariable = "COPILOT_TELEMETRY_LEVEL";

    public static string? Resolve(string? telemetryLevel)
        => telemetryLevel ?? Environment.GetEnvironmentVariable(CopilotTelemetryLevelEnvironmentVariable);
}
