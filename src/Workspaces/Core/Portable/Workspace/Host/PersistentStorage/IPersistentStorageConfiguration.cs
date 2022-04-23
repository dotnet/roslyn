// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Configuration of the <see cref="IPersistentStorageService"/> intended to be used to override behavior in tests.
    /// </summary>
    internal interface IPersistentStorageConfiguration : IWorkspaceService
    {
        /// <summary>
        /// Indicates that the client expects the DB to succeed at all work and that it should not ever gracefully fall over.
        /// Should not be set in normal host environments, where it is completely reasonable for things to fail
        /// (for example, if a client asks for a key that hasn't been stored yet).
        /// </summary>
        bool ThrowOnFailure { get; }

        string? TryGetStorageLocation(SolutionKey solutionKey);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageConfiguration)), Shared]
    internal sealed class DefaultPersistentStorageConfiguration : IPersistentStorageConfiguration
    {
        /// <summary>
        /// Used to ensure that the path components we generate do not contain any characters that might be invalid in a
        /// path.  For example, Base64 encoding will use <c>/</c> which is something that we definitely do not want
        /// errantly added to a path.
        /// </summary>
        private static readonly ImmutableArray<char> s_invalidPathChars = Path.GetInvalidPathChars().Concat('/').ToImmutableArray();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultPersistentStorageConfiguration()
        {
        }

        private static string GetCacheDirectory()
        {
            // Store in the LocalApplicationData/Roslyn/hash folder (%appdatalocal%/... on Windows,
            // ~/.local/share/... on unix).  This will place the folder in a location we can trust
            // to be able to get back to consistently as long as we're working with the same
            // solution and the same workspace kind.
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            return Path.Combine(appDataFolder, "Microsoft", "VisualStudio", "Roslyn", "Cache");
        }

        public bool ThrowOnFailure => false;

        public string? TryGetStorageLocation(SolutionKey solutionKey)
        {
            if (solutionKey.WorkspaceKind is not (WorkspaceKind.RemoteWorkspace or WorkspaceKind.RemoteTemporaryWorkspace or WorkspaceKind.Host))
                return null;

            if (string.IsNullOrWhiteSpace(solutionKey.FilePath))
                return null;

            // Ensure that each unique workspace kind for any given solution has a unique
            // folder to store their data in.

            var cacheDirectory = GetCacheDirectory();
            var kind = StripInvalidPathChars(solutionKey.WorkspaceKind);
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
