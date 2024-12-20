// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal record UnifiedSettingsEnumOption : UnifiedSettingsOption<string>
{
    [JsonPropertyName("enum")]
    public required string[] @Enum { get; init; }

    [JsonPropertyName("enumItemLabels")]
    [JsonConverter(typeof(ResourceStringArrayConverter))]
    public required string[] EnumItemLabels { get; init; }

    public virtual bool Equals(UnifiedSettingsEnumOption? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return base.Equals(other) && Enum.Equals(other.Enum) && EnumItemLabels.Equals(other.EnumItemLabels);
    }

    public override int GetHashCode()
    {
        return Hash.Combine(base.GetHashCode(), Enum, EnumItemLabels);
    }
}
