// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json.Serialization;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record EnumIntegerToString
{
    [JsonPropertyName("input")]
    public required Input Input { get; init; }

    [JsonPropertyName("map")]
    public required Map[] Map { get; init; }

    public bool Equals(EnumIntegerToString? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Input.Equals(other.Input) && Map.SequenceEqual(other.Map);
    }

    public override int GetHashCode()
    {
        return Hash.Combine(Input.GetHashCode(), Hash.CombineValues(Map));
    }
}
