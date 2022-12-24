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
            "indent_style", LineFormattingOptions.Default.UseTabs, FormattingOptionGroups.IndentationAndSpacing,
            new EditorConfigStorageLocation<bool>(s => s == "tab", isSet => isSet ? "tab" : "space"));

        public static PerLanguageOption2<int> TabSize = new(
            "tab_width", LineFormattingOptions.Default.TabSize, FormattingOptionGroups.IndentationAndSpacing,
            EditorConfigStorageLocation.ForInt32Option());

        public static PerLanguageOption2<int> IndentationSize = new(
            "indent_size", LineFormattingOptions.Default.IndentationSize, FormattingOptionGroups.IndentationAndSpacing,
            EditorConfigStorageLocation.ForInt32Option());

        public static PerLanguageOption2<string> NewLine = new(
            "end_of_line", LineFormattingOptions.Default.NewLine, FormattingOptionGroups.NewLine,
            new EditorConfigStorageLocation<string>(
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
            "insert_final_newline", DocumentFormattingOptions.Default.InsertFinalNewLine, FormattingOptionGroups.NewLine,
            EditorConfigStorageLocation.ForBoolOption());

        public static PerLanguageOption2<IndentStyle> SmartIndent = new(
            "FormattingOptions_SmartIndent", defaultValue: IndentationOptions.DefaultIndentStyle, group: FormattingOptionGroups.IndentationAndSpacing);

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
