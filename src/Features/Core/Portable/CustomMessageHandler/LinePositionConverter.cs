// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal sealed class LinePositionConverter : JsonConverter<LinePosition>
{
    public static LinePositionConverter Instance { get; } = new();

    public override LinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected start array");
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Expected a number");
        }

        var line = reader.GetInt32();

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Expected a number");
        }

        var character = reader.GetInt32();

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException($"Expected end array");
        }

        return new(line, character);
    }

    public override void Write(Utf8JsonWriter writer, LinePosition value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Line);
        writer.WriteNumberValue(value.Character);
        writer.WriteEndArray();
    }
}
