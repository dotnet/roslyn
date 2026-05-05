// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Telemetry;

internal sealed class NoOpTelemetryReporter : ITelemetryReporter
{
    public static readonly NoOpTelemetryReporter Instance = new();

    private NoOpTelemetryReporter()
    {
    }

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport)
        => TelemetryScope.Null;

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property)
        => TelemetryScope.Null;

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2)
        => TelemetryScope.Null;

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2, Property property3)
        => TelemetryScope.Null;

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, params ReadOnlySpan<Property> properties)
        => TelemetryScope.Null;

    public void ReportEvent(string name, Severity severity)
    {
    }

    public void ReportEvent(string name, Severity severity, Property property)
    {
    }

    public void ReportEvent(string name, Severity severity, Property property1, Property property2)
    {
    }

    public void ReportEvent(string name, Severity severity, Property property1, Property property2, Property property3)
    {
    }

    public void ReportEvent(string name, Severity severity, params ReadOnlySpan<Property> properties)
    {
    }

    public void ReportFault(Exception exception, string? message, params object?[] @params)
    {
    }

    public TelemetryScope TrackLspRequest(string lspMethodName, string lspServerName, TimeSpan minTimeToReport, Guid correlationId)
        => TelemetryScope.Null;

    public void ReportRequestTiming(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result)
    {
    }
}
