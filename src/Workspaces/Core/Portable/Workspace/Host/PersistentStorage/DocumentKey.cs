// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => new DocumentKey((ProjectKey)document.Project, document.Id, document.FilePath, document.Name);

        public SerializableDocumentKey Dehydrate()
        {
            return new SerializableDocumentKey
            {
                Project = Project.Dehydrate(),
                Id = Id,
                FilePath = FilePath,
                Name = Name,
            };
        }
    }

    internal class SerializableDocumentKey
    {
        public SerializableProjectKey Project;
        public DocumentId Id;
        public string FilePath;
        public string Name;

        public DocumentKey Rehydrate()
            => new DocumentKey(Project.Rehydrate(), Id, FilePath, Name);
    }
}
