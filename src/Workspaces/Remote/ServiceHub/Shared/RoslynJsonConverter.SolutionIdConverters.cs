// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AggregateJsonConverter : JsonConverter
    {
        private abstract class WorkspaceIdJsonConverter : BaseJsonConverter
        {
            protected (Guid, string) ReadFromJsonObject(JsonReader reader)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var (id, debugName) = ReadIdAndName(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return (id, debugName);
            }

            protected void WriteToJsonObject(JsonWriter writer, Guid id, string debugName)
            {
                writer.WriteStartObject();
                WriteIdAndName(writer, id, debugName);
                writer.WriteEndObject();
            }

            protected (Guid, string) ReadIdAndName(JsonReader reader)
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

        private class SolutionIdJsonConverter : WorkspaceIdJsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(SolutionId) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var (id, debugName) = ReadFromJsonObject(reader);
                return SolutionId.CreateFromSerialized(id, debugName);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var solutionId = (SolutionId)value;
                WriteToJsonObject(writer, solutionId.Id, solutionId.DebugName);
            }
        }

        private class ProjectIdJsonConverter : WorkspaceIdJsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(ProjectId) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var (id, debugName) = ReadFromJsonObject(reader);
                return ProjectId.CreateFromSerialized(id, debugName);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var projectId = (ProjectId)value;
                WriteToJsonObject(writer, projectId.Id, projectId.DebugName);
            }
        }

        private class DocumentIdJsonConverter : WorkspaceIdJsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(DocumentId) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.StartObject);

                var projectId = ReadProperty<ProjectId>(serializer, reader);

                var (id, debugName) = ReadIdAndName(reader);

                Contract.ThrowIfFalse(reader.Read());
                Contract.ThrowIfFalse(reader.TokenType == JsonToken.EndObject);

                return DocumentId.CreateFromSerialized(projectId, id, debugName);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var documentId = (DocumentId)value;

                writer.WriteStartObject();

                writer.WritePropertyName("projectId");
                serializer.Serialize(writer, documentId.ProjectId);

                WriteIdAndName(writer, documentId.Id, documentId.DebugName);

                writer.WriteEndObject();
            }
        }
    }
}
