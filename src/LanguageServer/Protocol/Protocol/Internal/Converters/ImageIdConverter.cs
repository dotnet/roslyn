// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Core.Imaging;

namespace Roslyn.LanguageServer.Protocol;
internal class ImageIdConverter : JsonConverter<ImageId>
{
    public static readonly ImageIdConverter Instance = new();

    public override ImageId Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.TryGetProperty(ObjectContentConverter.TypeProperty, out var typeProperty) && typeProperty.GetString() != nameof(ImageId))
            {
                throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ImageId)}");
            }

            var guid = root.GetProperty(nameof(ImageId.Guid)).GetString() ?? throw new JsonException();
            var id = root.GetProperty(nameof(ImageId.Id)).GetInt32();
            return new ImageId(new Guid(guid), id);
        }
        else
        {
            throw new JsonException("Expected start object or null tokens");
        }
    }

    public override void Write(Utf8JsonWriter writer, ImageId value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(ImageId.Guid), value.Guid.ToString());
        writer.WriteNumber(nameof(ImageId.Id), value.Id);
        writer.WriteString(ObjectContentConverter.TypeProperty, nameof(ImageId));
        writer.WriteEndObject();
    }
}
