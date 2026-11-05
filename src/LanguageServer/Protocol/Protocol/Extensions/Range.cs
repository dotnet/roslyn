// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Roslyn.LanguageServer.Protocol;

internal partial class Range : IEquatable<Range>
{
    public static bool operator ==(Range? left, Range? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Range? left, Range? right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is Range other && Equals(other);

    public bool Equals(Range? other)
        => other is not null &&
           EqualityComparer<Position>.Default.Equals(Start, other.Start) &&
           EqualityComparer<Position>.Default.Equals(End, other.End);

    public override int GetHashCode()
    {
        var hashCode = -1676728671;
        hashCode = (hashCode * -1521134295) + EqualityComparer<Position>.Default.GetHashCode(Start);
        return (hashCode * -1521134295) + EqualityComparer<Position>.Default.GetHashCode(End);
    }
}
