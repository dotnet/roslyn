﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PersistentStorage
{
    /// <summary>
    /// Handle that can be used with <see cref="IChecksummedPersistentStorage"/> to read data for a
    /// <see cref="Document"/> without needing to have the entire <see cref="Document"/> snapshot available.
    /// This is useful for cases where acquiring an entire snapshot might be expensive (for example, during 
    /// solution load), but querying the data is still desired.
    /// </summary>
    [DataContract]
    internal readonly struct DocumentKey : IEqualityComparer<DocumentKey>
    {
        [DataMember(Order = 0)]
        public readonly ProjectKey Project;

        [DataMember(Order = 1)]
        public readonly DocumentId Id;

        [DataMember(Order = 2)]
        public readonly string? FilePath;

        [DataMember(Order = 3)]
        public readonly string Name;

        public DocumentKey(ProjectKey project, DocumentId id, string? filePath, string name)
        {
            Project = project;
            Id = id;
            FilePath = filePath;
            Name = name;
        }

        public static DocumentKey ToDocumentKey(Document document)
            => ToDocumentKey(ProjectKey.ToProjectKey(document.Project), document.State);

        public static DocumentKey ToDocumentKey(ProjectKey projectKey, TextDocumentState state)
            => new(projectKey, state.Id, state.FilePath, state.Name);

        public bool Equals(DocumentKey x, DocumentKey y)
            => x.Id == y.Id;

        public int GetHashCode(DocumentKey obj)
            => obj.Id.GetHashCode();
    }
}
