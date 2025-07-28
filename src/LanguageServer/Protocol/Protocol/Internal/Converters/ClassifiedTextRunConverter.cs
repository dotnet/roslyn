// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Text.Adornments;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class ClassifiedTextRunConverter : JsonConverter<ClassifiedTextRun>
{
    public static readonly ClassifiedTextRunConverter Instance = new();

    public override ClassifiedTextRun? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var data = JsonDocument.ParseValue(ref reader).RootElement;
            if (data.TryGetProperty(ObjectContentConverter.TypeProperty, out var typeProperty) && typeProperty.GetString() != nameof(ClassifiedTextRun))
            {
                throw new JsonException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ClassifiedTextRun)}");
            }

            var classificationTypeName = data.GetProperty(nameof(ClassifiedTextRun.ClassificationTypeName)).GetString();
            var text = data.GetProperty(nameof(ClassifiedTextRun.Text)).GetString();
            var markerTagType = data.GetProperty(nameof(ClassifiedTextRun.MarkerTagType)).GetString();
            var style = (ClassifiedTextRunStyle)(data.GetProperty(nameof(ClassifiedTextRun.Style)).GetInt32());
            return new ClassifiedTextRun(classificationTypeName, text, style, markerTagType);
        }
        else
        {
            throw new JsonException("Expected start object or null tokens");
        }
    }

    public override void Write(Utf8JsonWriter writer, ClassifiedTextRun value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(ClassifiedTextRun.ClassificationTypeName), value.ClassificationTypeName);
        writer.WriteString(nameof(ClassifiedTextRun.Text), value.Text);
        writer.WriteString(nameof(ClassifiedTextRun.MarkerTagType), value.MarkerTagType);
        writer.WriteNumber(nameof(ClassifiedTextRun.Style), (int)value.Style);
        writer.WriteNull(nameof(ClassifiedTextRun.Tooltip));
        if (value.Tooltip is not null)
        {
            throw new JsonException();
        }

        writer.WriteNull(nameof(ClassifiedTextRun.NavigationAction));
        if (value.NavigationAction is not null)
        {
            throw new JsonException();
        }

        writer.WriteString(ObjectContentConverter.TypeProperty, nameof(ClassifiedTextRun));
        writer.WriteEndObject();
    }
}
