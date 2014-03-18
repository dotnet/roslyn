// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Instrumentation;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This class is used to resolve file references for the compilation.
    /// It provides APIs to resolve:
    /// (a) Metadata reference paths.
    /// (b) Assembly names.
    /// (c) Documentation files.
    /// (d) XML document files.
    /// </summary>
    public class FileResolver
    {
        private readonly ImmutableArray<string> assemblySearchPaths;
        private readonly string baseDirectory;
        private readonly TouchedFileLogger touchedFiles;

        /// <summary>
        /// Default file resolver.
        /// Does not create a new <see cref="TouchedFileLogger"/>.
        /// </summary>
        /// <remarks>
        /// This resolver doesn't resolve any relative paths.
        /// </remarks>
        public static readonly FileResolver Default = new FileResolver(
            assemblySearchPaths: ImmutableArray<string>.Empty,
            baseDirectory: null,
            touchedFiles: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileResolver"/> class.
        /// </summary>
        /// <param name="assemblySearchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        /// <param name="touchedFiles"></param>
        public FileResolver(
            ImmutableArray<string> assemblySearchPaths,
            string baseDirectory,
            TouchedFileLogger touchedFiles = null)
        {
            ValidateSearchPaths(assemblySearchPaths, "assemblySearchPaths");

            if (baseDirectory != null && PathUtilities.GetPathKind(baseDirectory) != PathKind.Absolute)
            {
                throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory");
            }

            this.assemblySearchPaths = assemblySearchPaths;
            this.baseDirectory = baseDirectory;
            this.touchedFiles = touchedFiles;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileResolver"/> class.
        /// </summary>
        /// <param name="assemblySearchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        /// <param name="touchedFiles"></param>
        public FileResolver(
            IEnumerable<string> assemblySearchPaths,
            string baseDirectory,
            TouchedFileLogger touchedFiles = null)
            : this(assemblySearchPaths.AsImmutableOrNull(), baseDirectory, touchedFiles)
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
        public ImmutableArray<string> AssemblySearchPaths
        {
            get { return this.assemblySearchPaths; }
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
        /// Resolves file name against the <see cref="FileResolver"/>'s base
        /// directory. Also logs the file as touched.
        /// </summary>
        internal string ResolveRelativePath(string path)
        {
            string resolved = FileUtilities.ResolveRelativePath(path, baseDirectory);
            if (touchedFiles != null && resolved != null)
            {
                touchedFiles.AddRead(resolved);
            }
            return resolved;
        }

        /// <summary>
        /// Resolves a metadata reference that is a path or an assembly name.
        /// </summary>
        /// <param name="assemblyDisplayNameOrPath">
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
        public string ResolveMetadataReference(string assemblyDisplayNameOrPath, string baseFilePath = null)
        {
            string path;
            if (!IsFilePath(assemblyDisplayNameOrPath))
            {
                path = ResolveAssemblyName(assemblyDisplayNameOrPath);
                if (path == null)
                {
                    return null;
                }
            }
            else
            {
                path = assemblyDisplayNameOrPath;
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
            string resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, baseDirectory, assemblySearchPaths, FileExists);

            if (!FileExists(resolvedPath))
            {
                return null;
            }

            return FileUtilities.NormalizeAbsolutePath(resolvedPath);
        }

        internal string ResolveMetadataFileChecked(string path, string baseFilePath)
        {
            string fullPath = ResolveMetadataFile(path, baseFilePath);
            if (fullPath != null)
            {
                if (!PathUtilities.IsAbsolute(fullPath))
                {
                    throw new InvalidOperationException(
                        String.Format(CodeAnalysisResources.PathReturnedByResolveMetadataFileMustBeAbsolute, GetType().FullName, fullPath));
                }
                if (touchedFiles != null)
                {
                    touchedFiles.AddRead(fullPath);
                }
            }

            return fullPath;
        }

        #region Source File Resolution // TODO: move to a separate type and make public

        internal virtual string NormalizePath(string path, string basePath)
        {
            return FileUtilities.NormalizeRelativePath(path, basePath, baseDirectory) ?? path;
        }

        #endregion

        #region XML Document resolution // TODO: move to a separate type and make public

        /// <summary>
        /// Resolves XML document file path.
        /// </summary>
        /// <param name="path">
        /// Value of the "file" attribute of an &lt;include&gt; documentation comment element.
        /// </param>
        /// <param name="baseFilePath">
        /// The base file path to use to resolve current-directory-relative paths against.
        /// Null if not available.
        /// </param>
        /// <returns>Normalized XML document file path or null if not found.</returns>
        public virtual string ResolveXmlFile(string path, string baseFilePath)
        {
            // Dev11: first look relative to the directory containing the file with the <include> element (baseFilepath)
            // and then look look in the base directory (i.e. current working directory of the compiler).

            string resolvedPath = FileUtilities.ResolveRelativePath(path, baseFilePath, baseDirectory);
            if (!FileExists(resolvedPath))
            {
                resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (!FileExists(resolvedPath))
                {
                    return null;
                }
            }

            if (touchedFiles != null)
            {
                touchedFiles.AddRead(resolvedPath);
            }

            return FileUtilities.NormalizeAbsolutePath(resolvedPath);
        }

        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not a valid absolute path.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        internal virtual Stream OpenRead(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

            try
            {
                // Use FileShare.Delete to support files that are opened with DeleteOnClose option.
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }
            catch (Exception e) if (!(e is IOException))
            {
                throw new IOException(e.Message, e);
            }
        }

        internal Stream OpenReadChecked(string fullPath)
        {
            var stream = OpenRead(fullPath);

            if (stream == null || !stream.CanRead)
            {
                throw new InvalidOperationException(CodeAnalysisResources.FileResolverShouldReturnReadableNonNullStream);
            }

            return stream;
        }

        #endregion

        // TODO (tomat): virtualized for testing, consider exposing as public API
        internal virtual bool FileExists(string fullPath)
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