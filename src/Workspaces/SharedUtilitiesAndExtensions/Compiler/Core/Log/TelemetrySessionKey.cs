// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Identifies the logical telemetry session a measurement belongs to.
/// <para>
/// A host that runs several independent language servers in one process needs each server's
/// measurements bucketed - and posted - separately, so aggregation state is keyed by this. Today there
/// is exactly one session per process and this is always <see cref="Default"/>.
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
