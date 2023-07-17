// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{
    internal sealed class DiagnosticFilter
    {
        public SuppressionDescriptor Descriptor { get; }
        public Action<DiagnosticFilteringRequest> Filter { get; }

        public DiagnosticFilter(SuppressionDescriptor descriptor, Action<DiagnosticFilteringRequest> filter)
        {
            Descriptor = descriptor;
            Filter = filter;
        }

    }

    internal record DiagnosticFilters(ImmutableArray<DiagnosticFilter> Filters)
    {
        public static DiagnosticFilters Empty { get; } =
            new(ImmutableArray<DiagnosticFilter>.Empty);

        public ImmutableDictionary<string, ImmutableArray<DiagnosticFilter>> FiltersByDiagnosticId =
            Filters.Select(f => new KeyValuePair<string, DiagnosticFilter>(f.Descriptor.SuppressedDiagnosticId, f))
                .GroupBy(p => p.Key)
                .ToImmutableDictionary(g => g.Key, g => g.Select(x => x.Value).ToImmutableArray(), StringComparer.OrdinalIgnoreCase);
    }

}
