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
    public class SourceFileResolver : SourceReferenceResolver, IEquatable<SourceFileResolver>
    {
        public static SourceFileResolver Default { get; } = new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        private readonly string _baseDirectory;
        private readonly ImmutableArray<string> _searchPaths;
        private readonly ImmutableArray<KeyValuePair<string, string>> _pathMap;

        public SourceFileResolver(IEnumerable<string> searchPaths, string baseDirectory)
            : this(searchPaths.AsImmutableOrNull(), baseDirectory)
        {
        }

        public SourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            : this(searchPaths, baseDirectory, ImmutableArray<KeyValuePair<string, string>>.Empty)
        {
        }

        public SourceFileResolver(
            ImmutableArray<string> searchPaths,
            string baseDirectory,
            ImmutableArray<KeyValuePair<string, string>> pathMap)
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
            _pathMap = pathMap.NullToEmpty();

            // the keys in pathMap should not end with a path separator
            if (!pathMap.IsDefaultOrEmpty)
            {
                foreach (var kv in pathMap)
                {
                    var key = kv.Key;
                    if (key == null || key.Length == 0)
                    {
                        throw new ArgumentException(CodeAnalysisResources.EmptyKeyInPathMap, nameof(pathMap));
                    }

                    var value = kv.Value;
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(PathMap));
                    }

                    if (IsPathSeparator(key[key.Length - 1]))
                    {
                        throw new ArgumentException(CodeAnalysisResources.KeyInPathMapEndsWithSeparator, nameof(pathMap));
                    }
                }
            }
        }

        public string BaseDirectory => _baseDirectory;

        public ImmutableArray<string> SearchPaths => _searchPaths;

        public ImmutableArray<KeyValuePair<string, string>> PathMap => _pathMap;

        public override string NormalizePath(string path, string baseFilePath)
        {
            string normalizedPath = FileUtilities.NormalizeRelativePath(path, baseFilePath, _baseDirectory);
            return (normalizedPath == null || _pathMap.IsDefaultOrEmpty) ? normalizedPath : NormalizePathPrefix(normalizedPath, _pathMap);
        }

        private static string NormalizePathPrefix(string normalizedPath, ImmutableArray<KeyValuePair<string, string>> pathMap)
        {
            // find the first key in the path map that matches a prefix of the normalized path (followed by a path separator).
            // Note that we expect the client to use consistent capitalization; we use ordinal (case-sensitive) comparisons.
            foreach (var kv in pathMap)
            {
                var oldPrefix = kv.Key;
                if (!(oldPrefix?.Length > 0)) continue;
                if (normalizedPath.StartsWith(oldPrefix, StringComparison.Ordinal) && normalizedPath.Length > oldPrefix.Length && IsPathSeparator(normalizedPath[oldPrefix.Length]))
                {
                    var replacementPrefix = kv.Value;

                    // Replace that prefix.
                    var replacement = replacementPrefix + normalizedPath.Substring(oldPrefix.Length);

                    // Normalize the path separators if used uniformly in the replacement
                    bool hasSlash = replacementPrefix.IndexOf('/') >= 0;
                    bool hasBackslash = replacementPrefix.IndexOf('\\') >= 0;
                    return
                        (hasSlash && !hasBackslash) ? replacement.Replace('\\', '/') :
                        (hasBackslash && !hasSlash) ? replacement.Replace('/', '\\') :
                        replacement;
                }
            }

            return normalizedPath;
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

        // For purposes of command-line processing, allow both \ and / to act as path separators.
        internal static bool IsPathSeparator(char c)
        {
            return (c == '\\') || (c == '/');
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

            return Equals((SourceFileResolver)obj);
        }

        public bool Equals(SourceFileResolver other)
        {
            return
                string.Equals(_baseDirectory, other._baseDirectory, StringComparison.Ordinal) &&
                _searchPaths.SequenceEqual(other._searchPaths, StringComparer.Ordinal) &&
                _pathMap.SequenceEqual(other._pathMap);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_baseDirectory != null ? StringComparer.Ordinal.GetHashCode(_baseDirectory) : 0,
                   Hash.Combine(Hash.CombineValues(_searchPaths, StringComparer.Ordinal),
                   Hash.CombineValues(_pathMap)));
        }
    }
}
