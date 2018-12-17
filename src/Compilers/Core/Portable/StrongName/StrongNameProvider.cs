// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal enum SigningCapability
    {
        SignsStream,
        SignsPeBuilder,
    }

    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public abstract class StrongNameProvider
    {
        protected StrongNameProvider()
        {
        }

        // TODO: delete these they don't matter anymore.
        public abstract override int GetHashCode();
        public override abstract bool Equals(object other);

        internal abstract SigningCapability Capability { get; }
        internal abstract StrongNameFileSystem FileSystem { get; }

        /// <exception cref="IOException"></exception>
        internal abstract Stream CreateInputStream();

        internal abstract StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider);

        internal StrongNameKeys CommonCreateKeys(string keyFilePath, ImmutableArray<string> keyFileSearchPaths, CommonMessageProvider messageProvider)
        {
            try
            {
                string resolvedKeyFile = ResolveStrongNameKeyFile(keyFilePath, keyFileSearchPaths);
                if (resolvedKeyFile == null)
                {
                    return new StrongNameKeys(StrongNameKeys.GetKeyFileError(messageProvider, keyFilePath, CodeAnalysisResources.FileNotFound));
                }

                Debug.Assert(PathUtilities.IsAbsolute(resolvedKeyFile));
                var fileContent = ImmutableArray.Create(FileSystem.ReadAllBytes(resolvedKeyFile));
                return StrongNameKeys.CreateHelper(fileContent, keyFilePath);
            }
            catch (Exception ex)
            {
                return new StrongNameKeys(StrongNameKeys.GetKeyFileError(messageProvider, keyFilePath, ex.Message));
            }
        }

        /// <summary>
        /// Resolves assembly strong name key file path.
        /// </summary>
        /// <returns>Normalized key file path or null if not found.</returns>
        internal string ResolveStrongNameKeyFile(string path, ImmutableArray<string> keyFileSearchPaths)
        {
            // Dev11: key path is simply appended to the search paths, even if it starts with the current (parent) directory ("." or "..").
            // This is different from PathUtilities.ResolveRelativePath.

            if (PathUtilities.IsAbsolute(path))
            {
                if (FileSystem.FileExists(path))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(path);
                }

                return path;
            }

            foreach (var searchPath in keyFileSearchPaths)
            {
                string combinedPath = PathUtilities.CombineAbsoluteAndRelativePaths(searchPath, path);

                Debug.Assert(combinedPath == null || PathUtilities.IsAbsolute(combinedPath));

                if (FileSystem.FileExists(combinedPath))
                {
                    return FileUtilities.TryNormalizeAbsolutePath(combinedPath);
                }
            }

            return null;
        }


        internal virtual void SignStream(StrongNameKeys keys, Stream inputStream, Stream outputStream)
        {
            throw new NotSupportedException();
        }

        internal virtual void SignPeBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privkey)
        {
            throw new NotSupportedException();
        }
    }
}
