// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Linq;
using Roslyn.Text.Adornments;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JsonConverter for serializing and deserializing <see cref="ClassifiedTextElement"/>.
/// </summary>
internal class ClassifiedTextElementConverter : JsonConverter
{
    /// <summary>
    /// A reusable instance of the <see cref="ClassifiedTextElementConverter"/>.
    /// </summary>
    public static readonly ClassifiedTextElementConverter Instance = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(ClassifiedTextElement);

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            reader.Read();
            return null;
        }
        else if (reader.TokenType == JsonToken.StartObject)
        {
            var data = JObject.Load(reader);
            var typeProperty = data[ObjectContentConverter.TypeProperty];
            if (typeProperty is not null && typeProperty.ToString() != nameof(ClassifiedTextElement))
            {
                throw new JsonSerializationException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ClassifiedTextElement)}");
            }

            var runTokens = data[nameof(ClassifiedTextElement.Runs)]?.ToArray() ??
                throw new JsonSerializationException($"Missing {nameof(ClassifiedTextElement.Runs)} property");
            var runs = new ClassifiedTextRun[runTokens.Length];
            for (var i = 0; i < runTokens.Length; i++)
            {
                var runTokenReader = runTokens[i].CreateReader();
                runTokenReader.Read();
                runs[i] = (ClassifiedTextRun)ClassifiedTextRunConverter.Instance.ReadJson(runTokenReader, typeof(ClassifiedTextRun), null, serializer)!;
            }

            return new ClassifiedTextElement(runs);
        }
        else
        {
            throw new JsonSerializationException("Expected start object or null tokens");
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ClassifiedTextElement classifiedTextElement)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ClassifiedTextElement.Runs));
            writer.WriteStartArray();
            foreach (var run in classifiedTextElement.Runs)
            {
                ClassifiedTextRunConverter.Instance.WriteJson(writer, run, serializer);
            }

            writer.WriteEndArray();
            writer.WritePropertyName(ObjectContentConverter.TypeProperty);
            writer.WriteValue(nameof(ClassifiedTextElement));
            writer.WriteEndObject();
        }
    }
}
