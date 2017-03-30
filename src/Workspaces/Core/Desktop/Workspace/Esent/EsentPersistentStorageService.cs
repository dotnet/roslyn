// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentPersistentStorageService : AbstractPersistentStorageService, IPersistentStorageService
    {
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

        public EsentPersistentStorageService(IOptionService optionService) 
            : base(optionService)
        {
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
            => EsentPersistentStorage.GetDatabaseFile(workingFolderPath);

        protected override bool TryCreatePersistentStorage(
            string workingFolderPath, string solutionPath,
            out AbstractPersistentStorage persistentStorage)
        {
            persistentStorage = null;
            EsentPersistentStorage esent = null;

            try
            {
                esent = new EsentPersistentStorage(OptionService, workingFolderPath, solutionPath, this.Release);
                esent.Initialize();

                persistentStorage = esent;
                return true;
            }
            catch (EsentAccessDeniedException ex)
            {
                // esent db is already in use by someone.
                if (esent != null)
                {
                    esent.Close();
                }

                EsentLogger.LogException(ex);

                return false;
            }
            catch (Exception ex)
            {
                if (esent != null)
                {
                    esent.Close();
                }

                EsentLogger.LogException(ex);
            }

            try
            {
                if (esent != null)
                {
                    Directory.Delete(esent.EsentDirectory, recursive: true);
                }
            }
            catch
            {
                // somehow, we couldn't delete the directory.
            }

            return false;
        }
    }
}