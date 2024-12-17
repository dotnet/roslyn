// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

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

        public static UnifiedSettingsOption<T> Create<T>(
            IOption2 onboardedOption,
            string title,
            int order,
            T defaultValue,
            (IOption2 featureFlagOption, object value) featureFlagAndExperimentValue = default,
            (IOption2 enableWhenOption, object whenValue) enableWhenOptionAndValue = default,
            string? languageName = null)
        {
            var migration = new Migration
            {
                Pass = new Pass()
                {
                    Input = Input(onboardedOption, languageName)
                }
            };

            var type = onboardedOption.Definition.Type;
            // If the option's type is nullable type, like bool?, we use bool in the registration file.
            var underlyingType = Nullable.GetUnderlyingType(type);
            var nonNullableType = underlyingType ?? type;

            var alternativeDefault = featureFlagAndExperimentValue is not default
                ? new AlternativeDefault<T>(featureFlagAndExperimentValue.featureFlagOption, featureFlagAndExperimentValue.value)
                : null;

            var enableWhen = enableWhenOptionAndValue is not default
                ? $"config:{UnifiedSettingsTests.s_optionToUnifiedSettingPath[enableWhenOptionAndValue]}='{enableWhenOptionAndValue.whenValue}'"
                : null;

            return new UnifiedSettingsOption<T>()
            {
                Title = title,
                Type = nonNullableType.ToString().ToCamelCase(),
                Order = order,
                EnableWhen = enableWhen,
                Migration = migration,
                AlternativeDefault = alternativeDefault,
            };
        }
    }
}
