// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Newtonsoft.Json;

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

            return builder.ToImmutable();
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
