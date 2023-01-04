// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        private static string EscapeLineBreaks(string str)
            => str.Replace("\\r", "\r").Replace("\\n", "\n");

        private static string UnescapeLineBreaks(string str)
            => str.Replace("\r", "\\r").Replace("\n", "\\n");

        private static readonly EditorConfigStorageLocation<bool> s_bool = new(
            parseValue: str => bool.TryParse(str, out var result) ? result : new Optional<bool>(),
            serializeValue: value => value ? "true" : "false");

        private static readonly EditorConfigStorageLocation<int> s_int32 = new(
            parseValue: str => int.TryParse(str, out var result) ? result : new Optional<int>(),
            serializeValue: StringExtensions.GetNumeral);

        private static readonly EditorConfigStorageLocation<string> s_string = new(
            parseValue: str => EscapeLineBreaks(str),
            serializeValue: UnescapeLineBreaks);

        public static EditorConfigStorageLocation<T> Default<T>()
        {
            if (typeof(T) == typeof(bool))
                return (EditorConfigStorageLocation<T>)(object)s_bool;

            if (typeof(T) == typeof(int))
                return (EditorConfigStorageLocation<T>)(object)s_int32;

            if (typeof(T) == typeof(string))
                return (EditorConfigStorageLocation<T>)(object)s_string;

            throw ExceptionUtilities.UnexpectedValue(typeof(T));
        }

        public static EditorConfigStorageLocation<string> String(string emptyStringRepresentation)
            => new(parseValue: str => str.Equals(emptyStringRepresentation, StringComparison.Ordinal) ? default(Optional<string>) : EscapeLineBreaks(str),
                   serializeValue: value => string.IsNullOrEmpty(value) ? emptyStringRepresentation : UnescapeLineBreaks(value));

        public static EditorConfigStorageLocation<CodeStyleOption2<T>> CodeStyle<T>(CodeStyleOption2<T> defaultValue)
        {
            if (typeof(T) == typeof(bool))
                return (EditorConfigStorageLocation<CodeStyleOption2<T>>)(object)CodeStyle((CodeStyleOption2<bool>)(object)defaultValue);

            if (typeof(T) == typeof(string))
                return (EditorConfigStorageLocation<CodeStyleOption2<T>>)(object)CodeStyle((CodeStyleOption2<string>)(object)defaultValue);

            throw ExceptionUtilities.UnexpectedValue(typeof(T));
        }

        public static EditorConfigStorageLocation<CodeStyleOption2<bool>> CodeStyle(CodeStyleOption2<bool> defaultValue)
            => new(parseValue: str => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<bool>>(),
                   serializeValue: value => (value.Value ? "true" : "false") + CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue));

        public static EditorConfigStorageLocation<CodeStyleOption2<string>> CodeStyle(CodeStyleOption2<string> defaultValue)
            => new(parseValue: str => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<string>>(),
                   serializeValue: value => value.Value.ToLowerInvariant() + CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue));
    }
}
