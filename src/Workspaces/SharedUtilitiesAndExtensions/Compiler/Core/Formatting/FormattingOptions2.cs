// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Indentation;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#else
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Formatting options stored in editorconfig.
    /// </summary>
    internal sealed partial class FormattingOptions2
    {
#if !CODE_STYLE
        [ExportEditorConfigOptionProvider, Shared]
        internal sealed class Provider : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Provider()
            {
            }

            public ImmutableArray<IOption2> Options { get; } = FormattingOptions2.Options;
        }
#endif

        public static PerLanguageOption2<bool> UseTabs = new(
            FormattingOptionGroups.IndentationAndSpacing, "indent_style", LineFormattingOptions.Default.UseTabs,
            new EditorConfigStorageLocation<bool>("indent_style", s => s == "tab", isSet => isSet ? "tab" : "space"));

        public static PerLanguageOption2<int> TabSize = new(
            FormattingOptionGroups.IndentationAndSpacing, "tab_width", LineFormattingOptions.Default.TabSize,
            EditorConfigStorageLocation.ForInt32Option("tab_width"));

        public static PerLanguageOption2<int> IndentationSize = new(
            FormattingOptionGroups.IndentationAndSpacing, "indent_size", LineFormattingOptions.Default.IndentationSize,
            EditorConfigStorageLocation.ForInt32Option("indent_size"));

        public static PerLanguageOption2<string> NewLine = new(
            FormattingOptionGroups.NewLine, "end_of_line", LineFormattingOptions.Default.NewLine,
            new EditorConfigStorageLocation<string>(
                "end_of_line",
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
                }));

        internal static Option2<bool> InsertFinalNewLine = new(
            FormattingOptionGroups.NewLine, "insert_final_newline", DocumentFormattingOptions.Default.InsertFinalNewLine,
            EditorConfigStorageLocation.ForBoolOption("insert_final_newline"));

        public static PerLanguageOption2<IndentStyle> SmartIndent = new(
            FormattingOptionGroups.IndentationAndSpacing, "FormattingOptions_SmartIndent", defaultValue: IndentationOptions.DefaultIndentStyle);

#if !CODE_STYLE
        internal static readonly ImmutableArray<IOption2> Options = ImmutableArray.Create<IOption2>(
            UseTabs,
            TabSize,
            IndentationSize,
            NewLine,
            InsertFinalNewLine);
#endif
    }

    internal static class FormattingOptionGroups
    {
        public static readonly OptionGroup IndentationAndSpacing = new(WorkspacesResources.Indentation_and_spacing, priority: 1);
        public static readonly OptionGroup NewLine = new(WorkspacesResources.New_line_preferences, priority: 2);
    }
}
