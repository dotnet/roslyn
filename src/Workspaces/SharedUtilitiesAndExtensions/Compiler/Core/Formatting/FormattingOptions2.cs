// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Formatting options stored in editorconfig.
    /// </summary>
    internal sealed class FormattingOptions2
    {
        private const string FeatureName = "FormattingOptions";

        public static PerLanguageOption2<bool> UseTabs =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(UseTabs), defaultValue: false,
            storageLocation: new EditorConfigStorageLocation<bool>(
                "indent_style",
                s => s == "tab",
                isSet => isSet ? "tab" : "space"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<int> TabSize =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(TabSize), defaultValue: 4,
            storageLocation: EditorConfigStorageLocation.ForInt32Option("tab_width"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<int> IndentationSize =
            new(FeatureName, FormattingOptionGroups.IndentationAndSpacing, nameof(IndentationSize), defaultValue: 4,
            storageLocation: EditorConfigStorageLocation.ForInt32Option("indent_size"));

        public static PerLanguageOption2<string> NewLine =
            new(FeatureName, FormattingOptionGroups.NewLine, nameof(NewLine), defaultValue: Environment.NewLine,
            storageLocation: new EditorConfigStorageLocation<string>(
                "end_of_line",
                parseValue: value => value.Trim() switch
                {
                    "lf" => "\n",
                    "cr" => "\r",
                    "crlf" => "\r\n",
                    _ => Environment.NewLine
                },
                getEditorConfigStringForValue: option => option switch
                {
                    "\n" => "lf",
                    "\r" => "cr",
                    "\r\n" => "crlf",
                    _ => "unset"
                }));

        internal static Option2<bool> InsertFinalNewLine =
            new(FeatureName, FormattingOptionGroups.NewLine, nameof(InsertFinalNewLine), defaultValue: false,
            storageLocation: EditorConfigStorageLocation.ForBoolOption("insert_final_newline"));

        public static ImmutableArray<IOption2> Options = ImmutableArray.Create<IOption2>(
            UseTabs,
            TabSize,
            IndentationSize,
            NewLine,
            InsertFinalNewLine);
    }

    internal static class FormattingOptionGroups
    {
        public static readonly OptionGroup IndentationAndSpacing = new(WorkspacesResources.Indentation_and_spacing, priority: 1);
        public static readonly OptionGroup NewLine = new(WorkspacesResources.New_line_preferences, priority: 2);
    }
}
