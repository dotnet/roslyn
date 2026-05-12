// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class TagHelperObject<T> : IEquatable<T>
    where T : TagHelperObject<T>
{
    private Checksum? _checksum;

    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public bool HasErrors
        => Diagnostics.Any(static d => d.Severity == RazorDiagnosticSeverity.Error);

    private protected TagHelperObject(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics.NullToEmpty();
    }

    internal Checksum Checksum
        => _checksum ??= ComputeChecksum();

    // Internal for benchmarks
    internal Checksum ComputeChecksum()
    {
        var builder = new Checksum.Builder();

        BuildChecksum(in builder);

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    private protected abstract void BuildChecksum(in Checksum.Builder builder);

    public sealed override bool Equals(object? obj)
        => obj is T other &&
           Equals(other);

    public bool Equals(T? other)
        => other is not null &&
           Checksum.Equals(other.Checksum);

    public sealed override int GetHashCode()
        => Checksum.GetHashCode();
}
