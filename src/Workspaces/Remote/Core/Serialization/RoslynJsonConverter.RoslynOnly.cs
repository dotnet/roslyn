// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
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
            Add(builder, new DiagnosticDataLocationJsonConverter());
            Add(builder, new DiagnosticDataJsonConverter());
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

        private sealed class DiagnosticDataLocationJsonConverter : BaseJsonConverter<DiagnosticDataLocation>
        {
            protected override DiagnosticDataLocation ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var documentId = ReadProperty<DocumentId>(reader, serializer);
                var sourceSpan = ReadProperty<TextSpan>(reader, serializer);
                var mappedFilePath = ReadProperty<string>(reader);
                var mappedStartLine = (int)ReadProperty<long>(reader);
                var mappedStartColumn = (int)ReadProperty<long>(reader);
                var mappedEndLine = (int)ReadProperty<long>(reader);
                var mappedEndColumn = (int)ReadProperty<long>(reader);
                var originalFilePath = ReadProperty<string>(reader);
                var originalStartLine = (int)ReadProperty<long>(reader);
                var originalStartColumn = (int)ReadProperty<long>(reader);
                var originalEndLine = (int)ReadProperty<long>(reader);
                var originalEndColumn = (int)ReadProperty<long>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new DiagnosticDataLocation(
                    documentId,
                    sourceSpan,
                    originalFilePath,
                    originalStartLine,
                    originalStartColumn,
                    originalEndLine,
                    originalEndColumn,
                    mappedFilePath,
                    mappedStartLine,
                    mappedStartColumn,
                    mappedEndLine,
                    mappedEndColumn);
            }

            protected override void WriteValue(JsonWriter writer, DiagnosticDataLocation location, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(DiagnosticDataLocation.DocumentId));
                serializer.Serialize(writer, location.DocumentId);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.SourceSpan));
                serializer.Serialize(writer, location.SourceSpan);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.MappedFilePath));
                writer.WriteValue(location.MappedFilePath);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.MappedStartLine));
                writer.WriteValue(location.MappedStartLine);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.MappedStartColumn));
                writer.WriteValue(location.MappedStartColumn);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.MappedEndLine));
                writer.WriteValue(location.MappedEndLine);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.MappedEndColumn));
                writer.WriteValue(location.MappedEndColumn);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.OriginalFilePath));
                writer.WriteValue(location.OriginalFilePath);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.OriginalStartLine));
                writer.WriteValue(location.OriginalStartLine);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.OriginalStartColumn));
                writer.WriteValue(location.OriginalStartColumn);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.OriginalEndLine));
                writer.WriteValue(location.OriginalEndLine);

                writer.WritePropertyName(nameof(DiagnosticDataLocation.OriginalEndColumn));
                writer.WriteValue(location.OriginalEndColumn);

                writer.WriteEndObject();
            }
        }

        private sealed class DiagnosticDataJsonConverter : BaseJsonConverter<DiagnosticData>
        {
            protected override DiagnosticData ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var id = ReadProperty<string>(reader);
                var category = ReadProperty<string>(reader);
                var message = ReadProperty<string>(reader);
                var enuMessageForBingSearch = ReadProperty<string>(reader);
                var severity = ReadProperty<DiagnosticSeverity>(reader, serializer);
                var defaultSeverity = ReadProperty<DiagnosticSeverity>(reader, serializer);
                var isEnabledByDefault = ReadProperty<bool>(reader);
                var warningLevel = (int)ReadProperty<long>(reader);
                var customTags = ReadProperty<ImmutableArray<string>>(reader, serializer);
                var properties = ReadProperty<ImmutableDictionary<string, string>>(reader, serializer);
                var projectId = ReadProperty<ProjectId>(reader, serializer);
                var location = ReadProperty<DiagnosticDataLocation>(reader, serializer);
                var additionalLocations = ReadProperty<ImmutableArray<DiagnosticDataLocation>>(reader, serializer);
                var language = ReadProperty<string>(reader);
                var title = ReadProperty<string>(reader);
                var description = ReadProperty<string>(reader);
                var helpLink = ReadProperty<string>(reader);
                var isSuppressed = ReadProperty<bool>(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return new DiagnosticData(
                    id,
                    category,
                    message,
                    enuMessageForBingSearch,
                    severity,
                    defaultSeverity,
                    isEnabledByDefault,
                    warningLevel,
                    customTags,
                    properties,
                    projectId,
                    location,
                    additionalLocations,
                    language,
                    title,
                    description,
                    helpLink,
                    isSuppressed);
            }

            protected override void WriteValue(JsonWriter writer, DiagnosticData data, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(DiagnosticData.Id));
                writer.WriteValue(data.Id);

                writer.WritePropertyName(nameof(DiagnosticData.Category));
                writer.WriteValue(data.Category);

                writer.WritePropertyName(nameof(DiagnosticData.Message));
                writer.WriteValue(data.Message);

                writer.WritePropertyName(nameof(DiagnosticData.ENUMessageForBingSearch));
                writer.WriteValue(data.ENUMessageForBingSearch);

                writer.WritePropertyName(nameof(DiagnosticData.Severity));
                writer.WriteValue((int)data.Severity);

                writer.WritePropertyName(nameof(DiagnosticData.DefaultSeverity));
                writer.WriteValue((int)data.DefaultSeverity);

                writer.WritePropertyName(nameof(DiagnosticData.IsEnabledByDefault));
                writer.WriteValue(data.IsEnabledByDefault);

                writer.WritePropertyName(nameof(DiagnosticData.WarningLevel));
                writer.WriteValue(data.WarningLevel);

                writer.WritePropertyName(nameof(DiagnosticData.CustomTags));
                serializer.Serialize(writer, data.CustomTags);

                writer.WritePropertyName(nameof(DiagnosticData.Properties));
                serializer.Serialize(writer, data.Properties);

                writer.WritePropertyName(nameof(DiagnosticData.ProjectId));
                serializer.Serialize(writer, data.ProjectId);

                writer.WritePropertyName(nameof(DiagnosticData.DataLocation));
                serializer.Serialize(writer, data.DataLocation);

                writer.WritePropertyName(nameof(DiagnosticData.AdditionalLocations));
                serializer.Serialize(writer, data.AdditionalLocations);

                writer.WritePropertyName(nameof(DiagnosticData.Language));
                writer.WriteValue(data.Language);

                writer.WritePropertyName(nameof(DiagnosticData.Title));
                writer.WriteValue(data.Title);

                writer.WritePropertyName(nameof(DiagnosticData.Description));
                writer.WriteValue(data.Description);

                writer.WritePropertyName(nameof(DiagnosticData.HelpLink));
                writer.WriteValue(data.HelpLink);

                writer.WritePropertyName(nameof(DiagnosticData.IsSuppressed));
                writer.WriteValue(data.IsSuppressed);

                writer.WriteEndObject();
            }
        }
    }
}
