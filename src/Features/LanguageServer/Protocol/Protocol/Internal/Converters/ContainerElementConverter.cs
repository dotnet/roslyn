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
/// JsonConverter for serializing and deserializing <see cref="ContainerElement"/>.
/// </summary>
internal class ContainerElementConverter : JsonConverter
{
    /// <summary>
    /// A reusable instance of the <see cref="ContainerElementConverter"/>.
    /// </summary>
    public static readonly ContainerElementConverter Instance = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(ContainerElement);

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
            if (typeProperty is not null && typeProperty.ToString() != nameof(ContainerElement))
            {
                throw new JsonSerializationException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ContainerElement)}");
            }

            var elementTokens = data[nameof(ContainerElement.Elements)]?.ToArray() ??
                throw new JsonSerializationException($"Missing {nameof(ContainerElement.Elements)} property");
            var elements = new object?[elementTokens.Length];
            for (var i = 0; i < elementTokens.Length; i++)
            {
                var elementTokenReader = elementTokens[i].CreateReader();
                elementTokenReader.Read();
                elements[i] = ObjectContentConverter.Instance.ReadJson(elementTokenReader, typeof(object), null, serializer);
            }

            var style = (ContainerElementStyle)(data[nameof(ContainerElement.Style)]?.Value<int>() ?? throw new JsonSerializationException());
            return new ContainerElement(style, elements);
        }
        else
        {
            throw new JsonSerializationException("Expected start object or null tokens");
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ContainerElement containerElement)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ContainerElement.Elements));
            writer.WriteStartArray();
            foreach (var run in containerElement.Elements)
            {
                ObjectContentConverter.Instance.WriteJson(writer, run, serializer);
            }

            writer.WriteEndArray();
            writer.WritePropertyName(nameof(ContainerElement.Style));
            writer.WriteValue(containerElement.Style);
            writer.WritePropertyName(ObjectContentConverter.TypeProperty);
            writer.WriteValue(nameof(ContainerElement));
            writer.WriteEndObject();
        }
    }
}
