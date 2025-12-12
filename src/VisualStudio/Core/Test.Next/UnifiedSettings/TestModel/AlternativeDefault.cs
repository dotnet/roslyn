// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Options;
using Xunit;
using static Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record AlternativeDefault<T>
{
    [JsonPropertyName("flagName")]
    public string FlagName { get; init; }

    [JsonPropertyName("default")]
    public T Default { get; init; }

    [JsonConstructor]
    public AlternativeDefault(string flagName, T @default)
    {
        FlagName = flagName;
        Default = @default;
    }

    public AlternativeDefault(IOption2 featureFlagOption, T defaultValue)
    {
        var optionStorage = Storages[featureFlagOption.Definition.ConfigName];
        Assert.IsType<FeatureFlagStorage>(optionStorage);
        FlagName = ((FeatureFlagStorage)optionStorage).FlagName;
        Default = defaultValue;
    }
}
