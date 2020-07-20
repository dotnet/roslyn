// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        private abstract class WorkspaceIdJsonConverter<T> : BaseJsonConverter<T>
        {
            protected static (Guid, string)? ReadFromJsonObject(JsonReader reader)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var (id, debugName) = ReadIdAndName(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return (id, debugName);
            }

            protected static void WriteToJsonObject(JsonWriter writer, Guid id, string debugName)
            {
                writer.WriteStartObject();
                WriteIdAndName(writer, id, debugName);
                writer.WriteEndObject();
            }

            protected static (Guid, string) ReadIdAndName(JsonReader reader)
            {
                var id = new Guid(ReadProperty<string>(reader));
                var debugName = ReadProperty<string>(reader);

                return (id, debugName);
            }

            protected static void WriteIdAndName(JsonWriter writer, Guid id, string debugName)
            {
                writer.WritePropertyName(nameof(id));
                writer.WriteValue(id);

                writer.WritePropertyName(nameof(debugName));
                writer.WriteValue(debugName);
            }
        }

        private class SolutionIdJsonConverter : WorkspaceIdJsonConverter<SolutionId>
        {
            protected override SolutionId ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                (Guid id, string debugName)? tuple = ReadFromJsonObject(reader);
                return tuple == null ? null : SolutionId.CreateFromSerialized(tuple.Value.id, tuple.Value.debugName);
            }

            protected override void WriteValue(JsonWriter writer, SolutionId solutionId, JsonSerializer serializer)
                => WriteToJsonObject(writer, solutionId.Id, solutionId.DebugName);
        }

        private class ProjectIdJsonConverter : WorkspaceIdJsonConverter<ProjectId>
        {
            protected override ProjectId ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                (Guid id, string debugName)? tuple = ReadFromJsonObject(reader);
                return tuple == null ? null : ProjectId.CreateFromSerialized(tuple.Value.id, tuple.Value.debugName);
            }

            protected override void WriteValue(JsonWriter writer, ProjectId projectId, JsonSerializer serializer)
                => WriteToJsonObject(writer, projectId.Id, projectId.DebugName);
        }

        private class DocumentIdJsonConverter : WorkspaceIdJsonConverter<DocumentId>
        {
            protected override DocumentId ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var projectId = ReadProperty<ProjectId>(reader, serializer);

                var (id, debugName) = ReadIdAndName(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return DocumentId.CreateFromSerialized(projectId, id, debugName);
            }

            protected override void WriteValue(JsonWriter writer, DocumentId documentId, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("projectId");
                serializer.Serialize(writer, documentId.ProjectId);

                WriteIdAndName(writer, documentId.Id, documentId.DebugName);

                writer.WriteEndObject();
            }
        }
    }
}
