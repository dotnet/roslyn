// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel
{
    internal abstract record UnifiedSettingBase()
    {
        [JsonPropertyName("title")]
        [JsonConverter(typeof(ResourceConverter))]
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
        [JsonPropertyName("flagName")]
        public string FlagName { get; }

        [JsonPropertyName("default")]
        public T Default { get; }

        public AlternativeDefault(IOption2 featureFlagOption, T defaultValue)
        {
            var optionStorage = Storages[featureFlagOption.Definition.ConfigName];
            Assert.IsType<FeatureFlagStorage>(optionStorage);
            FlagName = ((FeatureFlagStorage)optionStorage).FlagName;
            Default = defaultValue;
        }
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
        public string Store { get; }

        [JsonPropertyName("path")]
        public string Path { get; }

        public Input(IOption2 option, string? languageName = null)
        {
            Store = GetStore(option);
            Assert.True(option is IPerLanguageValuedOption && languageName is null);
            Path = GetPath(option, languageName);
        }

        private static string GetStore(IOption2 option)
        {
            var optionStorage = Storages[option.Definition.ConfigName];
            return optionStorage switch
            {
                RoamingProfileStorage => "SettingsManager",
                LocalUserProfileStorage => "VsUserSettingsRegistry",
                _ => throw ExceptionUtilities.Unreachable()
            };
        }

        private static string GetPath(IOption2 option, string? languageName)
        {
            var languageId = languageName switch
            {
                LanguageNames.CSharp => "CSharp",
                LanguageNames.VisualBasic => "VisualBasic",
                null => string.Empty,
            };

            var optionStorage = Storages[option.Definition.ConfigName];
            return option switch
            {
                RoamingProfileStorage roamingProfile => roamingProfile.Key.Replace("%LANGUAGE%", languageId),
                LocalUserProfileStorage userProfileStorage => $"{userProfileStorage.Path}\\{userProfileStorage.Key}"
                _ => throw ExceptionUtilities.UnexpectedValue(option)
            };
        }
    }
}
