// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class RazorFormattingOptionsStorage
{
    internal static readonly Option2<bool> CodeBlockBraceOnNextLine = new(
        "razor_code_block_brace_on_next_line", defaultValue: false, languageName: "Razor", isEditorConfigOption: true);

    internal static readonly Option2<AttributeIndentStyle> AttributeIndentStyle = new(
        "razor_attribute_indent_style",
        defaultValue: Settings.AttributeIndentStyle.AlignWithFirst,
        languageName: "Razor",
        isEditorConfigOption: true,
        serializer: EditorConfigValueSerializer.CreateSerializerForEnum(
            entries:
            [
                ("align_with_first", Settings.AttributeIndentStyle.AlignWithFirst),
                ("indent_by_one", Settings.AttributeIndentStyle.IndentByOne),
                ("indent_by_two", Settings.AttributeIndentStyle.IndentByTwo),
            ],
            alternativeEntries: []));

    internal static readonly Option2<bool> IgnoreIndentationInScriptOrStyleBlocksWithComplexRazor = new(
        "razor_ignore_indentation_in_script_or_style_blocks_with_complex_razor", defaultValue: true, languageName: "Razor", isEditorConfigOption: true);

    internal static IOptionsReader GetOptions(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        // Match the Razor file, not its generated C# document. Leave missing values to the caller so
        // IDE preferences remain the fallback without introducing a dependency on client settings here.
        return razorDocument.Project.State
            .GetAnalyzerOptionsForPath(razorDocument.FilePath.AssumeNotNull(), cancellationToken)
            .ConfigOptionsWithoutFallback;
    }
}
