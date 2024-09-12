// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using Roslyn.Core.Imaging;
using Roslyn.Text.Adornments;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JsonConverter for serializing and deserializing <see cref="ImageElement"/>.
/// </summary>
internal class ImageElementConverter : JsonConverter
{
    /// <summary>
    /// A reusable instance of the <see cref="ImageElementConverter"/>.
    /// </summary>
    public static readonly ImageElementConverter Instance = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(ImageElement);

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
            if (typeProperty is not null && typeProperty.ToString() != nameof(ImageElement))
            {
                throw new JsonSerializationException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ImageElement)}");
            }

            var imageTokenReader = data[nameof(ImageElement.ImageId)]?.CreateReader() ?? throw new JsonSerializationException();
            imageTokenReader.Read();
            var imageId = (ImageId)ImageIdConverter.Instance.ReadJson(imageTokenReader, typeof(ImageId), null, serializer)!;
            var automationName = data[nameof(ImageElement.AutomationName)]?.Value<string>();
            return automationName is null ? new ImageElement(imageId) : new ImageElement(imageId, automationName);
        }
        else
        {
            throw new JsonSerializationException("Expected start object or null tokens");
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ImageElement imageElement)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ImageElement.ImageId));
            ImageIdConverter.Instance.WriteJson(writer, imageElement.ImageId, serializer);
            writer.WritePropertyName(nameof(ImageElement.AutomationName));
            writer.WriteValue(imageElement.AutomationName);
            writer.WritePropertyName(ObjectContentConverter.TypeProperty);
            writer.WriteValue(nameof(ImageElement));
            writer.WriteEndObject();
        }
    }
}
