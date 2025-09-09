// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <param name="IncludedDiagnosticIds">If present, if an analyzer has at least one descriptor in this set, it will be included.</param>
/// <param name="ExcludedDiagnosticIds">If present, if all of the descriptors an analyzer has is in this set, it will be excluded.</param>
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
