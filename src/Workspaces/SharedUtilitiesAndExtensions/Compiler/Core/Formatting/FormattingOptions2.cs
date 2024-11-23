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

    /// <summary>
    /// Default value of 120 was picked based on the amount of code in a github.com diff at 1080p.
    /// That resolution is the most common value as per the last DevDiv survey as well as the latest
    /// Steam hardware survey.  This also seems to a reasonable length default in that shorter
    /// lengths can often feel too cramped for .NET languages, which are often starting with a
    /// default indentation of at least 16 (for namespace, class, member, plus the final construct
    /// indentation).
    /// 
    /// TODO: Currently the option has no storage and always has its default value. See https://github.com/dotnet/roslyn/pull/30422#issuecomment-436118696.
    /// 
    /// Internal option -- not exposed to tooling.
    /// </summary>
    public static readonly PerLanguageOption2<int> WrappingColumn = new(
        $"dotnet_unsupported_wrapping_column",
        defaultValue: SyntaxFormattingOptions.CommonDefaults.WrappingColumn,
        isEditorConfigOption: true);

    /// <summary>
    /// Internal option -- not exposed to editorconfig tooling.
    /// </summary>
    public static readonly PerLanguageOption2<int> ConditionalExpressionWrappingLength = new(
        $"dotnet_unsupported_conditional_expression_wrapping_length",
        defaultValue: SyntaxFormattingOptions.CommonDefaults.ConditionalExpressionWrappingLength,
        isEditorConfigOption: true);

#if !CODE_STYLE
    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = [UseTabs, TabSize, IndentationSize, NewLine, InsertFinalNewLine];

    /// <summary>
    /// Options that can be set via editorconfig but we do not provide tooling support.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> UndocumentedOptions = [WrappingColumn, ConditionalExpressionWrappingLength];
#endif
}

internal static class FormattingOptionGroups
{
    public static readonly OptionGroup FormattingOptionGroup = new(name: "formatting", description: "", parent: CodeStyleOptionGroups.CodeStyle);
    public static readonly OptionGroup IndentationAndSpacing = new(name: "indentation_and_spacing", description: WorkspacesResources.Indentation_and_spacing, priority: 1, parent: FormattingOptionGroup);
    public static readonly OptionGroup NewLine = new(name: "new_line", description: WorkspacesResources.New_line_preferences, priority: 2, parent: FormattingOptionGroup);
}
