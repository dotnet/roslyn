// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Roslyn.LanguageServer.Protocol;

// copied from https://github.com/dotnet/runtime/issues/98038 to match newtonsoft behavior
internal sealed class NaturalObjectConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadObjectCore(ref reader, options);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        => JsonMetadataServices.ObjectConverter.WriteAsPropertyName(writer, value, options);

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
        else if (value is JsonNode node)
        {
            node.WriteTo(writer, options);
        }
        else if (value is Exception exception)
        {
            // StreamJsonRpc registers its exception converter for the base type, not each concrete exception.
            JsonSerializer.Serialize(writer, exception, options);
        }
        else if (value is IDictionary dictionary && !options.TryGetTypeInfo(runtimeType, out _))
        {
            // Exception.Data can be a private framework dictionary that generated metadata cannot name.
            JsonSerializer.Serialize(writer, dictionary, options);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, runtimeType, options);
        }
    }

    private static object? ReadObjectCore(ref Utf8JsonReader reader, JsonSerializerOptions options)
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
                    var element = ReadObjectCore(ref reader, options);
                    list.Add(element);
                }
                return list;

            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            default:
                throw new JsonException();
        }
    }
}
