// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IOptionService optionService;
        private readonly Action<AbstractPersistentStorage> disposer;

        private int refCounter;

        protected AbstractPersistentStorage(IOptionService optionService, string solutionFilePath, Action<AbstractPersistentStorage> disposer)
        {
            Contract.ThrowIfNull(disposer);

            this.SolutionFilePath = solutionFilePath;

            this.refCounter = 0;
            this.optionService = optionService;
            this.disposer = disposer;
        }

        public string SolutionFilePath { get; private set; }

        protected bool PersistenceEnabled
        {
            get { return optionService.GetOption(PersistentStorageOptions.Enabled); }
        }

        public void Dispose()
        {
            disposer(this);
        }

        /// <summary>
        /// caller should make sure this is called in a thread-safe way
        /// </summary>
        public void AddRefUnsafe()
        {
            Contract.Requires(refCounter >= 0);
            refCounter++;
        }

        /// <summary>
        /// caller should make sure this is called in a thread-safe way
        /// </summary>
        public bool ReleaseRefUnsafe()
        {
            refCounter--;

            Contract.Requires(refCounter >= 0);
            return refCounter == 0;
        }

        public virtual void Close()
        {
        }

        public abstract Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken));
    }
}
