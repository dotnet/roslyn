// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

// copied from https://github.com/dotnet/runtime/issues/98038 to match newtonsoft behavior
internal sealed class NaturalObjectConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadObjectCore(ref reader);

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, runtimeType, options);
        }
    }

    private static object? ReadObjectCore(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.False or JsonTokenType.True:
                return reader.GetBoolean();

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue;
                }

                return reader.GetDouble();

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    var element = ReadObjectCore(ref reader);
                    list.Add(element);
                }
                return list;

            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<JsonElement>(ref reader);

            default:
                throw new JsonException();
        }
    }
}
