// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Feature name used in telemetry.
/// </summary>
internal readonly struct TelemetryFeatureName
{
    private const string LocalKind = "Local";
    private const string RemoteKind = "Remote";
    private const string ExtensionKind = "Extension";

    // Local services:

    public static readonly TelemetryFeatureName CodeFixProvider = GetClientFeatureName("CodeFixProvider");
    public static readonly TelemetryFeatureName InlineRename = GetClientFeatureName("InlineRename");
    public static readonly TelemetryFeatureName LegacySuppressionFix = GetClientFeatureName("TelemetryFeatureName");
    public static readonly TelemetryFeatureName VirtualMemoryNotification = GetClientFeatureName("VirtualMemoryNotification");

    private readonly string _name;
    private readonly string _kind;

    private TelemetryFeatureName(string name, string kind)
    {
        _name = name;
        _kind = kind;
    }

    private static TelemetryFeatureName GetClientFeatureName(string name)
        => new(name, LocalKind);

    public static TelemetryFeatureName GetRemoteFeatureName(string componentName, string serviceName)
        => new(componentName + ":" + serviceName, RemoteKind);

    public static TelemetryFeatureName GetExtensionName(Type type)
        => new(type.Assembly.FullName?.StartsWith("Microsoft.", StringComparison.Ordinal) == true ? type.FullName! : "External",
               ExtensionKind);

    public override string ToString()
        => _kind + ":" + _name;
}
