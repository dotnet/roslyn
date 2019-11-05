// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    interface IPersistentStorageLocationService : IWorkspaceService
    {
        string? TryGetStorageLocation(Solution solution);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService
    {
        [ImportingConstructor]
        public DefaultPersistentStorageLocationService()
        {
        }

        public string? TryGetStorageLocation(Solution solution)
        {
            if (string.IsNullOrWhiteSpace(solution.FilePath))
                return null;

            // Ensure that each unique workspace kind for any given solution has a unique
            // folder to store their data in.

            // Store in the LocalApplicationData/Roslyn/hash folder (%appdatalocal%/... on Windows,
            // ~/.local/share/... on unix).  This will place the folder in a location we can trust
            // to be able to get back to consistently as long as we're working with the same
            // solution and the same workspace kind.
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var kind = StripInvalidPathChars(solution.Workspace.Kind ?? "");
            var hash = StripInvalidPathChars(Checksum.Create(solution.FilePath).ToString());

            return Path.Combine(appDataFolder, "Roslyn", "Cache", kind, hash);

            static string StripInvalidPathChars(string val)
            {
                var invalidPathChars = Path.GetInvalidPathChars();
                val = new string(val.Where(c => !invalidPathChars.Contains(c)).ToArray());

                return string.IsNullOrWhiteSpace(val) ? "None" : val;
            }
        }
    }
}
