
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract class AbstractPersistentStorage : IChecksummedPersistentStorage
    {
        public string WorkingFolderPath { get; }
        public string SolutionFilePath { get; }

        public string DatabaseFile { get; }
        public string DatabaseDirectory => Path.GetDirectoryName(DatabaseFile) ?? throw ExceptionUtilities.UnexpectedValue(DatabaseFile);

        protected AbstractPersistentStorage(
            string workingFolderPath,
            string solutionFilePath,
            string databaseFile)
        {
            this.WorkingFolderPath = workingFolderPath;
            this.SolutionFilePath = solutionFilePath;
            this.DatabaseFile = databaseFile;

            if (!Directory.Exists(this.DatabaseDirectory))
            {
                Directory.CreateDirectory(this.DatabaseDirectory);
            }
        }

        public abstract void Dispose();

        public abstract Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken);

        protected abstract Task<Checksum> ReadChecksumAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, CancellationToken cancellationToken);
        protected abstract Task<Checksum> ReadChecksumAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, CancellationToken cancellationToken);

        public Task<Checksum> ReadChecksumAsync(ProjectKey projectKey, string name, CancellationToken cancellationToken)
            => ReadChecksumAsync(projectKey, bulkLoadSnapshot: null, name, cancellationToken);

        public Task<Checksum> ReadChecksumAsync(DocumentKey documentKey, string name, CancellationToken cancellationToken)
            => ReadChecksumAsync(documentKey, bulkLoadSnapshot: null, name, cancellationToken);

        public Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken)
            => ReadChecksumAsync((ProjectKey)project, project, name, cancellationToken);

        public Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken)
            => ReadChecksumAsync((DocumentKey)document, document, name, cancellationToken);

        public abstract Task<Stream> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken);

        protected abstract Task<Stream> ReadStreamAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken);
        protected abstract Task<Stream> ReadStreamAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken);

        public Task<Stream> ReadStreamAsync(ProjectKey projectKey, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync(projectKey, bulkLoadSnapshot: null, name, checksum, cancellationToken);

        public Task<Stream> ReadStreamAsync(DocumentKey documentKey, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync(documentKey, bulkLoadSnapshot: null, name, checksum, cancellationToken);

        public Task<Stream> ReadStreamAsync(Project project, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync((ProjectKey)project, project, name, checksum, cancellationToken);

        public Task<Stream> ReadStreamAsync(Document document, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync((DocumentKey)document, document, name, checksum, cancellationToken);

        public abstract Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);
        public abstract Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);
        public abstract Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);

        public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
            => ReadStreamAsync(name, checksum: null, cancellationToken);

        public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
            => ReadStreamAsync(project, name, checksum: null, cancellationToken);

        public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
            => ReadStreamAsync(document, name, checksum: null, cancellationToken);

        public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamAsync(name, stream, checksum: null, cancellationToken);

        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamAsync(project, name, stream, checksum: null, cancellationToken);

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamAsync(document, name, stream, checksum: null, cancellationToken);
    }
}
