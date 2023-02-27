// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigValueSerializer
    {
        private static string EscapeLineBreaks(string str)
            => str.Replace("\\r", "\r").Replace("\\n", "\n");

        private static string UnescapeLineBreaks(string str)
            => str.Replace("\r", "\\r").Replace("\n", "\\n");

        private static readonly EditorConfigValueSerializer<bool> s_bool = new(
            parseValue: ParseBoolean,
            serializeValue: SerializeBoolean);

        private static readonly EditorConfigValueSerializer<int> s_int32 = new(
            parseValue: str => int.TryParse(str, out var result) ? result : new Optional<int>(),
            serializeValue: StringExtensions.GetNumeral);

        private static readonly EditorConfigValueSerializer<string> s_string = new(
            parseValue: str => EscapeLineBreaks(str),
            serializeValue: UnescapeLineBreaks);

        private static readonly EditorConfigValueSerializer<bool?> s_nullableBoolean = new(
            parseValue: ParseNullableBoolean,
            serializeValue: value => value == null ? "null" : SerializeBoolean(value.Value));

        private static Optional<bool> ParseBoolean(string str)
            => bool.TryParse(str, out var result) ? result : new Optional<bool>();

        private static string SerializeBoolean(bool value)
            => value ? "true" : "false";

        private static Optional<bool?> ParseNullableBoolean(string str)
        {
            if (str.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                return new Optional<bool?>(null);
            }

            var optionalBool = ParseBoolean(str);
            return optionalBool.HasValue ? new Optional<bool?>(optionalBool.Value) : new Optional<bool?>();
        }

        public static EditorConfigValueSerializer<T> Default<T>()
        {
            if (typeof(T) == typeof(bool))
                return (EditorConfigValueSerializer<T>)(object)s_bool;

            if (typeof(T) == typeof(int))
                return (EditorConfigValueSerializer<T>)(object)s_int32;

            if (typeof(T) == typeof(string))
                return (EditorConfigValueSerializer<T>)(object)s_string;

            if (typeof(T) == typeof(bool?))
                return (EditorConfigValueSerializer<T>)(object)s_nullableBoolean;

            // TODO: https://github.com/dotnet/roslyn/issues/65787
            // Once all global options define a serializer this should be changed to:
            // throw ExceptionUtilities.UnexpectedValue(typeof(T));
            return EditorConfigValueSerializer<T>.Unsupported;
        }

        public static EditorConfigValueSerializer<string> String(string emptyStringRepresentation)
            => new(parseValue: str => str.Equals(emptyStringRepresentation, StringComparison.Ordinal) ? default(Optional<string>) : EscapeLineBreaks(str),
                   serializeValue: value => string.IsNullOrEmpty(value) ? emptyStringRepresentation : UnescapeLineBreaks(value));

        public static EditorConfigValueSerializer<CodeStyleOption2<T>> CodeStyle<T>(CodeStyleOption2<T> defaultValue)
        {
            if (typeof(T) == typeof(bool))
                return (EditorConfigValueSerializer<CodeStyleOption2<T>>)(object)CodeStyle((CodeStyleOption2<bool>)(object)defaultValue);

            if (typeof(T) == typeof(string))
                return (EditorConfigValueSerializer<CodeStyleOption2<T>>)(object)CodeStyle((CodeStyleOption2<string>)(object)defaultValue);

            throw ExceptionUtilities.UnexpectedValue(typeof(T));
        }

        public static EditorConfigValueSerializer<CodeStyleOption2<bool>> CodeStyle(CodeStyleOption2<bool> defaultValue)
            => new(parseValue: str => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<bool>>(),
                   serializeValue: value => (value.Value ? "true" : "false") + CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue));

        public static EditorConfigValueSerializer<CodeStyleOption2<string>> CodeStyle(CodeStyleOption2<string> defaultValue)
            => new(parseValue: str => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<string>>(),
                   serializeValue: value => value.Value.ToLowerInvariant() + CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue));

        public static EditorConfigValueSerializer<T> CreateSerializerForEnum<T>() where T : struct, Enum
            => new(
                parseValue: str => TryParseEnum<T>(str, out var result) ? new Optional<T>(result) : new Optional<T>(),
                serializeValue: value => value.ToString());

        public static EditorConfigValueSerializer<T?> CreateSerializerForNullableEnum<T>() where T : struct, Enum
        {
            return new EditorConfigValueSerializer<T?>(
                parseValue: ParseValueForNullableEnum,
                serializeValue: value => value == null ? "null" : value.Value.ToString());

            static Optional<T?> ParseValueForNullableEnum(string str)
            {
                if (str.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Optional<T?>(null);
                }

                if (TryParseEnum<T>(str, out var parsedValue))
                {
                    return new Optional<T?>(parsedValue);
                }

                return new Optional<T?>();
            }
        }

        private static bool TryParseEnum<T>(string str, out T result) where T : struct, Enum
        {
            result = default;
            // Block any int value.
            if (int.TryParse(str, out _))
            {
                return false;
            }

            // Enum.TryParse parses every enum as flags enum, we don't want to multiple values to be specified for enums are not flags.
            if (str.Contains(","))
            {
                return false;
            }

            return Enum.TryParse(str, ignoreCase: true, out result);
        }
    }
}
