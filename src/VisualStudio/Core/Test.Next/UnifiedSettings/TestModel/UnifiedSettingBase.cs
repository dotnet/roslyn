// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json.Serialization;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal abstract record UnifiedSettingBase
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(ResourceStringConverter))]
    public required string Title { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("order")]
    public required int Order { get; init; }

    [JsonPropertyName("enableWhen")]
    public string? EnableWhen { get; init; }

    [JsonPropertyName("migration")]
    public required Migration Migration { get; init; }

    [JsonPropertyName("messages")]
    public Message[]? Messages { get; init; }

    public virtual bool Equals(UnifiedSettingBase? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Title != other.Title
            || Type != other.Type
            || Order != other.Order
            || EnableWhen != other.EnableWhen
            || !Migration.Equals(other.Migration))
        {
            return false;
        }

        if (Messages is not null && other.Messages is not null)
        {
            return Messages.SequenceEqual(other.Messages);
        }

        return true;
    }

    public override int GetHashCode()
        => Hash.Combine(Hash.Combine(Hash.Combine(Hash.Combine(Hash.Combine(Title.GetHashCode(), Type.GetHashCode()), Order.GetHashCode()), EnableWhen?.GetHashCode() ?? 0), Migration.GetHashCode()), Messages is null ? 0 : Hash.CombineValues(Messages));
}
