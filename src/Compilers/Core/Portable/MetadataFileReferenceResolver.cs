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
    /// Resolves metadata references specified in source code (#r directives).
    /// </summary>
    internal class MetadataFileReferenceResolver
    {
        public static readonly MetadataFileReferenceResolver Default = new MetadataFileReferenceResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        private readonly ImmutableArray<string> _searchPaths;
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataFileReferenceResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        public MetadataFileReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataFileReferenceResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        public MetadataFileReferenceResolver(IEnumerable<string> searchPaths, string baseDirectory)
            : this(searchPaths.AsImmutableOrNull(), baseDirectory)
        {
        }

        internal static void ValidateSearchPaths(ImmutableArray<string> paths, string argName)
        {
            if (paths.IsDefault)
            {
                throw ExceptionUtilities.Unreachable;
                ////throw new ArgumentNullException(argName);
            }

            if (paths.Any(path => !PathUtilities.IsAbsolute(path)))
            {
                throw ExceptionUtilities.Unreachable;
                ////throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, argName);
            }
        }

        /// <summary>
        /// Search paths used when resolving metadata references.
        /// </summary>
        /// <remarks>
        /// All search paths are absolute.
        /// </remarks>
        public ImmutableArray<string> SearchPaths
        {
            get { return _searchPaths; }
        }

        /// <summary>
        /// Directory used for resolution of relative paths.
        /// A full directory path or null if not available.
        /// </summary>
        /// <remarks>
        /// This directory is only used if the base directory isn't implied by the context within which the path is being resolved.
        /// 
        /// It is used, for example, when resolving a strong name key file specified in <see cref="System.Reflection.AssemblyKeyFileAttribute"/>,
        /// or a metadata file path specified in <see cref="PortableExecutableReference.FilePath"/>.
        /// 
        /// Resolution of a relative path that needs the base directory fails if the base directory is null.
        /// </remarks>
        public string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        /// <summary>
        /// Resolves a metadata reference that is a path or an assembly name.
        /// </summary>
        /// <param name="reference">Reference path.</param>
        /// <param name="baseFilePath">
        /// The base file path to use to resolve relative paths against.
        /// Null to use the <see cref="BaseDirectory"/> as a base for relative paths.
        /// </param>
        /// <returns>
        /// Normalized absolute path to the referenced file or null if it can't be resolved.
        /// </returns>
        public virtual string ResolveReference(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, _baseDirectory, _searchPaths, FileExists);
            if (!FileExists(resolvedPath))
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        internal string ResolveReferenceChecked(string reference, string baseFilePath)
        {
            string fullPath = ResolveReference(reference, baseFilePath);
            if (fullPath != null && !PathUtilities.IsAbsolute(fullPath))
            {
                throw ExceptionUtilities.Unreachable;
                //// throw new InvalidOperationException(string.Format(CodeAnalysisResources.PathReturnedByResolveMetadataFileMustBeAbsolute, GetType().FullName, fullPath));
            }

            return fullPath;
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return PortableShim.File.Exists(fullPath);
        }

        public override bool Equals(object obj)
        {
            // Explicitly check that we're not comparing against a derived type
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (MetadataFileReferenceResolver)obj;
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
