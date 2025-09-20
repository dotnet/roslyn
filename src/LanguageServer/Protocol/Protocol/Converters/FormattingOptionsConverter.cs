// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>Enables the FormattingOptions.OtherOptions JsonExtensionData to be strongly typed</summary>
internal sealed class FormattingOptionsConverter : JsonConverter<FormattingOptions>
{
    public override FormattingOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new FormattingOptions();

        Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

        static void ReadSkippingComments(ref Utf8JsonReader reader)
        {
            do
            {
                if (!reader.Read())
                {
                    throw new JsonException(LanguageServerProtocolResources.FormattingOptionsEndedUnexpectedly);
                }
            }
            while (reader.TokenType == JsonTokenType.Comment);
        }

        [DoesNotReturn]
        static T ThrowMissingRequiredProperty<T>(string propertyName)
        {
            throw new JsonException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.FormattingOptionsMissingRequiredProperty, propertyName));
        }

        int? tabSize = null;
        bool? insertSpaces = null;
        bool trimTrailingWhitespace = false;
        bool insertFinalNewline = false;
        bool trimFinalNewlines = false;
        Dictionary<string, SumType<bool, int, string>>? otherOptions = null;

        while (true)
        {
            ReadSkippingComments(ref reader);

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new FormattingOptions
                {
                    TabSize = tabSize ?? ThrowMissingRequiredProperty<int>(nameof(tabSize)),
                    InsertSpaces = insertSpaces ?? ThrowMissingRequiredProperty<bool>(nameof(insertSpaces)),
                    TrimTrailingWhitespace = trimTrailingWhitespace,
                    InsertFinalNewline = insertFinalNewline,
                    TrimFinalNewlines = trimFinalNewlines,
                    OtherOptions = otherOptions
                };
            }

            if (reader.TokenType == JsonTokenType.Comment)
            {
                continue;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.FormattingOptionsEncounteredInvalidToken, reader.TokenType));
            }

            var propertyName = reader.GetString();

            ReadSkippingComments(ref reader);

            switch (propertyName)
            {
                case nameof(tabSize):
                    tabSize = reader.GetInt32();
                    continue;
                case nameof(insertSpaces):
                    insertSpaces = reader.GetBoolean();
                    continue;
                case nameof(trimTrailingWhitespace):
                    trimTrailingWhitespace = reader.GetBoolean();
                    continue;
                case nameof(insertFinalNewline):
                    insertFinalNewline = reader.GetBoolean();
                    continue;
                case nameof(trimFinalNewlines):
                    trimFinalNewlines = reader.GetBoolean();
                    continue;
                default:
                    break;
            }

            SumType<bool, int, string> value = reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.True => reader.GetBoolean(),
                JsonTokenType.False => reader.GetBoolean(),
                _ => throw new JsonException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.FormattingOptionsEncounteredInvalidToken, reader.TokenType))
            };

            (otherOptions ??= []).Add(propertyName, value);
        }
    }

    public override void Write(Utf8JsonWriter writer, FormattingOptions value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("tabSize", value.TabSize);
        writer.WriteBoolean("insertSpaces", value.InsertSpaces);

        if (value.TrimTrailingWhitespace != default)
        {
            writer.WriteBoolean("trimTrailingWhitespace", value.TrimTrailingWhitespace);
        }

        if (value.InsertFinalNewline != default)
        {
            writer.WriteBoolean("insertFinalNewline", value.InsertFinalNewline);
        }

        if (value.TrimFinalNewlines != default)
        {
            writer.WriteBoolean("trimFinalNewlines", value.TrimFinalNewlines);
        }

        if (value.OtherOptions is not null)
        {
            foreach (var item in value.OtherOptions)
            {
                writer.WritePropertyName(item.Key);
                JsonSerializer.Serialize(writer, item.Value, options);
            }
        }

        writer.WriteEndObject();
    }
}
