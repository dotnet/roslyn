// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
#pragma warning disable CA1822 // Mark members as static
        // this type is shared by multiple teams such as Razor, LUT and etc which have either 
        // separated/shared/shim repo so some types might not available to those context. this 
        // partial method let us add Roslyn specific types without breaking them
        partial void AppendRoslynSpecificJsonConverters(ImmutableDictionary<Type, JsonConverter>.Builder builder)
#pragma warning restore CA1822 // Mark members as static
        {
            Add(builder, new HighlightSpanJsonConverter());
            Add(builder, new TaggedTextJsonConverter());

            Add(builder, new TodoCommentDescriptorJsonConverter());
            Add(builder, new TodoCommentJsonConverter());

            Add(builder, new AnalyzerPerformanceInfoConverter());
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
                var descriptor = ReadProperty<TodoCommentDescriptor>(reader, serializer);
                var message = ReadProperty<string>(reader);
                var position = ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TodoComment(descriptor, message, (int)position);
            }

            protected override void WriteValue(JsonWriter writer, TodoComment todoComment, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TodoComment.Descriptor));
                serializer.Serialize(writer, todoComment.Descriptor);

                writer.WritePropertyName(nameof(TodoComment.Message));
                writer.WriteValue(todoComment.Message);

                writer.WritePropertyName(nameof(TodoComment.Position));
                writer.WriteValue(todoComment.Position);

                writer.WriteEndObject();
            }
        }

        private class HighlightSpanJsonConverter : BaseJsonConverter<HighlightSpan>
        {
            protected override HighlightSpan ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var textSpan = ReadProperty<TextSpan>(reader, serializer);
                var kind = (HighlightSpanKind)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new HighlightSpan(textSpan, kind);
            }

            protected override void WriteValue(JsonWriter writer, HighlightSpan source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(HighlightSpan.TextSpan));
                serializer.Serialize(writer, source.TextSpan);

                writer.WritePropertyName(nameof(HighlightSpan.Kind));
                writer.WriteValue(source.Kind);

                writer.WriteEndObject();
            }
        }

        private class TaggedTextJsonConverter : BaseJsonConverter<TaggedText>
        {
            protected override TaggedText ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var tag = ReadProperty<string>(reader);
                var text = ReadProperty<string>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new TaggedText(tag, text);
            }

            protected override void WriteValue(JsonWriter writer, TaggedText source, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(TaggedText.Tag));
                writer.WriteValue(source.Tag);

                writer.WritePropertyName(nameof(TaggedText.Text));
                writer.WriteValue(source.Text);

                writer.WriteEndObject();
            }
        }

        private class AnalyzerPerformanceInfoConverter : BaseJsonConverter<AnalyzerPerformanceInfo>
        {
            protected override AnalyzerPerformanceInfo ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var analyzerid = ReadProperty<string>(reader);
                var builtIn = ReadProperty<bool>(reader);
                var timeSpan = ReadProperty<TimeSpan>(reader, serializer);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new AnalyzerPerformanceInfo(analyzerid, builtIn, timeSpan);
            }

            protected override void WriteValue(JsonWriter writer, AnalyzerPerformanceInfo info, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.AnalyzerId));
                writer.WriteValue(info.AnalyzerId);

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.BuiltIn));
                writer.WriteValue(info.BuiltIn);

                writer.WritePropertyName(nameof(AnalyzerPerformanceInfo.TimeSpan));
                serializer.Serialize(writer, info.TimeSpan);

                writer.WriteEndObject();
            }
        }
    }
}
