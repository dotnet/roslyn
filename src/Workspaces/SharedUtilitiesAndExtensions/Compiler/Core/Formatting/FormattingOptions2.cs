// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Indentation;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
using PublicIndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions2.IndentStyle;
#else
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
using PublicIndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentStyle;
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
        private const string PublicFeatureName = "FormattingOptions";

        public static PerLanguageOption2<bool> UseTabs = new PerLanguageOption2<bool>(
            "indent_style", LineFormattingOptions.Default.UseTabs, FormattingOptionGroups.IndentationAndSpacing,
            new EditorConfigStorageLocation<bool>(s => s == "tab", isSet => isSet ? "tab" : "space"))
            .WithPublicOption(PublicFeatureName, "UseTabs");

        public static PerLanguageOption2<int> TabSize = new PerLanguageOption2<int>(
            "tab_width", LineFormattingOptions.Default.TabSize, FormattingOptionGroups.IndentationAndSpacing,
            EditorConfigStorageLocation.ForInt32Option())
            .WithPublicOption(PublicFeatureName, "TabSize");

        public static PerLanguageOption2<int> IndentationSize = new PerLanguageOption2<int>(
            "indent_size", LineFormattingOptions.Default.IndentationSize, FormattingOptionGroups.IndentationAndSpacing,
            EditorConfigStorageLocation.ForInt32Option())
            .WithPublicOption(PublicFeatureName, "IndentationSize");

        public static PerLanguageOption2<string> NewLine = new PerLanguageOption2<string>(
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
                }))
            .WithPublicOption(PublicFeatureName, "NewLine");

        internal static Option2<bool> InsertFinalNewLine = new(
            "insert_final_newline", DocumentFormattingOptions.Default.InsertFinalNewLine, FormattingOptionGroups.NewLine,
            EditorConfigStorageLocation.ForBoolOption());

        public static PerLanguageOption2<IndentStyle> SmartIndent = new PerLanguageOption2<IndentStyle>(
            "FormattingOptions_SmartIndent",
            defaultValue: IndentationOptions.DefaultIndentStyle,
            group: FormattingOptionGroups.IndentationAndSpacing)
            .WithPublicOption(PublicFeatureName, "SmartIndent", static value => (PublicIndentStyle)value);

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
