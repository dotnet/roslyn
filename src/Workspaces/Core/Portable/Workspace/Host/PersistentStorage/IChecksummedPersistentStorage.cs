// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.PersistentStorage
{
    internal readonly struct SolutionKey
    {
        public readonly SolutionId Id;
        public readonly string FilePath;
        public readonly bool IsPrimaryBranch;

        public SolutionKey(SolutionId id, string filePath, bool isPrimaryBranch)
        {
            Id = id;
            FilePath = filePath;
            IsPrimaryBranch = isPrimaryBranch;
        }

        public static explicit operator SolutionKey(Solution solution)
            => new SolutionKey(solution.Id, solution.FilePath, solution.BranchId == solution.Workspace.PrimaryBranchId);

        public SerializableSolutionKey Dehydrate()
        {
            return new SerializableSolutionKey
            {
                Id = Id,
                FilePath = FilePath,
                IsPrimaryBranch = IsPrimaryBranch,
            };
        }
    }

    internal readonly struct ProjectKey
    {
        public readonly SolutionKey Solution;

        public readonly ProjectId Id;
        public readonly string FilePath;
        public readonly string Name;

        public ProjectKey(SolutionKey solution, ProjectId id, string filePath, string name)
        {
            Solution = solution;
            Id = id;
            FilePath = filePath;
            Name = name;
        }

        public static explicit operator ProjectKey(Project project)
            => new ProjectKey((SolutionKey)project.Solution, project.Id, project.FilePath, project.Name);

        public SerializableProjectKey Dehydrate()
        {
            return new SerializableProjectKey
            {
                Solution = Solution.Dehydrate(),
                Id = Id,
                FilePath = FilePath,
                Name = Name,
            };
        }
    }

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

    internal class SerializableProjectKey
    {
        public SerializableSolutionKey Solution;
        public ProjectId Id;
        public string FilePath;
        public string Name;

        public ProjectKey Rehydrate()
            => new ProjectKey(Solution.Rehydrate(), Id, FilePath, Name);
    }

    internal class SerializableSolutionKey
    {
        public SolutionId Id;
        public string FilePath;
        public bool IsPrimaryBranch;

        public SolutionKey Rehydrate()
            => new SolutionKey(Id, FilePath, IsPrimaryBranch);
    }
}

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IChecksummedPersistentStorage : IPersistentStorage
    {
        /// <summary>
        /// Reads the existing checksum we have for the solution with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the existing checksum we have for the given <paramref name="project"/> with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the existing checksum we have for the given <paramref name="document"/> with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken = default);

        Task<Checksum> ReadChecksumAsync(ProjectKey project, string name, CancellationToken cancellationToken = default);
        Task<Checksum> ReadChecksumAsync(DocumentKey document, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the solution with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(Project project, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(Document document, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        Task<Stream> ReadStreamAsync(ProjectKey project, string name, Checksum checksum = null, CancellationToken cancellationToken = default);
        Task<Stream> ReadStreamAsync(DocumentKey document, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the solution with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);
    }
}
