// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class FormattingOptions
    {
        private static Option<T> CreateOption<T>(string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        {
            return new Option<T>(nameof(FormattingOptions), name, defaultValue, storageLocations);
        }

        public static Option<bool> UseTabs { get; } = CreateOption(
            nameof(UseTabs),
            defaultValue: false,
            storageLocations: new EditorConfigStorageLocation<bool>(
                "indent_style",
                s => s == "tab",
                isSet => isSet ? "tab" : "space"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static Option<int> TabSize { get; } = CreateOption(
            nameof(TabSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("tab_width"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static Option<int> IndentationSize { get; } = CreateOption(
            nameof(IndentationSize),
            defaultValue: 4,
            storageLocations: EditorConfigStorageLocation.ForInt32Option("indent_size"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static Option<IndentStyle> SmartIndent { get; } = CreateOption(
            nameof(SmartIndent),
            defaultValue: IndentStyle.Smart);

        public static Option<string> NewLine { get; } = CreateOption(
            nameof(NewLine),
            defaultValue: Environment.NewLine,
            storageLocations: new EditorConfigStorageLocation<string>(
                "end_of_line",
                ParseEditorConfigEndOfLine,
                GetEndOfLineEditorConfigString));

        internal static Option<bool> InsertFinalNewLine { get; } = CreateOption(
            nameof(InsertFinalNewLine),
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

        internal static Option<bool> AllowDisjointSpanMerging { get; } = CreateOption(nameof(AllowDisjointSpanMerging), defaultValue: false);

        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
