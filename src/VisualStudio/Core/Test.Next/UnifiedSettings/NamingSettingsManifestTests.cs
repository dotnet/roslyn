// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.VisualBasic.UnifiedSettings;
using Microsoft.VisualStudio.Shell;
using Moq;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsManifestTests
{
    [Fact]
    public void ResetRequiresConfirmation()
    {
        var notificationService = new Mock<INotificationService>(MockBehavior.Strict);
        notificationService.Setup(service => service.ConfirmMessageBox(
            ServicesVSResources.Reset_naming_preferences_confirmation,
            ServicesVSResources.Reset_naming_preferences,
            NotificationSeverity.Warning)).Returns(false);

        Assert.False(NamingSettingsPackage.ConfirmReset(notificationService.Object));
        notificationService.VerifyAll();
    }

    [Theory]
    [InlineData("csharpNamingSettings.registration.json", "csharp", typeof(CSharpNamingSettingsPackage), CSharpNamingSettingsPackage.ProviderServiceId, CSharpNamingSettingsPackage.MigratorServiceId, "294FBC9C-EF70-4AA0-BD4F-EB0C6A5908D7")]
    [InlineData("visualBasicNamingSettings.registration.json", "basic", typeof(VisualBasicNamingSettingsPackage), VisualBasicNamingSettingsPackage.ProviderServiceId, VisualBasicNamingSettingsPackage.MigratorServiceId, "BCA454E0-95E4-4877-B4CB-B1D642B7BAFA")]
    public void NativeDefaultsAndExternalCallbacksMatchTheCompleteLegacyGraph(string file, string language, Type package, string provider, string migrator, string legacyPage)
    {
        using var document = Load(file);
        var prefix = "languages." + language + ".codeStyle.naming.";
        var settings = document.RootElement.GetProperty("properties");
        Assert.Equal(4, settings.EnumerateObject().Count());
        var arrays = new[] { "symbolSpecifications", "namingStyles", "rules" }.Select(name => settings.GetProperty(prefix + name)).ToArray();
        foreach (var array in arrays)
        {
            Assert.Equal("array", array.GetProperty("type").GetString());
            Assert.Equal("false", array.GetProperty("visibleWhen").GetString());
            Assert.False(array.GetProperty("transientScopesSupported").GetBoolean());
            Assert.False(array.GetProperty("allowItemEditing").GetBoolean());
            Assert.False(array.GetProperty("allowAdditionsAndRemovals").GetBoolean());
            Assert.Equal(package.GUID, Guid.Parse(array.GetProperty("migrationCallback").GetProperty("packageId").GetString()!));
            Assert.Equal(Guid.Parse(migrator), Guid.Parse(array.GetProperty("migrationCallback").GetProperty("serviceId").GetString()!));
        }
        var graph = new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(ReadDefaults(arrays[0])), ReadDefaults(arrays[1]), ReadDefaults(arrays[2]));
        Assert.Equal(NamingStylePreferences.Default, graph.ToPreferences());
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(graph));
        Assert.All(graph.SymbolSpecifications.Concat(graph.NamingStyles), item => Assert.False(item.ContainsKey("legacyId")));
        var editor = settings.GetProperty(prefix + "editor");
        Assert.Equal("external", editor.GetProperty("type").GetString());
        Assert.True(editor.GetProperty("realtimeNotifications").GetBoolean());
        var rulesEditor = editor.GetProperty("properties").GetProperty("rules");
        var externalArrays = editor.GetProperty("properties");
        Assert.Equal(new[] { "symbolSpecifications", "namingStyles", "rules" }, externalArrays.EnumerateObject().Select(property => property.Name));
        foreach (var property in externalArrays.EnumerateObject())
        {
            Assert.Equal("array", property.Value.GetProperty("type").GetString());
            Assert.Equal("object", property.Value.GetProperty("items").GetProperty("type").GetString());
            Assert.True(property.Value.GetProperty("allowItemEditing").GetBoolean());
            Assert.True(property.Value.GetProperty("allowAdditionsAndRemovals").GetBoolean());
            Assert.False(property.Value.GetProperty("items").GetProperty("properties").TryGetProperty("id", out _));
            Assert.False(property.Value.GetProperty("items").GetProperty("properties").TryGetProperty("legacyId", out _));
            if (property.Name != "rules")
                Assert.False(property.Value.GetProperty("items").GetProperty("properties").GetProperty("name").TryGetProperty("minLength", out _));
        }
        var specificationFields = externalArrays.GetProperty("symbolSpecifications").GetProperty("items").GetProperty("properties");
        foreach (var field in new[] { "symbolKinds", "accessibilities", "requiredModifiers" })
        {
            Assert.Equal("array", specificationFields.GetProperty(field).GetProperty("type").GetString());
            Assert.True(specificationFields.GetProperty(field).GetProperty("uniqueItems").GetBoolean());
            Assert.Equal("string", specificationFields.GetProperty(field).GetProperty("items").GetProperty("type").GetString());
            Assert.NotEmpty(specificationFields.GetProperty(field).GetProperty("items").GetProperty("enum").EnumerateArray());
        }
        Assert.True(rulesEditor.GetProperty("allowItemEditing").GetBoolean());
        Assert.True(rulesEditor.GetProperty("allowAdditionsAndRemovals").GetBoolean());
        Assert.True(rulesEditor.GetProperty("itemsAreOrdered").GetBoolean());
        Assert.Equal(Guid.Parse(provider), Guid.Parse(editor.GetProperty("callback").GetProperty("serviceId").GetString()!));
        Assert.Equal(package.GUID, Guid.Parse(editor.GetProperty("callback").GetProperty("packageId").GetString()!));
        Assert.Equal(new[] { 258 }, editor.GetProperty("commands").EnumerateArray().Select(command => command.GetProperty("vsct").GetProperty("id").GetInt32()));
        Assert.Equal(Guid.Parse(legacyPage), Guid.Parse(document.RootElement.GetProperty("categories").GetProperty(prefix.TrimEnd('.')).GetProperty("legacyOptionPageId").GetString()!));
        Assert.Equal(@"UnifiedSettings\" + file, Assert.Single(package.GetCustomAttributes<ProvideSettingsManifestAttribute>()).PackageRelativeManifestFile);
        Assert.Equal(2, package.GetCustomAttributes<ProvideServiceAttribute>().Count());
        CheckResources(document.RootElement);
    }

    [Theory]
    [InlineData(typeof(CSharpNamingSettingsPackage), "CSharpNamingSettingsCommands.vsct")]
    [InlineData(typeof(VisualBasicNamingSettingsPackage), "VisualBasicNamingSettingsCommands.vsct")]
    public void CommandsRouteToColdCallbackPackage(Type package, string resource)
    {
        Assert.Single(package.GetCustomAttributes<ProvideMenuResourceAttribute>());
        using var stream = typeof(NamingSettingsManifestTests).Assembly.GetManifestResourceStream(resource)!;
        var table = XDocument.Load(stream);
        var ns = table.Root!.Name.Namespace;
        var symbol = Assert.Single(table.Descendants(ns + "GuidSymbol"));
        Assert.Equal(package.GUID, Guid.Parse((string)symbol.Attribute("value")!));
        Assert.Equal(new[] { "0x0102" }, symbol.Elements(ns + "IDSymbol").Select(id => (string?)id.Attribute("value")));
        Assert.Equal((string?)symbol.Attribute("name"), (string?)table.Root.Element(ns + "Commands")!.Attribute("package"));
        Assert.Single(table.Descendants(ns + "Button"));
        Assert.Empty(table.Descendants(ns + "Parent"));
    }

    internal static ImmutableArray<IReadOnlyDictionary<string, object>> ReadDefaults(JsonElement array)
        => array.GetProperty("default").EnumerateArray().Select(item => (IReadOnlyDictionary<string, object>)item.EnumerateObject().ToDictionary(
            property => property.Name, property => ReadScalar(property.Value))).ToImmutableArray();

    internal static object ReadScalar(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => (object)value.GetString()!,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidOperationException("Registered Naming object-array defaults must be scalars."),
        };

    private static void CheckResources(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var properties = value.EnumerateObject().ToArray();
            Assert.Equal(properties.Length, properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count());
            foreach (var property in properties)
                CheckResources(property.Value);
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
                CheckResources(item);
        }
        else if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text && (text.StartsWith("@", StringComparison.Ordinal) || text.StartsWith("#", StringComparison.Ordinal)))
        {
            var resolved = TestModel.Utilities.EvalResource(text);
            Assert.False(string.IsNullOrEmpty(resolved), "Unresolved resource: " + text);
            Assert.DoesNotContain("_", resolved);
        }
    }

    internal static JsonDocument Load(string file)
    {
        var assembly = typeof(NamingSettingsManifestTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(name => name.EndsWith(file, StringComparison.Ordinal)))!;
        return JsonDocument.Parse(stream);
    }
}
