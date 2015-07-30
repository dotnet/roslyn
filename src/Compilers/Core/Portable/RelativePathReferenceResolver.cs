// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class RelativePathReferenceResolver : MetadataFileReferenceResolver
    {
        private readonly ImmutableArray<string> _searchPaths;
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePathReferenceResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        public RelativePathReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            ValidateSearchPaths(searchPaths, "searchPaths");

            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw ExceptionUtilities.Unreachable;
                ////throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            _searchPaths = searchPaths;
            _baseDirectory = baseDirectory;
        }

        public override ImmutableArray<string> SearchPaths
        {
            get { return _searchPaths; }
        }

        public override string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        internal override MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            return new RelativePathReferenceResolver(searchPaths, _baseDirectory);
        }

        internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
        {
            return new RelativePathReferenceResolver(_searchPaths, baseDirectory);
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, _baseDirectory, _searchPaths, FileExists);
            if (resolvedPath == null)
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        private static bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return PortableShim.File.Exists(fullPath);
        }

        public override bool Equals(object obj)
        {
            var other = obj as RelativePathReferenceResolver;
            return (other != null) &&
                string.Equals(_baseDirectory, other._baseDirectory, StringComparison.Ordinal) &&
                _searchPaths.SequenceEqual(other._searchPaths, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_baseDirectory != null ? StringComparer.Ordinal.GetHashCode(_baseDirectory) : 0,
                   Hash.CombineValues(_searchPaths, StringComparer.Ordinal));
        }
    }
}
