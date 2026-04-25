// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Settings;

/// <summary>
/// Settings that are set and handled on the client, but needed by the LSP Server to function correctly. When these are
/// updated a workspace/didchangeconfiguration should be sent from client to the server. Then the server requests
/// workspace/configuration to get the latest settings. For VS, the razor protocol also handles this and serializes the
/// settings back to the server.
/// </summary>
/// <param name="ClientSpaceSettings"></param>
/// <param name="ClientCompletionSettings"></param>
/// <param name="AdvancedSettings"></param>
internal record ClientSettings(
    [property: JsonPropertyName("clientSpaceSettings")] ClientSpaceSettings ClientSpaceSettings,
    [property: JsonPropertyName("clientCompletionSettings")] ClientCompletionSettings ClientCompletionSettings,
    [property: JsonPropertyName("advancedSettings")] ClientAdvancedSettings AdvancedSettings)
{
    public static readonly ClientSettings Default = new(ClientSpaceSettings.Default, ClientCompletionSettings.Default, ClientAdvancedSettings.Default);

    public RazorFormattingOptions ToRazorFormattingOptions()
        => new()
        {
            InsertSpaces = !ClientSpaceSettings.IndentWithTabs,
            TabSize = ClientSpaceSettings.IndentSize,
            CodeBlockBraceOnNextLine = AdvancedSettings.CodeBlockBraceOnNextLine,
            AttributeIndentStyle = AdvancedSettings.AttributeIndentStyle,
        };
}

internal sealed record ClientCompletionSettings(
    [property: JsonPropertyName("autoShowCompletion")] bool AutoShowCompletion,
    [property: JsonPropertyName("autoListParams")] bool AutoListParams)
{
    public static readonly ClientCompletionSettings Default = new(AutoShowCompletion: true, AutoListParams: true);
}

internal sealed record ClientSpaceSettings(
    [property: JsonPropertyName("indentWithTabs")] bool IndentWithTabs,
    [property: JsonPropertyName("indentSize")] int IndentSize)
{
    public static readonly ClientSpaceSettings Default = new(IndentWithTabs: false, IndentSize: 4);
}

internal sealed record ClientAdvancedSettings(
    [property: JsonPropertyName("formatOnType")] bool FormatOnType,
    [property: JsonPropertyName("autoClosingTags")] bool AutoClosingTags,
    [property: JsonPropertyName("autoInsertAttributeQuotes")] bool AutoInsertAttributeQuotes,
    [property: JsonPropertyName("colorBackground")] bool ColorBackground,
    [property: JsonPropertyName("codeBlockBraceOnNextLine")] bool CodeBlockBraceOnNextLine,
    [property: JsonPropertyName("attributeIndentStyle")] AttributeIndentStyle AttributeIndentStyle,
    [property: JsonPropertyName("commitElementsWithSpace")] bool CommitElementsWithSpace,
    [property: JsonPropertyName("snippetSetting")] SnippetSetting SnippetSetting,
    [property: JsonPropertyName("logLevel")] LogLevel LogLevel,
    [property: JsonPropertyName("formatOnPaste")] bool FormatOnPaste,
    [property: JsonPropertyName("taskListDescriptors")] ImmutableArray<string> TaskListDescriptors)
{
    public static readonly ClientAdvancedSettings Default = new(FormatOnType: true,
                                                                 AutoClosingTags: true,
                                                                 AutoInsertAttributeQuotes: true,
                                                                ColorBackground: false,
                                                                CodeBlockBraceOnNextLine: false,
                                                                AttributeIndentStyle: AttributeIndentStyle.AlignWithFirst,
                                                                CommitElementsWithSpace: true,
                                                                SnippetSetting.All,
                                                                LogLevel.Warning,
                                                                FormatOnPaste: true,
                                                                TaskListDescriptors: []);

    public bool Equals(ClientAdvancedSettings? other)
    {
        return other is not null &&
            FormatOnType == other.FormatOnType &&
            AutoClosingTags == other.AutoClosingTags &&
            AutoInsertAttributeQuotes == other.AutoInsertAttributeQuotes &&
            ColorBackground == other.ColorBackground &&
            CodeBlockBraceOnNextLine == other.CodeBlockBraceOnNextLine &&
            AttributeIndentStyle == other.AttributeIndentStyle &&
            CommitElementsWithSpace == other.CommitElementsWithSpace &&
            SnippetSetting == other.SnippetSetting &&
            LogLevel == other.LogLevel &&
            FormatOnPaste == other.FormatOnPaste &&
            TaskListDescriptors.SequenceEqual(other.TaskListDescriptors);
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(FormatOnType);
        hash.Add(AutoClosingTags);
        hash.Add(AutoInsertAttributeQuotes);
        hash.Add(ColorBackground);
        hash.Add(CodeBlockBraceOnNextLine);
        hash.Add(AttributeIndentStyle);
        hash.Add(CommitElementsWithSpace);
        hash.Add(SnippetSetting);
        hash.Add(LogLevel);
        hash.Add(FormatOnPaste);
        hash.Add(TaskListDescriptors);
        return hash;
    }
}
