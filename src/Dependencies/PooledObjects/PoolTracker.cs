// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;

#if DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
#endif

namespace Microsoft.CodeAnalysis.PooledObjects;

#if DEBUG
/// <summary>
/// Tracks outstanding pooled object allocations, enabling per-test leak detection.
/// When tracking is active (via <see cref="StartTracking"/>), every <see cref="ObjectPool{T}.Allocate"/>
/// registers the instance and every free/forget removes it. After the test completes,
/// <see cref="PoolTrackingContext.HasLeaks"/> reveals whether any pooled objects were not returned.
/// All tracking is DEBUG-only and compiles out in Release builds.
/// </summary>
#endif
internal static class PoolTracker
{
#if DEBUG
    // AsyncLocal so tracking flows through async test methods and their continuations.
    private static readonly AsyncLocal<PoolTrackingContext?> s_currentContext = new AsyncLocal<PoolTrackingContext?>();

    // Fast check to avoid reading AsyncLocal when no tracking session is active.
    private static int s_activeTrackers;

    /// <summary>
    /// Begins tracking pooled object allocations on the current async flow.
    /// Returns the context that should later be inspected for leaks.
    /// </summary>
    /// <param name="traceLeaks">When true, allocation stack traces are captured for diagnostics.</param>
    internal static void StartTracking(out PoolTrackingContext context, bool traceLeaks = false)
    {
        context = new PoolTrackingContext(traceLeaks);
        s_currentContext.Value = context;
        Interlocked.Increment(ref s_activeTrackers);
    }

    /// <summary>
    /// Stops tracking pooled object allocations on the current async flow.
    /// </summary>
    internal static void StopTracking()
    {
        s_currentContext.Value = null;
        Interlocked.Decrement(ref s_activeTrackers);
    }
#endif

    /// <summary>
    /// Records that a pooled object has been allocated.
    /// </summary>
    [Conditional("DEBUG")]
    internal static void OnAllocate(object obj, string? poolName = null)
    {
#if DEBUG
        if (s_activeTrackers > 0)
        {
            s_currentContext.Value?.OnAllocate(obj, poolName);
        }
#endif
    }

    /// <summary>
    /// Records that a pooled object has been freed / forgotten.
    /// </summary>
    [Conditional("DEBUG")]
    internal static void OnFree(object obj)
    {
#if DEBUG
        if (s_activeTrackers > 0)
        {
            s_currentContext.Value?.OnFree(obj);
        }
#endif
    }

    /// <summary>
    /// Forgives all currently outstanding pooled object allocations, treating them as non-leaks.
    /// Use this when an exception path is known to abandon pooled objects and that is acceptable
    /// (e.g., MissingPredefinedMember exception unwinding through intermediate lowering methods).
    /// </summary>
    [Conditional("DEBUG")]
    internal static void ForgiveLeaks()
    {
#if DEBUG
        if (s_activeTrackers > 0)
        {
            s_currentContext.Value?.ForgiveLeaks();
        }
#endif
    }
}

#if DEBUG
/// <summary>
/// Holds the set of outstanding pooled object allocations for a single tracking session (typically one test).
/// </summary>
internal sealed class PoolTrackingContext
{
    private readonly ConcurrentDictionary<object, AllocationInfo> _outstanding = new ConcurrentDictionary<object, AllocationInfo>(ReferenceEqualityComparer.Instance);
    private readonly bool _traceLeaks;

    internal PoolTrackingContext(bool traceLeaks)
    {
        _traceLeaks = traceLeaks;
    }

    internal void OnAllocate(object obj, string? poolName)
    {
        _outstanding.TryAdd(obj, new AllocationInfo(obj.GetType(), poolName, _traceLeaks ? Environment.StackTrace : null));
    }

    internal void OnFree(object obj)
    {
        _outstanding.TryRemove(obj, out _);
    }

    /// <summary>
    /// Returns true if there are pooled objects that were allocated but never freed.
    /// </summary>
    internal bool HasLeaks => !_outstanding.IsEmpty;

    /// <summary>
    /// Clears all outstanding allocations, forgiving any current leaks.
    /// </summary>
    internal void ForgiveLeaks()
    {
        _outstanding.Clear();
    }

    /// <summary>
    /// Returns a human-readable summary of leaked pooled objects, grouped by type with counts
    /// and allocation stack traces.
    /// </summary>
    internal string GetLeakSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Pool leak detected! The following pooled objects were not returned:");

        foreach (var group in _outstanding.Values.GroupBy(v => (v.Type, v.PoolName)).OrderByDescending(g => g.Count()))
        {
            var poolInfo = group.Key.PoolName is not null ? $" (from {group.Key.PoolName})" : "";
            sb.AppendLine($"  {group.Key.Type}{poolInfo}: {group.Count()}");

            foreach (var info in group)
            {
                if (info.StackTrace != null)
                {
                    sb.AppendLine($"    Allocation stack trace:");
                    sb.AppendLine($"    {info.StackTrace}");
                }
            }
        }

        return sb.ToString();
    }

    private readonly struct AllocationInfo(Type type, string? poolName, string? stackTrace)
    {
        public readonly Type Type = type;
        public readonly string? PoolName = poolName;
        public readonly string? StackTrace = stackTrace;
    }
}
#endif
