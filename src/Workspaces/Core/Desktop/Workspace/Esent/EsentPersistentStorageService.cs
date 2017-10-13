// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentPersistentStorageService : AbstractPersistentStorageService
    {
        private const string StorageExtension = "vbcs.cache";
        private const string PersistentStorageFileName = "storage.ide";

        public EsentPersistentStorageService(
            IOptionService optionService,
            SolutionSizeTracker solutionSizeTracker)
            : base(optionService, solutionSizeTracker)
        {
        }

        public EsentPersistentStorageService(IOptionService optionService, bool testing) 
            : base(optionService, testing)
        {
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, PersistentStorageFileName);
        }

        protected override bool TryOpenDatabase(Solution solution, string workingFolderPath, string databaseFilePath, out AbstractPersistentStorage storage)
        {
            storage = new EsentPersistentStorage(OptionService, workingFolderPath, solution.FilePath, databaseFilePath, this.Release);
            return true;
        }

        protected override bool ShouldDeleteDatabase(Exception exception)
        {
            // Access denied can happen when some other process is holding onto the DB.
            // Don't want to delete it in that case.  For all other cases, delete the db.
            return !(exception is EsentAccessDeniedException);
        }
    }
}