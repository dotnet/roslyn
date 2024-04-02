// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.CodeStyle;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
using PublicIndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions2.IndentStyle;
#else
using PublicIndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentStyle;
#endif

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Formatting options stored in editorconfig.
/// </summary>
internal sealed partial class FormattingOptions2
{
    private const string PublicFeatureName = "FormattingOptions";

    public static PerLanguageOption2<bool> UseTabs = new PerLanguageOption2<bool>(
        "indent_style", LineFormattingOptions.Default.UseTabs, FormattingOptionGroups.IndentationAndSpacing, isEditorConfigOption: true,
        serializer: new EditorConfigValueSerializer<bool>(str => str == "tab", value => value ? "tab" : "space"))
        .WithPublicOption(PublicFeatureName, "UseTabs");

    public static PerLanguageOption2<int> TabSize = new PerLanguageOption2<int>(
        "tab_width", LineFormattingOptions.Default.TabSize, FormattingOptionGroups.IndentationAndSpacing, isEditorConfigOption: true)
        .WithPublicOption(PublicFeatureName, "TabSize");

    public static PerLanguageOption2<int> IndentationSize = new PerLanguageOption2<int>(
        "indent_size", LineFormattingOptions.Default.IndentationSize, FormattingOptionGroups.IndentationAndSpacing, isEditorConfigOption: true)
        .WithPublicOption(PublicFeatureName, "IndentationSize");

    public static PerLanguageOption2<string> NewLine = new PerLanguageOption2<string>(
        "end_of_line", LineFormattingOptions.Default.NewLine, FormattingOptionGroups.NewLine, isEditorConfigOption: true,
        serializer: new EditorConfigValueSerializer<string>(
            parseValue: value => value.Trim() switch
            {
                "lf" => "\n",
                "cr" => "\r",
                "crlf" => "\r\n",
                _ => Environment.NewLine
            },
            serializeValue: value => value switch
            {
                "\n" => "lf",
                "\r" => "cr",
                "\r\n" => "crlf",
                _ => "unset"
            }))
        .WithPublicOption(PublicFeatureName, "NewLine");

    internal static Option2<bool> InsertFinalNewLine = new(
        "insert_final_newline", DocumentFormattingOptions.Default.InsertFinalNewLine, FormattingOptionGroups.NewLine, isEditorConfigOption: true);

    public static PerLanguageOption2<IndentStyle> SmartIndent = new PerLanguageOption2<IndentStyle>(
        "smart_indent",
        defaultValue: IndentationOptions.DefaultIndentStyle,
        group: FormattingOptionGroups.IndentationAndSpacing)
        .WithPublicOption(PublicFeatureName, "SmartIndent", static value => (PublicIndentStyle)value, static value => (IndentStyle)value);

#if !CODE_STYLE
    internal static readonly ImmutableArray<IOption2> Options = [UseTabs, TabSize, IndentationSize, NewLine, InsertFinalNewLine];
#endif
}

internal static class FormattingOptionGroups
{
    public static readonly OptionGroup FormattingOptionGroup = new(name: "formatting", description: "", parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup IndentationAndSpacing = new(name: "indentation_and_spacing", description: WorkspacesResources.Indentation_and_spacing, priority: 1, parent: FormattingOptionGroup);
    public static readonly OptionGroup NewLine = new(name: "new_line", description: WorkspacesResources.New_line_preferences, priority: 2, parent: FormattingOptionGroup);
}
