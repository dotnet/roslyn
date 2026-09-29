// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Maps <see cref="FunctionId"/> onto the event and property names Roslyn's telemetry backend expects.
/// </summary>
internal static class TelemetryNaming
{
    private const string EventPrefix = "vs/ide/vbcs/";
    private const string PropertyPrefix = "vs.ide.vbcs.";

    // these don't have concurrency limit on purpose to reduce chance of lock contention.
    // if that becomes a problem - by showing up in our perf investigation, then we will consider adding concurrency limit.
    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = [];
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = [];

    public static string GetEventName(FunctionId id)
        => s_eventMap.GetOrAdd(id, id => EventPrefix + GetTelemetryName(id, separator: '/'));

    public static string GetPropertyName(FunctionId id, string name)
        => s_propertyMap.GetOrAdd((id, name), key => PropertyPrefix + GetTelemetryName(key.id, separator: '.') + "." + key.name.ToLowerInvariant());

    /// <summary>
    /// Derives the meter name (<c>vs.ide.vbcs.some.operation.meter</c>) from an event name already
    /// produced by <see cref="GetEventName"/> (<c>vs/ide/vbcs/some/operation</c>).
    /// </summary>
    public static string GetMeterName(string eventName)
        => eventName.Replace('/', '.') + ".meter";

    /// <summary>
    /// Derives a property name (<c>vs.ide.vbcs.some.operation.tagname</c>) from an event name already
    /// produced by <see cref="GetEventName"/>.
    /// </summary>
    public static string GetPropertyName(string eventName, string tagName)
        => eventName.Replace('/', '.') + "." + tagName.ToLowerInvariant();

    private static string GetTelemetryName(FunctionId id, char separator)
        => Enum.GetName(typeof(FunctionId), id)!.Replace('_', separator).ToLowerInvariant();
}
