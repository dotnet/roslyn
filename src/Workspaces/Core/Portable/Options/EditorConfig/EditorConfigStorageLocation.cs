// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        public static EditorConfigStorageLocation<bool> ForBoolOption(string keyName)
            => new EditorConfigStorageLocation<bool>(keyName, s_parseBool, s_getBoolEditorConfigStringForValue);

        public static EditorConfigStorageLocation<int> ForInt32Option(string keyName)
            => new EditorConfigStorageLocation<int>(keyName, s_parseInt32, s_getInt32EditorConfigStringForValue);

        public static EditorConfigStorageLocation<CodeStyleOption<bool>> ForBoolCodeStyleOption(string keyName)
            => new EditorConfigStorageLocation<CodeStyleOption<bool>>(keyName, s_parseBoolCodeStyleOption, s_getBoolCodeStyleOptionEditorConfigStringForValue);

        public static EditorConfigStorageLocation<CodeStyleOption<string>> ForStringCodeStyleOption(string keyName)
            => new EditorConfigStorageLocation<CodeStyleOption<string>>(keyName, s_parseStringCodeStyleOption, s_getStringCodeStyleOptionEditorConfigStringForValue);

        private static readonly Func<string, Optional<bool>> s_parseBool = ParseBool;
        private static Optional<bool> ParseBool(string str)
            => bool.TryParse(str, out var result) ? result : new Optional<bool>();
        private static readonly Func<bool, string> s_getBoolEditorConfigStringForValue = GetBoolEditorConfigStringForValue;
        private static string GetBoolEditorConfigStringForValue(bool value) => value.ToString().ToLowerInvariant();

        private static readonly Func<string, Optional<int>> s_parseInt32 = ParseInt32;
        private static Optional<int> ParseInt32(string str)
            => int.TryParse(str, out var result) ? result : new Optional<int>();
        private static readonly Func<int, string> s_getInt32EditorConfigStringForValue = GetInt32EditorConfigStringForValue;
        private static string GetInt32EditorConfigStringForValue(int value) => value.ToString().ToLowerInvariant();

        private static readonly Func<string, Optional<CodeStyleOption<bool>>> s_parseBoolCodeStyleOption = ParseBoolCodeStyleOption;
        private static Optional<CodeStyleOption<bool>> ParseBoolCodeStyleOption(string str)
            => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, out var result) ? result : new Optional<CodeStyleOption<bool>>();
        private static readonly Func<CodeStyleOption<bool>, string> s_getBoolCodeStyleOptionEditorConfigStringForValue = GetBoolCodeStyleOptionEditorConfigStringForValue;
        private static string GetBoolCodeStyleOptionEditorConfigStringForValue(CodeStyleOption<bool> value)
            => $"{(value.Value ? "true" : "false")}:{value.Notification.ToEditorConfigString()}";

        private static readonly Func<string, Optional<CodeStyleOption<string>>> s_parseStringCodeStyleOption = ParseStringCodeStyleOption;
        private static Optional<CodeStyleOption<string>> ParseStringCodeStyleOption(string str)
            => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, out var result) ? result : new Optional<CodeStyleOption<string>>();
        private static readonly Func<CodeStyleOption<string>, string> s_getStringCodeStyleOptionEditorConfigStringForValue = GetStringCodeStyleOptionEditorConfigStringForValue;
        private static string GetStringCodeStyleOptionEditorConfigStringForValue(CodeStyleOption<string> value)
            => $"{value.Value.ToLowerInvariant()}:{value.Notification.ToEditorConfigString()}";
    }
}
