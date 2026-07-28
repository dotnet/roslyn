// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.LanguageClient.Options;

internal static class SettingsNames
{
    public const string UnifiedCollection = "languages.razor.advanced";

    public const string FormatOnType = UnifiedCollection + ".formatOnType";
    public const string AutoClosingTags = UnifiedCollection + ".autoClosingTags";
    public const string AutoInsertAttributeQuotes = UnifiedCollection + ".autoInsertAttributeQuotes";
    public const string ColorBackground = UnifiedCollection + ".colorBackground";
    public const string CodeBlockBraceOnNextLine = UnifiedCollection + ".codeBlockBraceOnNextLine";
    public const string AttributeIndentStyle = UnifiedCollection + ".attributeIndentStyle";
    public const string CommitElementsWithSpace = UnifiedCollection + ".commitElementsWithSpace";
    public const string Snippets = UnifiedCollection + ".snippets";
    public const string LogLevel = UnifiedCollection + ".logLevel";
    public const string FormatOnPaste = UnifiedCollection + ".formatOnPaste";

    public static readonly string[] AllSettings =
    [
        FormatOnType,
        AutoClosingTags,
        AutoInsertAttributeQuotes,
        ColorBackground,
        CodeBlockBraceOnNextLine,
        AttributeIndentStyle,
        CommitElementsWithSpace,
        Snippets,
        LogLevel,
        FormatOnPaste,
    ];
}
