// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        public static EditorConfigStorageLocation<bool> ForBoolOption(string keyName)
            => new(keyName, s_parseBool, s_getBoolEditorConfigStringForValue);

        public static EditorConfigStorageLocation<int> ForInt32Option(string keyName)
            => new(keyName, s_parseInt32, s_getInt32EditorConfigStringForValue);

        public static EditorConfigStorageLocation<string> ForStringOption(string keyName, string emptyStringRepresentation)
            => new(keyName, s_parseString, (string value) => string.IsNullOrEmpty(value) ? emptyStringRepresentation : s_getStringEditorConfigStringForValue(value));

        public static EditorConfigStorageLocation<CodeStyleOption2<bool>> ForBoolCodeStyleOption(string keyName, CodeStyleOption2<bool> defaultValue)
            => new(keyName, str => ParseBoolCodeStyleOption(str, defaultValue), value => GetBoolCodeStyleOptionEditorConfigStringForValue(value, defaultValue));

        public static EditorConfigStorageLocation<CodeStyleOption2<string>> ForStringCodeStyleOption(string keyName, CodeStyleOption2<string> defaultValue)
            => new(keyName, str => ParseStringCodeStyleOption(str, defaultValue), value => GetStringCodeStyleOptionEditorConfigStringForValue(value, defaultValue));

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

        private static Optional<CodeStyleOption2<bool>> ParseBoolCodeStyleOption(string str, CodeStyleOption2<bool> defaultValue)
            => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<bool>>();
        private static string GetBoolCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<bool> value, CodeStyleOption2<bool> defaultValue)
            => $"{(value.Value ? "true" : "false")}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue)}";

        private static Optional<CodeStyleOption2<string>> ParseStringCodeStyleOption(string str, CodeStyleOption2<string> defaultValue)
            => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<string>>();
        private static string GetStringCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<string> value, CodeStyleOption2<string> defaultValue)
            => $"{value.Value.ToLowerInvariant()}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue)}";
    }
}
