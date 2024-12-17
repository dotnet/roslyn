// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel
{
    internal abstract partial record UnifiedSettingBase()
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

        public static UnifiedSettingsOption<T> CreateOption<T>(
            IOption2 onboardedOption,
            string title,
            int order,
            T? defaultValue = default,
            (IOption2 featureFlagOption, T value) featureFlagAndExperimentValue = default,
            (IOption2 enableWhenOption, object whenValue) enableWhenOptionAndValue = default,
            string? languageName = null) where T : notnull
        {
            var migration = new Migration(new Pass { Input = new Input(onboardedOption, languageName) });
            var type = onboardedOption.Definition.Type;
            // If the option's type is nullable type, like bool?, we use bool in the registration file.
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is not null)
            {
                Assert.True(featureFlagAndExperimentValue.value is not default);
            }
            var nonNullableType = underlyingType ?? type;

            var alternativeDefault = featureFlagAndExperimentValue is not default
                ? new AlternativeDefault<T>(featureFlagAndExperimentValue.featureFlagOption, featureFlagAndExperimentValue.value)
                : null;

            var enableWhen = enableWhenOptionAndValue is not default
                ? $"config:{UnifiedSettingsTests.s_optionToUnifiedSettingPath[enableWhenOptionAndValue.enableWhenOption]}='{enableWhenOptionAndValue.whenValue}'"
                : null;

            var expectedDefault = defaultValue ?? onboardedOption.Definition.DefaultValue;
            // If the option default value is null, it means the option is in experiment mode and is hidden by a feature flag.
            // In Unified Settings it is not allowed and should be replaced by using the alternative default.
            // Like:
            //     "textEditor.csharp.intellisense.showNewSnippetExperience": {
            //         "type": "boolean",
            //         "default": false,
            //         "alternateDefault": {
            //             "flagName": "Roslyn.SnippetCompletion",
            //             "default": true
            //         }
            //      }
            // so please specify a non-null default value.
            Assert.NotNull(expectedDefault);

            return new UnifiedSettingsOption<T>
            {
                Title = title,
                Type = nonNullableType.ToString().ToCamelCase(),
                Order = order,
                EnableWhen = enableWhen,
                Migration = migration,
                AlternativeDefault = alternativeDefault,
                Default = (T)expectedDefault
            };
        }

        public static UnifiedSettingsEnumOption CreateEnumOption<T>(
            IOption2 onboardedOption,
            string title,
            int order,
            T? defaultValue,
            string[] enumLabels,
            T[]? enumValues = null,
            (IOption2 featureFlagOption, T value) featureFlagAndExperimentValue = default,
            (IOption2 enableWhenOption, object whenValue) enableWhenOptionAndValue = default,
            string? languageName = null) where T : Enum
        {
            var type = onboardedOption.Definition.Type;
            // If the option's type is nullable type, we use the original type in the registration file.
            var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
            Assert.Equal(typeof(T), nonNullableType);

            var expectedEnumValues = enumValues ?? Enum.GetValues(nonNullableType).Cast<T>().ToArray();
            var migration = new Migration(new EnumToInteger()
            {
                Input = new Input(onboardedOption, languageName),
                Map = new Map()
                {
                    EnumValueMatches = expectedEnumValues.SelectAsArray(value => new Map.EnumToValuePair { Result = value.ToString().ToCamelCase(), Match = value })
                }
            });

            var alternativeDefault = featureFlagAndExperimentValue is not default
                ? new AlternativeDefault<string>(featureFlagAndExperimentValue.featureFlagOption, featureFlagAndExperimentValue.value.ToString().ToCamelCase())
                : null;

            var enableWhen = enableWhenOptionAndValue is not default
                ? $"config:{UnifiedSettingsTests.s_optionToUnifiedSettingPath[enableWhenOptionAndValue.enableWhenOption]}='{enableWhenOptionAndValue.whenValue}'"
                : null;

            var expectedDefault = defaultValue ?? onboardedOption.Definition.DefaultValue;
            Assert.NotNull(expectedDefault);

            return new UnifiedSettingsEnumOption()
            {
                Title = title,
                Type = "string",
                Enum = expectedEnumValues.Select(value => value.ToString()).ToArray(),
                EnumLabel = enumLabels,
                Order = order,
                EnableWhen = enableWhen,
                Migration = migration,
                AlternativeDefault = alternativeDefault,
                Default = expectedDefault.ToString().ToCamelCase(),
            };
        }
    }
}
