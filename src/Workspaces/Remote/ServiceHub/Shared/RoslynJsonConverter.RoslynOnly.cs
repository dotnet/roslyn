// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.TodoComments;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        partial void AppendRoslynSpecificJsonConverters(ImmutableDictionary<Type, JsonConverter>.Builder builder)
        {
            Add(builder, new TodoCommentDescriptorJsonConverter());
            Add(builder, new TodoCommentJsonConverter());
            Add(builder, new PackageSourceJsonConverter());
        }

        private class TodoCommentDescriptorJsonConverter : BaseJsonConverter<TodoCommentDescriptor>
        {
            protected override TodoCommentDescriptor ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var text = ReadProperty<string>(reader);
                var priority = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TodoCommentDescriptor(text, (int)priority);
            }

            protected override void WriteValue(JsonWriter writer, TodoCommentDescriptor descriptor, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("text");
                writer.WriteValue(descriptor.Text);

                writer.WritePropertyName("priority");
                writer.WriteValue(descriptor.Priority);

                writer.WriteEndObject();
            }
        }

        private class TodoCommentJsonConverter : BaseJsonConverter<TodoComment>
        {
            protected override TodoComment ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var descriptor = ReadProperty<TodoCommentDescriptor>(serializer, reader);
                var message = ReadProperty<string>(reader);
                var position = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TodoComment(descriptor, message, (int)position);
            }

            protected override void WriteValue(JsonWriter writer, TodoComment todoComment, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("descriptor");
                serializer.Serialize(writer, todoComment.Descriptor);

                writer.WritePropertyName("message");
                writer.WriteValue(todoComment.Message);

                writer.WritePropertyName("position");
                writer.WriteValue(todoComment.Position);

                writer.WriteEndObject();
            }
        }

        private class PackageSourceJsonConverter : BaseJsonConverter<PackageSource>
        {
            protected override PackageSource ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var name = ReadProperty<string>(reader);
                var source = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new PackageSource(name, source);
            }

            protected override void WriteValue(JsonWriter writer, PackageSource source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");
                writer.WriteValue(source.Name);

                writer.WritePropertyName("source");
                writer.WriteValue(source.Source);

                writer.WriteEndObject();
            }
        }
    }
}