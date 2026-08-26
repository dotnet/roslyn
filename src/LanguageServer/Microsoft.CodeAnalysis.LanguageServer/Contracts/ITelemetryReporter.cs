// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Contracts.Telemetry;

internal interface ITelemetryReporter : IDisposable
{
    void InitializeSession(string telemetryLevel, string? sessionId, bool isDefaultSession);

    /// <summary>
    /// Posts an already-named telemetry event with already-final property names. Used by the Razor
    /// VS Code bridge (<c>ILanguageServerTelemetryReporterWrapper</c>), which owns no telemetry session
    /// of its own and forwards through this reporter. Roslyn's own FunctionId-based
    /// event pipeline does not go through here - it uses <c>TelemetryLogger</c> directly.
    /// </summary>
    void Log(string name, List<KeyValuePair<string, object?>> properties);
}
