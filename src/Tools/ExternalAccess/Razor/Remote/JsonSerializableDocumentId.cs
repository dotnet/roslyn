// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

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
