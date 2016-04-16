// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// The result of loading an assembly reference to the interactive session.
    /// </summary>
    internal struct AssemblyLoadResult
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
