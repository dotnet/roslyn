﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IOptionService _optionService;
        private readonly Action<AbstractPersistentStorage> _disposer;

        private int _refCounter;

        public string WorkingFolderPath { get; }
        public string SolutionFilePath { get; }

        public string DatabaseFile { get; }
        public string DatabaseDirectory => Path.GetDirectoryName(DatabaseFile);

        protected bool PersistenceEnabled
            => _optionService.GetOption(PersistentStorageOptions.Enabled);

        protected AbstractPersistentStorage(
            IOptionService optionService, 
            string workingFolderPath, 
            string solutionFilePath,
            string databaseFile,
            Action<AbstractPersistentStorage> disposer)
        {
            Contract.ThrowIfNull(disposer);

            this.WorkingFolderPath = workingFolderPath;
            this.SolutionFilePath = solutionFilePath;
            this.DatabaseFile = databaseFile;

            _refCounter = 0;
            _optionService = optionService;
            _disposer = disposer;

            if (!Directory.Exists(this.DatabaseDirectory))
            {
                Directory.CreateDirectory(this.DatabaseDirectory);
            }
        }

        public abstract void Initialize(Solution solution);
        public abstract void Close();

        public abstract Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default);
        public abstract Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default);
        public abstract Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default);

        public abstract Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default);
        public abstract Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default);
        public abstract Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default);

        public void Dispose()
        {
            _disposer(this);
        }

        /// <summary>
        /// caller should make sure this is called in a thread-safe way
        /// </summary>
        public void AddRefUnsafe()
        {
            Contract.Requires(_refCounter >= 0);
            Interlocked.Increment(ref _refCounter);
        }

        /// <summary>
        /// caller should make sure this is called in a thread-safe way
        /// </summary>
        public bool ReleaseRefUnsafe()
        {
            var changedValue = Interlocked.Decrement(ref _refCounter);

            Contract.Requires(changedValue >= 0);
            return changedValue == 0;
        }
    }
}
