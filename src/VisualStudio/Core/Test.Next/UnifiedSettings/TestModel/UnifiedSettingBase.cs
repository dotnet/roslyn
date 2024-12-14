// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel
{
    internal abstract record UnifiedSettingBase()
    {
        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("order")]
        public required int Order { get; init; }

        [JsonPropertyName("enableWhen")]
        public string? EnableWhen { get; init; }

        [JsonPropertyName("Migration")]
        public required Migration Migration { get; init; }
    }

    internal record UnifiedSettingsOption<T> : UnifiedSettingBase
    {
        [JsonPropertyName("Default")]
        public required T Default { get; init; }

        [JsonPropertyName("AlternativeDefault")]
        public AlternativeDefault<T>? AlternativeDefault { get; init; }
    }

    public record AlternativeDefault<T>
    {
        [JsonPropertyName("FlagName")]
        public required string FlagName { get; init; }

        [JsonPropertyName("Default")]
        public required T Default { get; init; }
    }

    internal record Migration
    {
        [JsonPropertyName("pass")]
        public required Pass Pass { get; init; }
    }

    internal record Pass
    {
        [JsonPropertyName("input")]
        public required Input Input { get; init; }
    }

    internal record Input
    {
        [JsonPropertyName("store")]
        public required string Store { get; init; }

        [JsonPropertyName("path")]
        public required string Path { get; init; }
    }
}
