// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        public static readonly AggregateJsonConverter Instance = new AggregateJsonConverter();

        private readonly ConcurrentDictionary<Type, JsonConverter> _map;

        private AggregateJsonConverter()
        {
            _map = new ConcurrentDictionary<Type, JsonConverter>(CreateConverterMap());
        }

        public override bool CanConvert(Type objectType)
            => _map.ContainsKey(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => _map[objectType].ReadJson(reader, objectType, existingValue, serializer);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => _map[value.GetType()].WriteJson(writer, value, serializer);

        // this type is shared by multiple teams such as Razor, LUT and etc which have either 
        // separated/shared/shim repo so some types might not available to those context. this 
        // partial method let us add Roslyn specific types without breaking them
        partial void AppendRoslynSpecificJsonConverters(ImmutableDictionary<Type, JsonConverter>.Builder builder);

        private ImmutableDictionary<Type, JsonConverter> CreateConverterMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, JsonConverter>();

            Add(builder, new ChecksumJsonConverter());
            Add(builder, new SolutionIdJsonConverter());
            Add(builder, new ProjectIdJsonConverter());
            Add(builder, new DocumentIdJsonConverter());
            Add(builder, new TextSpanJsonConverter());
            Add(builder, new TextChangeJsonConverter());
            Add(builder, new SymbolKeyJsonConverter());
            Add(builder, new PinnedSolutionInfoJsonConverter());

            AppendRoslynSpecificJsonConverters(builder);

            return builder.ToImmutable();
        }

        private static void Add<T>(
            ImmutableDictionary<Type, JsonConverter>.Builder builder,
            BaseJsonConverter<T> converter)
            => builder.Add(typeof(T), converter);

        internal bool TryAdd(Type type, JsonConverter converter)
            => _map.TryAdd(type, converter);

        private abstract class BaseJsonConverter<T> : JsonConverter
        {
            public sealed override bool CanConvert(Type objectType) => typeof(T) == objectType;

            public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                => ReadValue(reader, serializer);

            public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => WriteValue(writer, (T)value, serializer);

            protected abstract T ReadValue(JsonReader reader, JsonSerializer serializer);
            protected abstract void WriteValue(JsonWriter writer, T value, JsonSerializer serializer);

            protected static U ReadProperty<U>(JsonReader reader, JsonSerializer serializer)
            {
                // read property
                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.PropertyName);

                Contract.ThrowIfFalse(reader.Read());
                return serializer.Deserialize<U>(reader);
            }

            protected static U ReadProperty<U>(JsonReader reader)
            {
                // read property
                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.PropertyName);

                Contract.ThrowIfFalse(reader.Read());
                return (U)reader.Value;
            }
        }

        private class TextSpanJsonConverter : BaseJsonConverter<TextSpan>
        {
            protected override TextSpan ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var start = ReadProperty<long>(reader);
                var length = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TextSpan((int)start, (int)length);
            }

            protected override void WriteValue(JsonWriter writer, TextSpan span, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TextSpan.Start));
                writer.WriteValue(span.Start);

                writer.WritePropertyName(nameof(TextSpan.Length));
                writer.WriteValue(span.Length);

                writer.WriteEndObject();
            }
        }

        private class TextChangeJsonConverter : BaseJsonConverter<TextChange>
        {
            protected override TextChange ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var start = ReadProperty<long>(reader);
                var length = ReadProperty<long>(reader);
                var newText = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TextChange(new TextSpan((int)start, (int)length), newText);
            }

            protected override void WriteValue(JsonWriter writer, TextChange change, JsonSerializer serializer)
            {
                var span = change.Span;

                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TextSpan.Start));
                writer.WriteValue(span.Start);

                writer.WritePropertyName(nameof(TextSpan.Length));
                writer.WriteValue(span.Length);

                writer.WritePropertyName(nameof(TextChange.NewText));
                writer.WriteValue(change.NewText);

                writer.WriteEndObject();
            }
        }

        private class SymbolKeyJsonConverter : BaseJsonConverter<SymbolKey>
        {
            protected override SymbolKey ReadValue(JsonReader reader, JsonSerializer serializer)
                => new SymbolKey((string)reader.Value);

            protected override void WriteValue(JsonWriter writer, SymbolKey value, JsonSerializer serializer)
                => writer.WriteValue(value.ToString());
        }

        private class ChecksumJsonConverter : BaseJsonConverter<Checksum>
        {
            protected override Checksum ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                var value = (string)reader.Value;
                return value == null ? null : Checksum.FromSerialized(Convert.FromBase64String(value));
            }

            protected override void WriteValue(JsonWriter writer, Checksum value, JsonSerializer serializer)
                => writer.WriteValue(value?.ToString());
        }

        private class PinnedSolutionInfoJsonConverter : BaseJsonConverter<PinnedSolutionInfo>
        {
            protected override PinnedSolutionInfo ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var scopeId = ReadProperty<long>(reader);
                var fromPrimaryBranch = ReadProperty<bool>(reader);
                var workspaceVersion = ReadProperty<long>(reader);
                var checksum = ReadProperty<Checksum>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new PinnedSolutionInfo((int)scopeId, fromPrimaryBranch, (int)workspaceVersion, checksum);
            }

            protected override void WriteValue(JsonWriter writer, PinnedSolutionInfo scope, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("scopeId");
                writer.WriteValue(scope.ScopeId);

                writer.WritePropertyName("primary");
                writer.WriteValue(scope.FromPrimaryBranch);

                writer.WritePropertyName("version");
                writer.WriteValue(scope.WorkspaceVersion);

                writer.WritePropertyName("checksum");
                serializer.Serialize(writer, scope.SolutionChecksum);

                writer.WriteEndObject();
            }
        }
    }
}
