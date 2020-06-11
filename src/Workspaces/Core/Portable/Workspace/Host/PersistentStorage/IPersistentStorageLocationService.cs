﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IPersistentStorageLocationService : IWorkspaceService
    {
        bool IsSupported(Workspace workspace);
        string? TryGetStorageLocation(Solution solution);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultPersistentStorageLocationService()
        {
        }

        public virtual bool IsSupported(Workspace workspace) => false;

        protected virtual string GetCacheDirectory()
        {
            // Store in the LocalApplicationData/Roslyn/hash folder (%appdatalocal%/... on Windows,
            // ~/.local/share/... on unix).  This will place the folder in a location we can trust
            // to be able to get back to consistently as long as we're working with the same
            // solution and the same workspace kind.
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            return Path.Combine(appDataFolder, "Microsoft", "VisualStudio", "Roslyn", "Cache");
        }

        public string? TryGetStorageLocation(Solution solution)
        {
            if (!IsSupported(solution.Workspace))
                return null;

            if (string.IsNullOrWhiteSpace(solution.FilePath))
                return null;

            // Ensure that each unique workspace kind for any given solution has a unique
            // folder to store their data in.

            var cacheDirectory = GetCacheDirectory();
            var kind = StripInvalidPathChars(solution.Workspace.Kind ?? "");
            var hash = StripInvalidPathChars(Checksum.Create(solution.FilePath).ToString());

            return Path.Combine(cacheDirectory, kind, hash);

            static string StripInvalidPathChars(string val)
            {
                var invalidPathChars = Path.GetInvalidPathChars();
                val = new string(val.Where(c => !invalidPathChars.Contains(c)).ToArray());

                return string.IsNullOrWhiteSpace(val) ? "None" : val;
            }
        }
    }
}
