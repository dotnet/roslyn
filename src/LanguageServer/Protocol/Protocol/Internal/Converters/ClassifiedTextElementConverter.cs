// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Text.Adornments;

namespace Roslyn.LanguageServer.Protocol;
internal class ClassifiedTextElementConverter : JsonConverter<ClassifiedTextElement>
{
    public static readonly ClassifiedTextElementConverter Instance = new();

    public override ClassifiedTextElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<ClassifiedTextRun> objects = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new ClassifiedTextElement(objects);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case nameof(ClassifiedTextElement.Runs):
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;

                            objects.Add(ClassifiedTextRunConverter.Instance.Read(ref reader, typeof(ClassifiedTextRun), options)!);
                        }

                        break;
                    case ObjectContentConverter.TypeProperty:
                        if (reader.GetString() != nameof(ClassifiedTextElement))
                            throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ClassifiedTextElement)}");
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ClassifiedTextElement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(ClassifiedTextElement.Runs));
        writer.WriteStartArray();
        foreach (var run in value.Runs)
        {
            ClassifiedTextRunConverter.Instance.Write(writer, run, options);
        }

        writer.WriteEndArray();
        writer.WritePropertyName(ObjectContentConverter.TypeProperty);
        writer.WriteStringValue(nameof(ClassifiedTextElement));
        writer.WriteEndObject();
    }
}
