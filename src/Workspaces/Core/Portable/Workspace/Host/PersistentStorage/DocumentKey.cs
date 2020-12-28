// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal readonly struct DocumentKey
    {
        public readonly ProjectKey Project;

        public readonly DocumentId Id;
        public readonly string FilePath;
        public readonly string Name;

        public DocumentKey(ProjectKey project, DocumentId id, string filePath, string name)
        {
            Project = project;
            Id = id;
            FilePath = filePath;
            Name = name;
        }

        public static explicit operator DocumentKey(Document document)
            => new((ProjectKey)document.Project, document.Id, document.FilePath, document.Name);

        public SerializableDocumentKey Dehydrate()
            => new(Project.Dehydrate(), Id, FilePath, Name);
    }

    [DataContract]
    internal readonly struct SerializableDocumentKey
    {
        [DataMember(Order = 0)]
        public readonly SerializableProjectKey Project;

        [DataMember(Order = 1)]
        public readonly DocumentId Id;

        [DataMember(Order = 2)]
        public readonly string FilePath;

        [DataMember(Order = 3)]
        public readonly string Name;

        public SerializableDocumentKey(SerializableProjectKey project, DocumentId id, string filePath, string name)
        {
            Project = project;
            Id = id;
            FilePath = filePath;
            Name = name;
        }

        public DocumentKey Rehydrate()
            => new(Project.Rehydrate(), Id, FilePath, Name);
    }
}
