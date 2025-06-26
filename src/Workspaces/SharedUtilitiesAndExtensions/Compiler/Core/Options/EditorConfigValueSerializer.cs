// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

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

    public static EditorConfigValueSerializer<T> GetDefault<T>(bool isEditorConfigOption)
    {
        if (typeof(T) == typeof(bool))
            return (EditorConfigValueSerializer<T>)(object)s_bool;

        if (typeof(T) == typeof(int))
            return (EditorConfigValueSerializer<T>)(object)s_int32;

        if (typeof(T) == typeof(string))
            return (EditorConfigValueSerializer<T>)(object)s_string;

        if (typeof(T) == typeof(bool?))
            return (EditorConfigValueSerializer<T>)(object)s_nullableBoolean;

        // editorconfig options must have a serializer:
        if (isEditorConfigOption)
            throw ExceptionUtilities.UnexpectedValue(typeof(T));

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

    /// <summary>
    /// Creates a serializer for an enum value that uses the enum field names.
    /// </summary>
    public static EditorConfigValueSerializer<T> CreateSerializerForEnum<T>() where T : struct, Enum
        => new(
            parseValue: str => TryParseEnum<T>(str, out var result) ? new Optional<T>(result) : new Optional<T>(),
            serializeValue: value => value.ToString());

    /// <summary>
    /// Creates a serializer for an enum value given a <paramref name="map"/> between value names and the corresponding enum values.
    /// </summary>
    public static EditorConfigValueSerializer<T> CreateSerializerForEnum<T>(BidirectionalMap<string, T> map) where T : struct, Enum
        => CreateSerializerForEnum(map, ImmutableDictionary<string, T>.Empty);

    /// <summary>
    /// Creates a serializer for an enum value given a <paramref name="map"/> between value names and the corresponding enum values.
    /// <paramref name="alternative"/> specifies alternative value representations for backward compatibility.
    /// </summary>
    public static EditorConfigValueSerializer<T> CreateSerializerForEnum<T>(BidirectionalMap<string, T> map, ImmutableDictionary<string, T> alternative) where T : struct, Enum
        => new(parseValue: str => map.TryGetValue(str, out var result) || alternative.TryGetValue(str, out result) ? new Optional<T>(result) : new Optional<T>(),
               serializeValue: value => map.TryGetKey(value, out var key) ? key : throw ExceptionUtilities.UnexpectedValue(value));

    /// <summary>
    /// Creates a serializer for an enum value given a <paramref name="entries"/> between value names and the corresponding enum values.
    /// <paramref name="alternativeEntries"/> specifies alternative value representations for backward compatibility.
    /// </summary>
    public static EditorConfigValueSerializer<T> CreateSerializerForEnum<T>(IEnumerable<(string name, T value)> entries, IEnumerable<(string name, T value)> alternativeEntries) where T : struct, Enum
    {
        var map = new BidirectionalMap<string, T>(entries, StringComparer.OrdinalIgnoreCase);
        var alternativeMap = ImmutableDictionary<string, T>.Empty.WithComparers(keyComparer: StringComparer.OrdinalIgnoreCase)
            .AddRange(alternativeEntries.Select(static p => KeyValuePair.Create(p.name, p.value)));

        return CreateSerializerForEnum(map, alternativeMap);
    }

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

    /// <summary>
    /// Serializes arbitrary editorconfig option value (including naming style preferences) into a given builder.
    /// Replaces existing value if present.
    /// </summary>
    public static void Serialize(IDictionary<string, string> builder, IOption2 option, string language, object? value)
    {
        if (value is NamingStylePreferences preferences)
        {
            // remove existing naming style values:
            foreach (var name in builder.Keys)
            {
                if (name.StartsWith("dotnet_naming_rule.") || name.StartsWith("dotnet_naming_symbols.") || name.StartsWith("dotnet_naming_style."))
                {
                    builder.Remove(name);
                }
            }

            NamingStylePreferencesEditorConfigSerializer.WriteNamingStylePreferencesToEditorConfig(
                preferences.SymbolSpecifications,
                preferences.NamingStyles,
                preferences.Rules.NamingRules,
                language,
                entryWriter: (name, value) => builder[name] = value,
                triviaWriter: null,
                setPrioritiesToPreserveOrder: true);
        }
        else
        {
            builder[option.Definition.ConfigName] = option.Definition.Serializer.Serialize(value);
        }
    }

    public static EditorConfigValueSerializer<TToEnum>? ConvertEnumSerializer<TFromEnum, TToEnum>(EditorConfigValueSerializer<TFromEnum> serializer)
        where TFromEnum : struct, Enum
        where TToEnum : struct, Enum
    {
        return new(
            value => serializer.ParseValue(value).ConvertEnum<TFromEnum, TToEnum>(),
            value => serializer.SerializeValue(EnumValueUtilities.ConvertEnum<TToEnum, TFromEnum>(value)));
    }
}
