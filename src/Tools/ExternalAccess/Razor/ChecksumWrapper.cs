// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal readonly struct ChecksumWrapper(Checksum checksum) : IEquatable<ChecksumWrapper>
{
    private readonly Checksum _value = checksum;

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is ChecksumWrapper wrapper)
        {
            return Equals(wrapper);
        }
        return false;
    }

    public bool Equals(ChecksumWrapper other)
    {
        return _value.Equals(other._value);
    }

    public override string ToString()
        => _value.ToString();
}
