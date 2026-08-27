// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// A destination for aggregated measurements. Implementations accumulate values in memory and post
/// them in batches when <see cref="Flush"/> is called.
/// <para>
/// The tag parameter mirrors <c>System.Diagnostics.Metrics.Counter&lt;T&gt;.Add</c>, so recording could
/// move onto BCL metric instruments without touching any call site.
/// </para>
/// </summary>
internal interface IMetricSink
{
    /// <summary>
    /// Adds <paramref name="delta"/> to a monotonically increasing counter.
    /// </summary>
    void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags);

    /// <summary>
    /// Records <paramref name="value"/> as one observation in a distribution.
    /// </summary>
    void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags);

    /// <summary>
    /// Posts everything accumulated so far and resets. Safe to call at any time and from any thread;
    /// hosts call it periodically, at shutdown, and whenever a logical session ends.
    /// </summary>
    void Flush();
}
