// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        private static readonly Func<string, Optional<bool>> s_parseBool =
            str => bool.TryParse(str, out var result) ? result : new Optional<bool>();

        private static readonly Func<string, Optional<int>> s_parseInt32 =
            str => int.TryParse(str, out var result) ? result : new Optional<int>();

        private static readonly Func<string, Optional<string>> s_parseString =
            str => str.Equals("unset", StringComparison.Ordinal) ? default(Optional<string>) : str.Replace("\\r", "\r").Replace("\\n", "\n");

        private static readonly Func<bool, string> s_serializeBoolean =
            value => value ? "true" : "false";

        private static readonly Func<int, string> s_serializeInt32 =
            value => value.ToString();

        private static readonly Func<string, string> s_serializeString =
            value => value.ToString().Replace("\r", "\\r").Replace("\n", "\\n");

        public static EditorConfigStorageLocation<bool> ForBoolOption()
            => new(s_parseBool, s_serializeBoolean);

        public static EditorConfigStorageLocation<int> ForInt32Option()
            => new(s_parseInt32, s_serializeInt32);

        public static EditorConfigStorageLocation<string> ForStringOption(string emptyStringRepresentation)
            => new(s_parseString, (string value) => string.IsNullOrEmpty(value) ? emptyStringRepresentation : s_serializeString(value));

        public static EditorConfigStorageLocation<CodeStyleOption2<bool>> ForBoolCodeStyleOption(CodeStyleOption2<bool> defaultValue)
            => new(str => ParseBoolCodeStyleOption(str, defaultValue), value => GetBoolCodeStyleOptionEditorConfigStringForValue(value, defaultValue));

        public static EditorConfigStorageLocation<CodeStyleOption2<string>> ForStringCodeStyleOption(CodeStyleOption2<string> defaultValue)
            => new(str => ParseStringCodeStyleOption(str, defaultValue), value => GetStringCodeStyleOptionEditorConfigStringForValue(value, defaultValue));

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
