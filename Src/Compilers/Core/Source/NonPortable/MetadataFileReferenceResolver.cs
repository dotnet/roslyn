using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Instrumentation;

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
        private readonly TouchedFileLogger touchedFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataFileReferenceResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        /// <param name="touchedFiles"></param>
        public MetadataFileReferenceResolver(
            ImmutableArray<string> searchPaths,
            string baseDirectory,
            TouchedFileLogger touchedFiles = null)
        {
            ValidateSearchPaths(searchPaths, "assemblySearchPaths");

            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            this.searchPaths = searchPaths;
            this.baseDirectory = baseDirectory;
            this.touchedFiles = touchedFiles;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataFileReferenceResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        /// <param name="touchedFiles"></param>
        public MetadataFileReferenceResolver(
            IEnumerable<string> searchPaths,
            string baseDirectory,
            TouchedFileLogger touchedFiles = null)
            : this(searchPaths.AsImmutableOrNull(), baseDirectory, touchedFiles)
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
        /// <param name="reference">
        /// Assembly name or file path. 
        /// <see cref="M:IsFilePath"/> is used to determine whether to consider this value an assembly name or a file path.
        /// </param>
        /// <param name="baseFilePath">
        /// The base file path to use to resolve relative paths against.
        /// Null to use the <see cref="BaseDirectory"/> as a base for relative paths.
        /// </param>
        /// <returns>
        /// Normalized absolute path to the referenced file or null if it can't be resolved.
        /// </returns>
        public override string ResolveReference(string reference, string baseFilePath)
        {
            string path;
            if (!IsFilePath(reference))
            {
                path = ResolveAssemblyName(reference);
                if (path == null)
                {
                    return null;
                }
            }
            else
            {
                path = reference;
            }

            return ResolveMetadataFileChecked(path, baseFilePath);
        }

        /// <summary>
        /// Resolves given assembly name.
        /// </summary>
        /// <returns>Full path to an assembly file.</returns>
        public virtual string ResolveAssemblyName(string displayName)
        {
            return null;
        }

        /// <summary>
        /// Resolves a given reference path.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <param name="baseFilePath">
        /// The base file path to use to resolve relative paths against.
        /// Null to use the <see cref="BaseDirectory"/> as a base for relative paths.
        /// </param>
        /// <returns>
        /// The resolved metadata reference path. A normalized absolute path or null.
        /// </returns>
        public virtual string ResolveMetadataFile(string path, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, baseDirectory, searchPaths, FileExists);

            if (!FileExists(resolvedPath))
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        internal string ResolveMetadataFileChecked(string path, string baseFilePath)
        {
            string fullPath = ResolveMetadataFile(path, baseFilePath);
            if (fullPath != null)
            {
                if (!PathUtilities.IsAbsolute(fullPath))
                {
                    throw new InvalidOperationException(
                        string.Format(CodeAnalysisResources.PathReturnedByResolveMetadataFileMustBeAbsolute, GetType().FullName, fullPath));
                }
                if (touchedFiles != null)
                {
                    touchedFiles.AddRead(fullPath);
                }
            }

            return fullPath;
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath == null || PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Determines whether an assembly reference is considered an assembly file path or an assembly name.
        /// used, for example, on values of /r and #r.
        /// </summary>
        internal static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            Debug.Assert(assemblyDisplayNameOrPath != null);

            string extension = PathUtilities.GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(PathUtilities.DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(PathUtilities.AltDirectorySeparatorChar) != -1;
        }
    }
}