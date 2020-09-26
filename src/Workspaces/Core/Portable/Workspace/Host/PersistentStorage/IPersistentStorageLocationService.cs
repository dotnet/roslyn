// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IPersistentStorageLocationService : IWorkspaceService
    {
        bool IsSupported(Workspace workspace);
        string? TryGetStorageLocation(Solution solution);
    }

    internal interface IPersistentStorageLocationService2 : IPersistentStorageLocationService
    {
        string? TryGetStorageLocation(Workspace workspace, SolutionKey solutionKey);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService, IPersistentStorageLocationService2
    {
        /// <summary>
        /// Used to ensure that the path components we generate do not contain any characters that might be invalid in a
        /// path.  For example, Base64 encoding will use <c>/</c> which is something that we definitely do not want
        /// errantly added to a path.
        /// </summary>
        private static readonly ImmutableArray<char> s_invalidPathChars = Path.GetInvalidPathChars().Concat('/').ToImmutableArray();

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
            => TryGetStorageLocation(solution.Workspace, (SolutionKey)solution);

        public string? TryGetStorageLocation(Workspace workspace, SolutionKey solutionKey)
        {
            if (!IsSupported(workspace))
                return null;

            if (string.IsNullOrWhiteSpace(solutionKey.FilePath))
                return null;

            // Ensure that each unique workspace kind for any given solution has a unique
            // folder to store their data in.

            var cacheDirectory = GetCacheDirectory();
            var kind = StripInvalidPathChars(workspace.Kind ?? "");
            var hash = StripInvalidPathChars(Checksum.Create(solutionKey.FilePath).ToString());

            return Path.Combine(cacheDirectory, kind, hash);

            static string StripInvalidPathChars(string val)
            {
                val = new string(val.Where(c => !s_invalidPathChars.Contains(c)).ToArray());

                return string.IsNullOrWhiteSpace(val) ? "None" : val;
            }
        }
    }
}
