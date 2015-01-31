// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// The result of loading an assembly reference to the interactive session.
    /// </summary>
    [Serializable]
    public struct AssemblyLoadResult
    {
        private readonly string _path;
        private readonly string _originalPath;
        private readonly bool _successful;

        internal static AssemblyLoadResult CreateSuccessful(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, successful: true);
        }

        internal static AssemblyLoadResult CreateAlreadyLoaded(string path, string originalPath)
        {
            return new AssemblyLoadResult(path, originalPath, successful: false);
        }

        private AssemblyLoadResult(string path, string originalPath, bool successful)
        {
            _path = path;
            _originalPath = originalPath;
            _successful = successful;
        }

        /// <summary>
        /// True if the assembly was loaded by the assembly loader, false if has been loaded before.
        /// </summary>
        public bool IsSuccessful
        {
            get { return _successful; }
        }

        /// <summary>
        /// Full path to the physical assembly file (might be a shadow-copy of the original assembly file).
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Original assembly file path.
        /// </summary>
        public string OriginalPath
        {
            get { return _originalPath; }
        }
    }
}
