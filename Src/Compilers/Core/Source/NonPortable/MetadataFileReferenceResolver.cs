// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves metadata references specified in source code (#r directives).
    /// </summary>
    public class MetadataFileReferenceResolver : MetadataReferenceResolver
    {
        public static readonly MetadataFileReferenceResolver Default = new MetadataFileReferenceResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        private readonly ImmutableArray<string> searchPaths;
        private readonly string baseDirectory;

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
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            this.searchPaths = searchPaths;
            this.baseDirectory = baseDirectory;
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
                throw new ArgumentNullException(argName);
            }

            if (paths.Any(path => !PathUtilities.IsAbsolute(path)))
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, argName);
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
            get { return this.searchPaths; }
        }

        /// <summary>
        /// Directory used for resolution of relative paths.
        /// A full directory path or null if not available.
        /// </summary>
        /// <remarks>
        /// This directory is only used if the base directory isn't implied by the context within which the path is being resolved.
        /// 
        /// It is used, for example, when resolving a strong name key file specified in <see cref="System.Reflection.AssemblyKeyFileAttribute"/>,
        /// or a metadata file path specified in <see cref="MetadataFileReference"/>.
        /// 
        /// Resolution of a relative path that needs the base directory fails if the base directory is null.
        /// </remarks>
        public string BaseDirectory
        {
            get { return baseDirectory; }
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
        public override string ResolveReference(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, baseDirectory, searchPaths, FileExists);
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
                throw new InvalidOperationException(string.Format(CodeAnalysisResources.PathReturnedByResolveMetadataFileMustBeAbsolute, GetType().FullName, fullPath));
            }

            return fullPath;
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }
    }
}