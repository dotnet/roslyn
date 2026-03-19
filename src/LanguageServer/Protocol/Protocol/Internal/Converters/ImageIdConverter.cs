// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Core.Imaging;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class ImageIdConverter : JsonConverter<ImageId>
{
    public static readonly ImageIdConverter Instance = new();

    public override ImageId Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            Guid? guid = null;
            int? id = null;

            Span<char> scratchChars = stackalloc char[64];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (guid is null || id is null)
                        throw new JsonException("Expected properties Guid and Id to be present");

                    return new ImageId(guid.Value, id.Value);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetStringSpan(scratchChars);

                    reader.Read();
                    switch (propertyName)
                    {
                        case nameof(ImageId.Guid):
                            guid = reader.GetGuid();
                            break;
                        case nameof(ImageId.Id):
                            id = reader.GetInt32();
                            break;
                        case ObjectContentConverter.TypeProperty:
                            var typePropertyValue = reader.GetStringSpan(scratchChars);

                            if (!typePropertyValue.SequenceEqual(nameof(ImageId).AsSpan()))
                                throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ImageId)}");
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        throw new JsonException("Expected start object or null tokens");
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
