// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Settings;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class ClientSettingsJsonSerializationTest
{
    [Fact]
    public void ClientSettings_RoundTripsWithExpectedPropertyNames()
    {
        var settings = new ClientSettings(
            new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 2),
            new ClientCompletionSettings(AutoShowCompletion: false, AutoListParams: false),
            new ClientAdvancedSettings(
                FormatOnType: false,
                AutoClosingTags: false,
                AutoInsertAttributeQuotes: false,
                ColorBackground: true,
                CodeBlockBraceOnNextLine: true,
                AttributeIndentStyle: AttributeIndentStyle.IndentByOne,
                CommitElementsWithSpace: false,
                SnippetSetting: SnippetSetting.None,
                LogLevel: LogLevel.Trace,
                FormatOnPaste: false,
                TaskListDescriptors: ["TODO", "HACK"]));

        var json = JsonSerializer.Serialize(settings);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("clientSpaceSettings", out var clientSpaceSettings));
        Assert.True(root.TryGetProperty("clientCompletionSettings", out var clientCompletionSettings));
        Assert.True(root.TryGetProperty("advancedSettings", out var advancedSettings));

        Assert.True(clientSpaceSettings.TryGetProperty("indentWithTabs", out var indentWithTabs));
        Assert.True(indentWithTabs.GetBoolean());
        Assert.True(clientSpaceSettings.TryGetProperty("indentSize", out var indentSize));
        Assert.Equal(2, indentSize.GetInt32());

        Assert.True(clientCompletionSettings.TryGetProperty("autoShowCompletion", out var autoShowCompletion));
        Assert.False(autoShowCompletion.GetBoolean());
        Assert.True(clientCompletionSettings.TryGetProperty("autoListParams", out var autoListParams));
        Assert.False(autoListParams.GetBoolean());

        Assert.True(advancedSettings.TryGetProperty("formatOnType", out var formatOnType));
        Assert.False(formatOnType.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("autoClosingTags", out var autoClosingTags));
        Assert.False(autoClosingTags.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("autoInsertAttributeQuotes", out var autoInsertAttributeQuotes));
        Assert.False(autoInsertAttributeQuotes.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("colorBackground", out var colorBackground));
        Assert.True(colorBackground.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("codeBlockBraceOnNextLine", out var codeBlockBraceOnNextLine));
        Assert.True(codeBlockBraceOnNextLine.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("attributeIndentStyle", out var attributeIndentStyle));
        Assert.Equal((int)AttributeIndentStyle.IndentByOne, attributeIndentStyle.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("commitElementsWithSpace", out var commitElementsWithSpace));
        Assert.False(commitElementsWithSpace.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("snippetSetting", out var snippetSetting));
        Assert.Equal((int)SnippetSetting.None, snippetSetting.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("logLevel", out var logLevel));
        Assert.Equal((int)LogLevel.Trace, logLevel.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("formatOnPaste", out var formatOnPaste));
        Assert.False(formatOnPaste.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("taskListDescriptors", out var taskListDescriptors));
        Assert.Collection<string>(taskListDescriptors.EnumerateArray().Select(e => e.GetString())!,
            descriptor => Assert.Equal("TODO", descriptor),
            descriptor => Assert.Equal("HACK", descriptor));

        var roundTripped = JsonSerializer.Deserialize<ClientSettings>(json);
        Assert.Equal(settings, roundTripped);
    }
}
