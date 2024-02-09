// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using Roslyn.Text.Adornments;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JsonConverter for serializing and deserializing <see cref="ClassifiedTextRun"/>.
/// </summary>
internal class ClassifiedTextRunConverter : JsonConverter
{
    /// <summary>
    /// A reusable instance of the <see cref="ClassifiedTextRunConverter"/>.
    /// </summary>
    public static readonly ClassifiedTextRunConverter Instance = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType)
        => objectType == typeof(ClassifiedTextRun);

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
            if (typeProperty is not null && typeProperty.ToString() != nameof(ClassifiedTextRun))
            {
                throw new JsonSerializationException($"Expected {ObjectContentConverter.TypeProperty} property value {nameof(ClassifiedTextRun)}");
            }

            var classificationTypeName = data[nameof(ClassifiedTextRun.ClassificationTypeName)]?.Value<string>();
            var text = data[nameof(ClassifiedTextRun.Text)]?.Value<string>();
            var markerTagType = data[nameof(ClassifiedTextRun.MarkerTagType)]?.Value<string>();
            var style = (ClassifiedTextRunStyle)(data[nameof(ClassifiedTextRun.Style)]?.Value<int>() ?? 0);
            return new ClassifiedTextRun(classificationTypeName!, text!, style, markerTagType);
        }
        else
        {
            throw new JsonSerializationException("Expected start object or null tokens");
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ClassifiedTextRun classifiedTextRun)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ClassifiedTextRun.ClassificationTypeName));
            writer.WriteValue(classifiedTextRun.ClassificationTypeName);
            writer.WritePropertyName(nameof(ClassifiedTextRun.Text));
            writer.WriteValue(classifiedTextRun.Text);
            writer.WritePropertyName(nameof(ClassifiedTextRun.MarkerTagType));
            writer.WriteValue(classifiedTextRun.MarkerTagType);
            writer.WritePropertyName(nameof(ClassifiedTextRun.Style));
            writer.WriteValue(classifiedTextRun.Style);
            writer.WritePropertyName(nameof(ClassifiedTextRun.Tooltip));
            writer.WriteNull();
            if (classifiedTextRun.Tooltip is not null)
            {
                throw new JsonSerializationException();
            }

            writer.WritePropertyName(nameof(ClassifiedTextRun.NavigationAction));
            writer.WriteNull();
            if (classifiedTextRun.NavigationAction is not null)
            {
                throw new JsonSerializationException();
            }

            writer.WritePropertyName(ObjectContentConverter.TypeProperty);
            writer.WriteValue(nameof(ClassifiedTextRun));
            writer.WriteEndObject();
        }
    }
}
