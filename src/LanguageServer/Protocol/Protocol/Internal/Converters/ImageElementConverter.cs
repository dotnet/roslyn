// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Core.Imaging;
using Roslyn.Text.Adornments;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class ImageElementConverter : JsonConverter<ImageElement>
{
    public static readonly ImageElementConverter Instance = new();

    public override ImageElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            ImageId? imageId = null;
            string? automationName = null;

            Span<char> scratchChars = stackalloc char[64];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    imageId ??= imageId ?? throw new JsonException();
                    return automationName is null ? new ImageElement(imageId.Value) : new ImageElement(imageId.Value, automationName);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var valueLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;

                    var propertyNameLength = valueLength <= scratchChars.Length ? reader.CopyString(scratchChars) : -1;
                    var propertyName = propertyNameLength >= 0 ? scratchChars[..propertyNameLength] : reader.GetString().AsSpan();

                    reader.Read();
                    switch (propertyName)
                    {
                        case nameof(ImageElement.ImageId):
                            imageId = ImageIdConverter.Instance.Read(ref reader, typeof(ImageId), options);
                            break;
                        case nameof(ImageElement.AutomationName):
                            automationName = reader.GetString();
                            break;
                        case ObjectContentConverter.TypeProperty:
                            valueLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;

                            var typePropertyLength = valueLength <= scratchChars.Length ? reader.CopyString(scratchChars) : -1;
                            var typeProperty = typePropertyLength >= 0 ? scratchChars[..typePropertyLength] : reader.GetString().AsSpan();

                            if (!typeProperty.SequenceEqual(nameof(ImageElement).AsSpan()))
                                throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ImageElement)}");
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

    public override void Write(Utf8JsonWriter writer, ImageElement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(ImageElement.ImageId));
        ImageIdConverter.Instance.Write(writer, value.ImageId, options);

        if (value.AutomationName != null)
            writer.WriteString(nameof(ImageElement.AutomationName), value.AutomationName);

        writer.WriteString(ObjectContentConverter.TypeProperty, nameof(ImageElement));
        writer.WriteEndObject();
    }
}
