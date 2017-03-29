// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentPersistentStorageService : AbstractPersistentStorageService, IPersistentStorageService
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

        protected override bool TryCreatePersistentStorage(
            Solution solution, string workingFolderPath,
            out AbstractPersistentStorage persistentStorage)
        {
            persistentStorage = null;
            EsentPersistentStorage database = null;

            try
            {
                database = new EsentPersistentStorage(OptionService, 
                    workingFolderPath, solution.FilePath, GetDatabaseFilePath(workingFolderPath), this.Release);
                database.Initialize(solution);

                persistentStorage = database;
                return true;
            }
            catch (EsentAccessDeniedException ex)
            {
                // esent db is already in use by someone.
                if (database != null)
                {
                    database.Close();
                }

                StorageDatabaseLogger.LogException(ex);

                return false;
            }
            catch (Exception ex)
            {
                if (database != null)
                {
                    database.Close();
                }

                StorageDatabaseLogger.LogException(ex);
            }

            if (database != null)
            {
                IOUtilities.PerformIO(() => Directory.Delete(database.DatabaseDirectory, recursive: true));
            }

            return false;
        }
    }
}