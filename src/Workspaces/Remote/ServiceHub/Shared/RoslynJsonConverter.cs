// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        public static readonly AggregateJsonConverter Instance = new AggregateJsonConverter();

        private readonly ImmutableDictionary<Type, JsonConverter> _map;

        private AggregateJsonConverter()
        {
            _map = CreateConverterMap();
        }

        public override bool CanConvert(Type objectType)
        {
            return _map.ContainsKey(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return _map[objectType].ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _map[value.GetType()].WriteJson(writer, value, serializer);
        }

        private ImmutableDictionary<Type, JsonConverter> CreateConverterMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, JsonConverter>();

            builder.Add(typeof(Checksum), new ChecksumJsonConverter());
            builder.Add(typeof(SolutionId), new SolutionIdJsonConverter());
            builder.Add(typeof(ProjectId), new ProjectIdJsonConverter());
            builder.Add(typeof(DocumentId), new DocumentIdJsonConverter());
            builder.Add(typeof(TextSpan), new TextSpanJsonConverter());
            builder.Add(typeof(SymbolKey), new SymbolKeyJsonConverter());

            return builder.ToImmutable();
        }

        private abstract class BaseJsonConverter : JsonConverter
        {
            protected static T ReadProperty<T>(JsonSerializer serializer, JsonReader reader)
            {
                // read property
                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.PropertyName);

                Contract.ThrowIfFalse(reader.Read());
                return serializer.Deserialize<T>(reader);
            }

            protected static T ReadProperty<T>(JsonReader reader)
            {
                // read property
                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.PropertyName);

                Contract.ThrowIfFalse(reader.Read());
                return (T)reader.Value;
            }
        }

        private class TextSpanJsonConverter : BaseJsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(TextSpan) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var start = ReadProperty<long>(reader);
                var length = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TextSpan((int)start, (int)length);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var span = (TextSpan)value;

                writer.WriteStartObject();

                writer.WritePropertyName("start");
                writer.WriteValue(span.Start);

                writer.WritePropertyName("length");
                writer.WriteValue(span.Length);

                writer.WriteEndObject();
            }
        }

        private class SymbolKeyJsonConverter : BaseJsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(SymbolKey) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                => new SymbolKey((string)reader.Value);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                writer.WriteValue(value.ToString());
        }

        private class ChecksumJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(Checksum) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
                new Checksum(Convert.FromBase64String((string)reader.Value));

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                writer.WriteValue(value.ToString());
        }
    }
}
