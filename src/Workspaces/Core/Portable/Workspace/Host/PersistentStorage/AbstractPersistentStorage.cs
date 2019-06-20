// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract class AbstractPersistentStorage : IChecksummedPersistentStorage
    {
        public string WorkingFolderPath { get; }
        public string SolutionFilePath { get; }

        public string DatabaseFile { get; }
        public string DatabaseDirectory => Path.GetDirectoryName(DatabaseFile);

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
        public abstract Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken);
        public abstract Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken);

        public abstract Task<Stream> ReadStreamAsync(string name, Checksum checksum, CancellationToken cancellationToken);
        public abstract Task<Stream> ReadStreamAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken);
        public abstract Task<Stream> ReadStreamAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken);

        public abstract Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum, CancellationToken cancellationToken);
        public abstract Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken);
        public abstract Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken);

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
