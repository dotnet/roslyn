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
/// JsonConverter for serializing and deserializing <see cref="ImageId"/>.
/// </summary>
internal class ImageIdConverter : JsonConverter
{
    /// <summary>
    /// A reusable instance of the <see cref="ImageIdConverter"/>.
    /// </summary>
    public static readonly ImageIdConverter Instance = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(ImageId);

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
            if (typeProperty is not null && typeProperty.ToString() != nameof(ImageId))
            {
                throw new JsonSerializationException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ImageId)}");
            }

            var guid = data[nameof(ImageId.Guid)]?.Value<string>() ?? throw new JsonSerializationException();
            var id = data[nameof(ImageId.Id)]?.Value<int>() ?? throw new JsonSerializationException();
            return new ImageId(new Guid(guid), id);
        }
        else
        {
            throw new JsonSerializationException("Expected start object or null tokens");
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ImageId imageId)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ImageId.Guid));
            writer.WriteValue(imageId.Guid);
            writer.WritePropertyName(nameof(ImageId.Id));
            writer.WriteValue(imageId.Id);
            writer.WritePropertyName(ObjectContentConverter.TypeProperty);
            writer.WriteValue(nameof(ImageId));
            writer.WriteEndObject();
        }
    }
}
