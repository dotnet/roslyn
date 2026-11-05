// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.LanguageServer.Protocol;

internal partial class Position : IEquatable<Position>
{
    public Position()
    {
    }

    public Position(int line, int character)
    {
        Line = line;
        Character = character;
    }

    public static bool operator ==(Position? left, Position? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Position? left, Position? right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is Position other && Equals(other);

    public bool Equals(Position? other)
        => other is not null && Line == other.Line && Character == other.Character;

    public override int GetHashCode()
        => Line ^ Character;
}
