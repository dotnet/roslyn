// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class FormattingOptions
    {
        private static readonly ImmutableArray<IOption>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption>();

        internal static ImmutableArray<IOption> AllOptions { get; }

        private static PerLanguageOption<T> CreatePerLanguageOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        {
            var option = new PerLanguageOption<T>(nameof(FormattingOptions), group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option<T> CreateOption<T>(OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        {
            var option = new Option<T>(nameof(FormattingOptions), group, name, defaultValue, storageLocations);
            s_allOptionsBuilder.Add(option);
            return option;
        }

        public static PerLanguageOption<bool> UseTabs { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(UseTabs),
            defaultValue: false,
            storageLocations: new EditorConfigStorageLocation<bool>(
                "indent_style",
                s => s == "tab",
                isSet => isSet ? "tab" : "space"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> TabSize { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(TabSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("tab_width"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> IndentationSize { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(IndentationSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("indent_size"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.IndentationAndSpacing, nameof(SmartIndent),
            defaultValue: IndentStyle.Smart);

        public static PerLanguageOption<string> NewLine { get; } = CreatePerLanguageOption(
            FormattingOptionGroups.NewLine, nameof(NewLine),
            defaultValue: Environment.NewLine,
            storageLocations: new EditorConfigStorageLocation<string>(
                "end_of_line",
                ParseEditorConfigEndOfLine,
                GetEndOfLineEditorConfigString));

        internal static Option<bool> InsertFinalNewLine { get; } = CreateOption(
            FormattingOptionGroups.NewLine, nameof(InsertFinalNewLine),
            defaultValue: false,
            storageLocations: EditorConfigStorageLocation.ForBoolOption("insert_final_newline"));

        private static readonly BidirectionalMap<string, string> s_parenthesesPreferenceMap =
            new BidirectionalMap<string, string>(new[]
            {
                KeyValuePairUtil.Create("lf", "\n"),
                KeyValuePairUtil.Create("cr", "\r"),
                KeyValuePairUtil.Create("crlf", "\r\n"),
            });

        private static Optional<string> ParseEditorConfigEndOfLine(string endOfLineValue)
            => s_parenthesesPreferenceMap.TryGetValue(endOfLineValue, out var parsedOption) ? parsedOption : NewLine.DefaultValue;

        private static string GetEndOfLineEditorConfigString(string option)
            => s_parenthesesPreferenceMap.TryGetKey(option, out var editorConfigString) ? editorConfigString : null;

        internal static PerLanguageOption<bool> DebugMode { get; } = CreatePerLanguageOption(OptionGroup.Default, nameof(DebugMode), defaultValue: false);

        internal static Option<bool> AllowConcurrent { get; } = new Option<bool>(nameof(FormattingOptions), nameof(AllowConcurrent), defaultValue: true);

        internal static Option<bool> AllowDisjointSpanMerging { get; } = CreateOption(OptionGroup.Default, nameof(AllowDisjointSpanMerging), defaultValue: false);

        static FormattingOptions()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to s_allOptionsBuilder.
            AllOptions = s_allOptionsBuilder.ToImmutable();
        }

        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }

    internal static class FormattingOptionGroups
    {
        public static readonly OptionGroup IndentationAndSpacing = new OptionGroup(WorkspacesResources.Indentation_and_spacing, priority: 1);
        public static readonly OptionGroup NewLine = new OptionGroup(WorkspacesResources.New_line_preferences, priority: 2);
    }
}
