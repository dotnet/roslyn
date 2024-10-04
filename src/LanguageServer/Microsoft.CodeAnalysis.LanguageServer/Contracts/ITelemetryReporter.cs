// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Contracts.Telemetry;

internal interface ITelemetryReporter : IDisposable
{
    void InitializeSession(string telemetryLevel, string? sessionId, bool isDefaultSession);
    void Log(string name, List<KeyValuePair<string, object?>> properties);
    void LogBlockStart(string eventName, int kind, int blockId);
    void LogBlockEnd(int blockId, List<KeyValuePair<string, object?>> properties, CancellationToken cancellationToken);
    void ReportFault(string eventName, string description, int logLevel, bool forceDump, int processId, Exception exception);
}
