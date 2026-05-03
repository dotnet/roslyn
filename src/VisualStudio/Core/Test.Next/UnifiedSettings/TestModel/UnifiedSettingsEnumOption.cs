// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json.Serialization;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record UnifiedSettingsEnumOption : UnifiedSettingsOption<string>
{
    [JsonPropertyName("enum")]
    public required string[] @Enum { get; init; }

    [JsonPropertyName("enumItemLabels")]
    [JsonConverter(typeof(ResourceStringArrayConverter))]
    public required string[] EnumItemLabels { get; init; }

    public bool Equals(UnifiedSettingsEnumOption? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return base.Equals(other) && @Enum.SequenceEqual(other.@Enum) && EnumItemLabels.SequenceEqual(other.EnumItemLabels);
    }

    public override int GetHashCode()
    {
        return Hash.Combine(Hash.Combine(base.GetHashCode(), Hash.CombineValues(@Enum)), Hash.CombineValues(EnumItemLabels));
    }
}
