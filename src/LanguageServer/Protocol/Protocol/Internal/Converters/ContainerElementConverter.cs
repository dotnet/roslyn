// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Text.Adornments;

namespace Roslyn.LanguageServer.Protocol;
/// <summary>
/// System.Text.Json.JsonConverter for serializing and deserializing <see cref="ContainerElement"/>.
/// </summary>
internal sealed class ContainerElementConverter : JsonConverter<ContainerElement>
{
    public static readonly ContainerElementConverter Instance = new();

    public override ContainerElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            ContainerElementStyle? style = null;
            List<object> objects = [];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new ContainerElement(style ?? throw new JsonException(), objects);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case nameof(ContainerElement.Elements):
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndArray)
                                    break;

                                objects.Add(ObjectContentConverter.Instance.Read(ref reader, typeof(object), options));
                            }

                            break;
                        case nameof(ContainerElement.Style):
                            style = (ContainerElementStyle)reader.GetInt32();
                            break;
                        case ObjectContentConverter.TypeProperty:
                            if (reader.GetString() != nameof(ContainerElement))
                                throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ContainerElement)}");
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ContainerElement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(ContainerElement.Elements));
        writer.WriteStartArray();
        foreach (var run in value.Elements)
        {
            ObjectContentConverter.Instance.Write(writer, run, options);
        }
        writer.WriteEndArray();
        writer.WriteNumber(nameof(ContainerElement.Style), (int)value.Style);
        writer.WritePropertyName(ObjectContentConverter.TypeProperty);
        writer.WriteStringValue(nameof(ContainerElement));
        writer.WriteEndObject();
    }
}
