// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class RelativePathResolver : IEquatable<RelativePathResolver>
    {
        public ImmutableArray<string> SearchPaths { get; }
        public string BaseDirectory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePathResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        public RelativePathResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            Debug.Assert(searchPaths.All(PathUtilities.IsAbsolute));
            Debug.Assert(baseDirectory == null || PathUtilities.GetPathKind(baseDirectory) == PathKind.Absolute);

            SearchPaths = searchPaths;
            BaseDirectory = baseDirectory;
        }

        public string ResolvePath(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, BaseDirectory, SearchPaths, FileExists);
            if (resolvedPath == null)
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return PortableShim.File.Exists(fullPath);
        }

        public RelativePathResolver WithSearchPaths(ImmutableArray<string> searchPaths) => 
            new RelativePathResolver(searchPaths, BaseDirectory);

        public RelativePathResolver WithBaseDirectory(string baseDirectory) =>
            new RelativePathResolver(SearchPaths, baseDirectory);

        public bool Equals(RelativePathResolver other) =>
            BaseDirectory == other.BaseDirectory && SearchPaths.SequenceEqual(other.SearchPaths);

        public override int GetHashCode() =>
            Hash.Combine(BaseDirectory, Hash.CombineValues(SearchPaths));

        public override bool Equals(object obj) => Equals(obj as RelativePathResolver);
    }
}
