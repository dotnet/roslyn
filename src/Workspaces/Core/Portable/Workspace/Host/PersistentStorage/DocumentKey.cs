// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Handle that can be used with <see cref="IChecksummedPersistentStorage"/> to read data for a
    /// <see cref="Document"/> without needing to have the entire <see cref="Document"/> snapshot available.
    /// This is useful for cases where acquiring an entire snapshot might be expensive (for example, during 
    /// solution load), but querying the data is still desired.
    /// </summary>
    [DataContract]
    internal readonly struct DocumentKey(ProjectKey project, DocumentId id, string? filePath, string name) : IEqualityComparer<DocumentKey>, IEquatable<DocumentKey>
    {
        [DataMember(Order = 0)]
        public readonly ProjectKey Project = project;

        [DataMember(Order = 1)]
        public readonly DocumentId Id = id;

        [DataMember(Order = 2)]
        public readonly string? FilePath = filePath;

        [DataMember(Order = 3)]
        public readonly string Name = name;

        public static DocumentKey ToDocumentKey(Document document)
            => ToDocumentKey(ProjectKey.ToProjectKey(document.Project), document.State);

        public static DocumentKey ToDocumentKey(ProjectKey projectKey, TextDocumentState state)
            => new(projectKey, state.Id, state.FilePath, state.Name);

        public override bool Equals(object? obj)
            => obj is DocumentKey other && Equals(other);

        public bool Equals(DocumentKey other)
            => this.Id == other.Id;

        public override int GetHashCode()
            => this.Id.GetHashCode();

        public bool Equals(DocumentKey x, DocumentKey y)
            => x.Equals(y);

        public int GetHashCode(DocumentKey obj)
            => obj.GetHashCode();
    }
}
