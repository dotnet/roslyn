// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis.Razor.Settings;
using LspFormattingOptions = Roslyn.LanguageServer.Protocol.FormattingOptions;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class RazorFormattingOptionsExtensions
{
    public static RazorFormattingOptions GetRazorFormattingOptions(
        this TextDocument razorDocument,
        ClientSettings clientSettings,
        CancellationToken cancellationToken)
        => razorDocument.GetRazorFormattingOptions(clientSettings, formattingOptions: null, cancellationToken);

    public static RazorFormattingOptions GetRazorFormattingOptions(
        this TextDocument razorDocument,
        ClientSettings clientSettings,
        LspFormattingOptions? formattingOptions,
        CancellationToken cancellationToken)
    {
        var spaceSettings = clientSettings.ClientSpaceSettings;
        var advancedSettings = clientSettings.AdvancedSettings;

        var insertSpaces = formattingOptions?.InsertSpaces ?? !spaceSettings.IndentWithTabs;
        var tabSize = formattingOptions?.TabSize ?? spaceSettings.IndentSize;
        var csharpSyntaxFormattingOptions = razorDocument.GetCSharpSyntaxFormattingOptions(cancellationToken);

        var codeBlockBraceOnNextLine = advancedSettings.CodeBlockBraceOnNextLine;
        var attributeIndentStyle = advancedSettings.AttributeIndentStyle;
        var ignoreIndentationInScriptOrStyleBlocksWithComplexRazor = advancedSettings.IgnoreIndentationInScriptOrStyleBlocksWithComplexRazor;

        return new RazorFormattingOptions
        {
            InsertSpaces = insertSpaces,
            TabSize = tabSize,
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
            IgnoreIndentationInScriptOrStyleBlocksWithComplexRazor = ignoreIndentationInScriptOrStyleBlocksWithComplexRazor,
        };
    }
}
