// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Host
{
    interface IPersistentStorageLocationService : IWorkspaceService
    {
        string TryGetStorageLocation(Solution solution);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService
    {
        [ImportingConstructor]
        public DefaultPersistentStorageLocationService()
        {
        }

        public string TryGetStorageLocation(Solution solution)
        {
            if (string.IsNullOrWhiteSpace(solution.FilePath))
                return null;

            // Ensure that each unique workspace kind for any given solution has a unique
            // folder to store their data in.
            var checksums = new[] { Checksum.Create(solution.FilePath), Checksum.Create(solution.Workspace.Kind) };
            var hashedName = Checksum.Create(WellKnownSynchronizationKind.Null, checksums).ToString();

            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var workingFolder = Path.Combine(appDataFolder, "Roslyn", hashedName);

            return workingFolder;
        }
    }
}
