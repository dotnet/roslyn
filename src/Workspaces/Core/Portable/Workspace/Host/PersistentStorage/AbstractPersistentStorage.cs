// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract class AbstractPersistentStorage : IPersistentStorage
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

        public abstract Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default);
        public abstract Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default);
        public abstract Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default);

        public abstract Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default);
        public abstract Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default);
        public abstract Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default);
        public abstract void Dispose();
    }
}
