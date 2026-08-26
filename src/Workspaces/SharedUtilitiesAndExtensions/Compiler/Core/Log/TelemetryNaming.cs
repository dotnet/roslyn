// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Maps <see cref="FunctionId"/> onto the event and property names Roslyn's telemetry backend expects.
/// <para>
/// This is deliberately the only place the <c>vs/ide/vbcs/</c> naming convention appears. Sinks receive
/// already-final names, which is what lets a single sink implementation serve Roslyn (whose identity is
/// <see cref="FunctionId"/>) and Razor (whose identity is a plain string) without either knowing about
/// the other's naming.
/// </para>
/// </summary>
internal static class TelemetryNaming
{
    public const string EventPrefix = "vs/ide/vbcs/";
    public const string PropertyPrefix = "vs.ide.vbcs.";

    // these don't have concurrency limit on purpose to reduce chance of lock contention.
    // if that becomes a problem - by showing up in our perf investigation, then we will consider adding concurrency limit.
    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = [];
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = [];

    public static string GetEventName(FunctionId id)
        => s_eventMap.GetOrAdd(id, id => EventPrefix + GetTelemetryName(id, separator: '/'));

    public static string GetPropertyName(FunctionId id, string name)
        => s_propertyMap.GetOrAdd((id, name), key => PropertyPrefix + GetTelemetryName(key.id, separator: '.') + "." + key.name.ToLowerInvariant());

    private static string GetTelemetryName(FunctionId id, char separator)
        => Enum.GetName(typeof(FunctionId), id)!.Replace('_', separator).ToLowerInvariant();
}
