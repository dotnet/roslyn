// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.LanguageServer.Protocol;

internal partial class VSInternalKindAndModifier : IEquatable<VSInternalKindAndModifier>
{
    public static bool operator ==(VSInternalKindAndModifier? left, VSInternalKindAndModifier? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(VSInternalKindAndModifier? left, VSInternalKindAndModifier? right)
        => !(left == right);

    public override bool Equals(object? obj)
        => Equals(obj as VSInternalKindAndModifier);

    public bool Equals(VSInternalKindAndModifier? other)
    {
        if (other is null || !string.Equals(Kind, other.Kind, StringComparison.Ordinal) ||
            (Modifier is null ^ other.Modifier is null))
        {
            return false;
        }

        if (Modifier is not null && other.Modifier is not null && Modifier.Length == other.Modifier.Length)
        {
            for (var i = 0; i < Modifier.Length; i++)
            {
                if (!string.Equals(Modifier[i], other.Modifier[i], StringComparison.Ordinal))
                    return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = 1850642763;
        hashCode = (hashCode * -1521134295) + (Kind is null ? 0 : StringComparer.Ordinal.GetHashCode(Kind));
        if (Modifier is not null)
        {
            foreach (var modifier in Modifier)
            {
                if (modifier is not null)
                    hashCode = (hashCode * -1521134295) + StringComparer.Ordinal.GetHashCode(modifier);
            }
        }

        return hashCode;
    }
}
