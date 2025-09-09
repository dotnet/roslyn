// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <param name="IncludedDiagnosticIds">
/// If present, if an analyzer has at least one descriptor in this set, it will be included.
/// Note: this set can include diagnostic IDs from multiple analyzers in it.
/// </param>
/// <param name="ExcludedDiagnosticIds">
/// If present, if all of the descriptors an analyzer has is in this set, it will be excluded.
/// Note: this set can include diagnostic IDs from multiple analyzers in it.
/// </param>
[DataContract]
internal readonly record struct DiagnosticIdFilter(
    [property: DataMember(Order = 0)] ImmutableHashSet<string>? IncludedDiagnosticIds,
    [property: DataMember(Order = 1)] ImmutableHashSet<string>? ExcludedDiagnosticIds)
{
    public static readonly DiagnosticIdFilter All = default;

    public static DiagnosticIdFilter Include(ImmutableHashSet<string>? includedDiagnosticIds)
        => new(includedDiagnosticIds, ExcludedDiagnosticIds: null);

    public static DiagnosticIdFilter Exclude(ImmutableHashSet<string> excludedDiagnosticIds)
        => new(IncludedDiagnosticIds: null, excludedDiagnosticIds);

    /// <summary>
    /// Checks the IDs from a single analyzer's <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> to see if it is
    /// allowed by this filter.  If this is <see cref="All"/>, this will always return true.  If there are any
    /// values in <see cref="IncludedDiagnosticIds"/>, at least one of the IDs must be in that set.  If there are any
    /// values in <see cref="ExcludedDiagnosticIds"/>, not all of the IDs can be in that set.
    /// </summary>
    public bool Allow(params IEnumerable<string> ids)
    {
        if (this == All)
            return true;

        foreach (var id in ids)
        {
            // If the ID is in the included set, then that's good enough as the semantics for this type are that as long
            // as we see one allowed id, we allow the analyzer.
            if (IncludedDiagnosticIds != null &&
                IncludedDiagnosticIds.Contains(id))
            {
                return true;
            }

            // If the ID is *not* in the excluded set, then that's good enough as the semantics for this type are that we
            // only filter out the analyzers that have *all* their IDs in the excluded set.
            if (ExcludedDiagnosticIds != null &&
                !ExcludedDiagnosticIds.Contains(id))
            {
                return true;
            }
        }

        return false;
    }
}
