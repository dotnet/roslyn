// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Xunit;
using static Microsoft.VisualStudio.LanguageServices.Options.VisualStudioOptionStorage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record Input
{
    [JsonPropertyName("store")]
    public string Store { get; init; }

    [JsonPropertyName("path")]
    public string Path { get; init; }

    [JsonConstructor]
    public Input(string store, string path)
    {
        Store = store;
        Path = path;
    }

    public Input(IOption2 option, string? languageName = null)
    {
        Assert.False(option is IPerLanguageValuedOption && languageName is null);
        Store = GetStore(option);
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
            _ => string.Empty,
        };

        var optionStorage = Storages[option.Definition.ConfigName];
        return optionStorage switch
        {
            RoamingProfileStorage roamingProfile => roamingProfile.Key.Replace("%LANGUAGE%", languageId),
            LocalUserProfileStorage userProfileStorage => $"{userProfileStorage.Path}\\{userProfileStorage.Key}",
            _ => throw ExceptionUtilities.UnexpectedValue(option)
        };
    }
}
