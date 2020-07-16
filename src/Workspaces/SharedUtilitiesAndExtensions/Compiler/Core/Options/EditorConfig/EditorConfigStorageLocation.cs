// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        public static EditorConfigStorageLocation<string> ForStringOption(string keyName, string emptyStringRepresentation)
            => new EditorConfigStorageLocation<string>(keyName, s_parseString, (string value) => string.IsNullOrEmpty(value) ? emptyStringRepresentation : s_getStringEditorConfigStringForValue(value));

        public static EditorConfigStorageLocation<CodeStyleOption2<bool>> ForBoolCodeStyleOption(string keyName)
            => new EditorConfigStorageLocation<CodeStyleOption2<bool>>(keyName, s_parseBoolCodeStyleOption, s_getBoolCodeStyleOptionEditorConfigStringForValue);

        public static EditorConfigStorageLocation<CodeStyleOption2<string>> ForStringCodeStyleOption(string keyName)
            => new EditorConfigStorageLocation<CodeStyleOption2<string>>(keyName, s_parseStringCodeStyleOption, s_getStringCodeStyleOptionEditorConfigStringForValue);

        private static readonly Func<string, Optional<bool>> s_parseBool = ParseBool;
        private static Optional<bool> ParseBool(string str)
            => bool.TryParse(str, out var result) ? result : new Optional<bool>();
        private static readonly Func<bool, string> s_getBoolEditorConfigStringForValue = GetBoolEditorConfigStringForValue;
        private static string GetBoolEditorConfigStringForValue(bool value) => value.ToString().ToLowerInvariant();

        private static readonly Func<string, Optional<int>> s_parseInt32 = ParseInt32;
        private static Optional<int> ParseInt32(string str)
            => int.TryParse(str, out var result) ? result : new Optional<int>();

        private static readonly Func<string, Optional<string>> s_parseString = ParseString;
        private static Optional<string> ParseString(string str)
        {
            if (str.Equals("unset", StringComparison.Ordinal))
            {
                return default;
            }

            str ??= "";
            return str.Replace("\\r", "\r").Replace("\\n", "\n");
        }

        private static readonly Func<int, string> s_getInt32EditorConfigStringForValue = GetInt32EditorConfigStringForValue;
        private static string GetInt32EditorConfigStringForValue(int value) => value.ToString().ToLowerInvariant();

        private static readonly Func<string, string> s_getStringEditorConfigStringForValue = GetStringEditorConfigStringForValue;
        private static string GetStringEditorConfigStringForValue(string value) => value.ToString().Replace("\r", "\\r").Replace("\n", "\\n");

        private static readonly Func<string, Optional<CodeStyleOption2<bool>>> s_parseBoolCodeStyleOption = ParseBoolCodeStyleOption;
        private static Optional<CodeStyleOption2<bool>> ParseBoolCodeStyleOption(string str)
            => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, out var result) ? result : new Optional<CodeStyleOption2<bool>>();
        private static readonly Func<CodeStyleOption2<bool>, string> s_getBoolCodeStyleOptionEditorConfigStringForValue = GetBoolCodeStyleOptionEditorConfigStringForValue;
        private static string GetBoolCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<bool> value)
            => $"{(value.Value ? "true" : "false")}:{value.Notification.ToEditorConfigString()}";

        private static readonly Func<string, Optional<CodeStyleOption2<string>>> s_parseStringCodeStyleOption = ParseStringCodeStyleOption;
        private static Optional<CodeStyleOption2<string>> ParseStringCodeStyleOption(string str)
            => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, out var result) ? result : new Optional<CodeStyleOption2<string>>();
        private static readonly Func<CodeStyleOption2<string>, string> s_getStringCodeStyleOptionEditorConfigStringForValue = GetStringCodeStyleOptionEditorConfigStringForValue;
        private static string GetStringCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<string> value)
            => $"{value.Value.ToLowerInvariant()}:{value.Notification.ToEditorConfigString()}";
    }
}
