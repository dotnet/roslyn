// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

/// <summary>
/// Exposed to provide an efficient checksum implementation.
/// Intended usage including caching responses w/o retaining potentially long strings.
/// </summary>
internal sealed class CopilotChecksumWrapper
{
    private readonly Checksum _checksum;

    private CopilotChecksumWrapper(Checksum checksum)
    {
        _checksum = checksum;
    }

    public static CopilotChecksumWrapper Create(ImmutableArray<string> values)
    {
        return new(Checksum.Create(values));
    }

    public bool Equals(CopilotChecksumWrapper? other)
    {
        return other != null && _checksum.Equals(other._checksum);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CopilotChecksumWrapper another)
            return false;

        return Equals(another);
    }

    public override int GetHashCode()
        => _checksum.GetHashCode();
}
