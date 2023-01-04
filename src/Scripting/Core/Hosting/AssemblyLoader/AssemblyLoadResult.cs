// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// The result of loading an assembly reference to the interactive session.
    /// </summary>
    internal readonly struct AssemblyLoadResult
    {
        /// <summary>
        /// True if the assembly was loaded by the assembly loader, false if has been loaded before.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Full path to the physical assembly file (might be a shadow-copy of the original assembly file).
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Original assembly file path.
        /// </summary>
        public string OriginalPath { get; }

        internal static AssemblyLoadResult CreateSuccessful(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, isSuccessful: true);
        }

        internal static AssemblyLoadResult CreateAlreadyLoaded(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, isSuccessful: false);
        }

        public AssemblyLoadResult(string path, string originalPath, bool isSuccessful)
        {
            Path = path;
            OriginalPath = originalPath;
            IsSuccessful = isSuccessful;
        }
    }
}
