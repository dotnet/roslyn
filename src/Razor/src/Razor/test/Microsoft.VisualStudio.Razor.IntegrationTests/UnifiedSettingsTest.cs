// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.Razor.LanguageClient.Options;
using Xunit;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class UnifiedSettingsTest
{
    [Fact]
    public void TestJsonIsValid()
    {
        var document = ReadRegistrationJson();
        Assert.NotNull(document);
    }

    [Fact]
    public void RegistrationListsAllSettingNames()
    {
        var document = ReadRegistrationJson();
        var properties = document.RootElement.GetProperty("properties");

        foreach (var setting in SettingsNames.AllSettings)
        {
            Assert.True(properties.TryGetProperty(setting, out _), $"Could not find setting '{setting}' in razor.registration.json");
        }
    }

    [Fact]
    public void SettingNamesListsAllProperties()
    {
        var document = ReadRegistrationJson();
        var properties = document.RootElement.GetProperty("properties");

        // iterate through properties and check that each one is in SettingsNames.AllSettings
        foreach (var property in properties.EnumerateObject())
        {
            var settingName = property.Name;
            Assert.Contains(settingName, SettingsNames.AllSettings);
        }
    }

    private static JsonDocument ReadRegistrationJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Microsoft.VisualStudio.Razor.IntegrationTests.razor.registration.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.False(string.IsNullOrEmpty(json));

        var options = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip
        };
        return JsonDocument.Parse(json, options);
    }
}
