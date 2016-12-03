// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class FormattingOptions
    {
        public static PerLanguageOption<bool> UseTabs { get; } = new PerLanguageOption<bool>(nameof(FormattingOptions), nameof(UseTabs), defaultValue: false,
            storageLocations: new EditorConfigStorageLocation("indent_style", s => s == "tab"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> TabSize { get; } = new PerLanguageOption<int>(nameof(FormattingOptions), nameof(TabSize), defaultValue: 4,
            storageLocations: new EditorConfigStorageLocation("tab_width"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> IndentationSize { get; } = new PerLanguageOption<int>(nameof(FormattingOptions), nameof(IndentationSize), defaultValue: 4,
            storageLocations: new EditorConfigStorageLocation("indent_size"));

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = new PerLanguageOption<IndentStyle>(nameof(FormattingOptions), nameof(SmartIndent), defaultValue: IndentStyle.Smart);

        public static PerLanguageOption<string> NewLine { get; } = new PerLanguageOption<string>(nameof(FormattingOptions), nameof(NewLine), defaultValue: "\r\n",
            storageLocations: new EditorConfigStorageLocation("end_of_line", ParseEditorConfigEndOfLine));

        private static object ParseEditorConfigEndOfLine(string endOfLineValue)
        {
            switch (endOfLineValue)
            {
                case "lf": return "\n";
                case "cr": return "\r";
                case "crlf": return "\r\n";
                default: return NewLine.DefaultValue;
            }
        }

        internal static PerLanguageOption<bool> DebugMode { get; } = new PerLanguageOption<bool>(nameof(FormattingOptions), nameof(DebugMode), defaultValue: false);

        internal static Option<bool> AllowDisjointSpanMerging { get; } = new Option<bool>(nameof(FormattingOptions), nameof(AllowDisjointSpanMerging), defaultValue: false);

        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
