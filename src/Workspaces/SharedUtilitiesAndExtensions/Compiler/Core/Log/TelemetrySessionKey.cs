// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Identifies the logical telemetry session a measurement belongs to.
/// <para>
/// Today there is exactly one per process and this is a constant. It exists so that aggregation state
/// is never keyed on the assumption of a single session: a host that runs several independent language
/// servers in one process (daemon mode) needs each server's measurements bucketed - and posted -
/// separately, and retrofitting that into a single-session aggregation table would be a breaking change
/// to the aggregation implementation rather than a configuration change.
/// </para>
/// <para>
/// See <see cref="RoslynTelemetry.CurrentSessionKey"/> for how a key is resolved at record time.
/// </para>
/// </summary>
internal readonly struct TelemetrySessionKey : IEquatable<TelemetrySessionKey>
{
    /// <summary>
    /// The key used when a host has not opted into per-session routing.
    /// </summary>
    public static readonly TelemetrySessionKey Default = new("default");

    public string Id { get; }

    public TelemetrySessionKey(string id)
        => Id = id;

    public bool Equals(TelemetrySessionKey other)
        => string.Equals(Id, other.Id, StringComparison.Ordinal);

    public override bool Equals(object? obj)
        => obj is TelemetrySessionKey other && Equals(other);

    public override int GetHashCode()
        => Id?.GetHashCode() ?? 0;

    public override string ToString()
        => Id ?? "";
}
