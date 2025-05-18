// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// Represents a DocumentId that can be used by Razor for OOP services that communicate via System.Text.Json
    /// </summary>
    internal readonly record struct JsonSerializableDocumentId(
        [property: JsonPropertyName("projectId")] Guid ProjectId,
        [property: JsonPropertyName("id")] Guid Id)
    {
        public static implicit operator JsonSerializableDocumentId(DocumentId documentId)
        {
            return new JsonSerializableDocumentId(documentId.ProjectId.Id, documentId.Id);
        }

        public static implicit operator DocumentId(JsonSerializableDocumentId serializableDocumentId)
        {
            return DocumentId.CreateFromSerialized(CodeAnalysis.ProjectId.CreateFromSerialized(serializableDocumentId.ProjectId), serializableDocumentId.Id);
        }
    }
}
