// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class FormattingOptions2
    {
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        internal static ImmutableArray<IOption2> AllOptions { get; }

        private static PerLanguageOption2<T> CreatePerLanguageOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
        {
            var option = new PerLanguageOption2<T>(nameof(FormattingOptions), group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
        {
            var option = new Option2<T>(nameof(FormattingOptions), group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        public static PerLanguageOption2<bool> UseTabs { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(UseTabs),
            defaultValue: false,
            storageLocations: new EditorConfigStorageLocation<bool>(
                "indent_style",
                s => s == "tab",
                isSet => isSet ? "tab" : "space"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<int> TabSize { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(TabSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("tab_width"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<int> IndentationSize { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(IndentationSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("indent_size"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption2<FormattingOptions.IndentStyle> SmartIndent { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(SmartIndent),
            defaultValue: FormattingOptions.IndentStyle.Smart);

        public static PerLanguageOption2<string> NewLine { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.NewLine, nameof(NewLine),
            defaultValue: Environment.NewLine,
            storageLocations: new EditorConfigStorageLocation<string>(
                "end_of_line",
                ParseEditorConfigEndOfLine,
                GetEndOfLineEditorConfigString));

        internal static Option2<bool> InsertFinalNewLine { get; } = CreateOption(
            FormattingOptionGroups.NewLine, nameof(InsertFinalNewLine),
            defaultValue: false,
            storageLocations: EditorConfigStorageLocation.ForBoolOption("insert_final_newline"));

        /// <summary>
        /// Default value of 120 was picked based on the amount of code in a github.com diff at 1080p.
        /// That resolution is the most common value as per the last DevDiv survey as well as the latest
        /// Steam hardware survey.  This also seems to a reasonable length default in that shorter
        /// lengths can often feel too cramped for .NET languages, which are often starting with a
        /// default indentation of at least 16 (for namespace, class, member, plus the final construct
        /// indentation).
        /// </summary>
        internal static Option2<int> PreferredWrappingColumn { get; } = new Option2<int>(
            nameof(FormattingOptions),
            FormattingOptionGroups.NewLine,
            nameof(PreferredWrappingColumn),
            defaultValue: 120);

        private static readonly BidirectionalMap<string, string> s_parenthesesPreferenceMap =
            new BidirectionalMap<string, string>(new[]
            {
                KeyValuePairUtil.Create("lf", "\n"),
                KeyValuePairUtil.Create("cr", "\r"),
                KeyValuePairUtil.Create("crlf", "\r\n"),
            });

        private static Optional<string> ParseEditorConfigEndOfLine(string endOfLineValue)
            => s_parenthesesPreferenceMap.TryGetValue(endOfLineValue.Trim(), out var parsedOption) ? parsedOption : NewLine.DefaultValue;

        private static string GetEndOfLineEditorConfigString(string option)
            => s_parenthesesPreferenceMap.TryGetKey(option, out var editorConfigString) ? editorConfigString : "unset";

        internal static Option2<bool> AllowDisjointSpanMerging { get; } = CreateOption(OptionGroup.Default, nameof(AllowDisjointSpanMerging), defaultValue: false);

        internal static readonly PerLanguageOption2<bool> AutoFormattingOnReturn = CreatePerLanguageOption(OptionGroup.Default, nameof(AutoFormattingOnReturn), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Auto Formatting On Return"));

        static FormattingOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }
    }

    internal static class FormattingOptionGroups
    {
        public static readonly OptionGroup IndentationAndSpacing = new OptionGroup(WorkspacesResources.Indentation_and_spacing, priority: 1);
        public static readonly OptionGroup NewLine = new OptionGroup(WorkspacesResources.New_line_preferences, priority: 2);
    }
}
