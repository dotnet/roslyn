// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to source files specified in source code.
    /// </summary>
    public class SourceFileResolver : SourceReferenceResolver
    {
        public static SourceFileResolver Default { get; } = new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        private readonly string _baseDirectory;
        private readonly ImmutableArray<string> _searchPaths;

        public SourceFileResolver(IEnumerable<string> searchPaths, string baseDirectory)
            : this(searchPaths.AsImmutableOrNull(), baseDirectory)
        {
        }

        public SourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            if (searchPaths.IsDefault)
            {
                throw new ArgumentNullException(nameof(searchPaths));
            }

            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, nameof(baseDirectory));
            }

            _baseDirectory = baseDirectory;
            _searchPaths = searchPaths;
        }

        public string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        public ImmutableArray<string> SearchPaths
        {
            get { return _searchPaths; }
        }

        public override string NormalizePath(string path, string baseFilePath)
        {
            return FileUtilities.NormalizeRelativePath(path, baseFilePath, _baseDirectory);
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, _baseDirectory, _searchPaths, FileExists);
            if (resolvedPath == null)
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        public override Stream OpenRead(string resolvedPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(resolvedPath, nameof(resolvedPath));
            return FileUtilities.OpenRead(resolvedPath);
        }

        protected virtual bool FileExists(string resolvedPath)
        {
            return PortableShim.File.Exists(resolvedPath);
        }

        public override bool Equals(object obj)
        {
            // Explicitly check that we're not comparing against a derived type
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (SourceFileResolver)obj;
            return string.Equals(_baseDirectory, other._baseDirectory, StringComparison.Ordinal) &&
                _searchPaths.SequenceEqual(other._searchPaths, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_baseDirectory != null ? StringComparer.Ordinal.GetHashCode(_baseDirectory) : 0,
                   Hash.CombineValues(_searchPaths, StringComparer.Ordinal));
        }
    }
}
