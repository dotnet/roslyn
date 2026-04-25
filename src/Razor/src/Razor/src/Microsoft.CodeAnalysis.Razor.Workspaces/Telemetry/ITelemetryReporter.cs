// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Telemetry;

internal interface ITelemetryReporter
{
    TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport);
    TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property);
    TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2);
    TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2, Property property3);
    TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, params ReadOnlySpan<Property> properties);

    TelemetryScope TrackLspRequest(string lspMethodName, string lspServerName, TimeSpan minTimeToReport, Guid correlationId);

    void ReportEvent(string name, Severity severity);
    void ReportEvent(string name, Severity severity, Property property);
    void ReportEvent(string name, Severity severity, Property property1, Property property2);
    void ReportEvent(string name, Severity severity, Property property1, Property property2, Property property3);
    void ReportEvent(string name, Severity severity, params ReadOnlySpan<Property> properties);

    void ReportFault(Exception exception, string? message, params object?[] @params);

    /// <summary>
    /// Reports timing data for an lsp request
    /// </summary>
    /// <param name="name">The method name</param>
    /// <param name="language">The language for the request</param>
    /// <param name="queuedDuration">How long the request was in the queue before it was handled by code</param>
    /// <param name="requestDuration">How long it took to handle the request</param>
    /// <param name="result">The result of handling the request</param>
    void ReportRequestTiming(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result);
}
