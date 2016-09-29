// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Implements shadow-copying metadata file cache.
    /// </summary>
    public sealed class MetadataShadowCopyProvider : IDisposable
    {
        private readonly CultureInfo _documentationCommentsCulture;

        // normalized absolute path
        private readonly string _baseDirectory;

        // Normalized absolute path to a directory where assemblies are copied. Must contain nothing but shadow-copied assemblies.
        // Internal for testing.
        internal string ShadowCopyDirectory;

        // normalized absolute paths
        private readonly ImmutableArray<string> _noShadowCopyDirectories;

        private struct CacheEntry<TPublic>
        {
            public readonly TPublic Public;
            public readonly Metadata Private;

            public CacheEntry(TPublic @public, Metadata @private)
            {
                Debug.Assert(@public != null);
                Debug.Assert(@private != null);

                Public = @public;
                Private = @private;
            }
        }

        // Cache for files that are shadow-copied:
        // (original path, last write timestamp) -> (public shadow copy, private metadata instance that owns the PE image)
        private readonly Dictionary<FileKey, CacheEntry<MetadataShadowCopy>> _shadowCopies = new Dictionary<FileKey, CacheEntry<MetadataShadowCopy>>();

        // Cache for files that are not shadow-copied:
        // (path, last write timestamp) -> (public metadata, private metadata instance that owns the PE image)
        private readonly Dictionary<FileKey, CacheEntry<Metadata>> _noShadowCopyCache = new Dictionary<FileKey, CacheEntry<Metadata>>();

        // files that should not be copied:
        private HashSet<string> _lazySuppressedFiles;

        private object Guard => _shadowCopies;

        /// <summary>
        /// Creates an instance of <see cref="MetadataShadowCopyProvider"/>.
        /// </summary>
        /// <param name="directory">The directory to use to store file copies.</param>
        /// <param name="noShadowCopyDirectories">Directories to exclude from shadow-copying.</param>
        /// <param name="documentationCommentsCulture">Culture of documentation comments to copy. If not specified no doc comment files are going to be copied.</param>
        /// <exception cref="ArgumentNullException"><paramref name="directory"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="directory"/> is not an absolute path.</exception>
        public MetadataShadowCopyProvider(string directory = null, IEnumerable<string> noShadowCopyDirectories = null, CultureInfo documentationCommentsCulture = null)
        {
            if (directory != null)
            {
                RequireAbsolutePath(directory, nameof(directory));
                try
                {
                    _baseDirectory = FileUtilities.NormalizeDirectoryPath(directory);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(e.Message, nameof(directory));
                }
            }
            else
            {
                _baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            }

            if (noShadowCopyDirectories != null)
            {
                try
                {
                    _noShadowCopyDirectories = ImmutableArray.CreateRange(noShadowCopyDirectories.Select(FileUtilities.NormalizeDirectoryPath));
                }
                catch (Exception e)
                {
                    throw new ArgumentException(e.Message, nameof(noShadowCopyDirectories));
                }
            }
            else
            {
                _noShadowCopyDirectories = ImmutableArray<string>.Empty;
            }

            _documentationCommentsCulture = documentationCommentsCulture;
        }

        private static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException(ScriptingResources.AbsolutePathExpected, argumentName);
            }
        }

        /// <summary>
        /// Determine whether given path is under the shadow-copy directory managed by this shadow-copy provider.
        /// </summary>
        /// <param name="fullPath">Absolute path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        public bool IsShadowCopy(string fullPath)
        {
            RequireAbsolutePath(fullPath, nameof(fullPath));

            string directory = ShadowCopyDirectory;
            if (directory == null)
            {
                return false;
            }

            string normalizedPath;
            try
            {
                normalizedPath = FileUtilities.NormalizeDirectoryPath(fullPath);
            }
            catch
            {
                return false;
            }

            return normalizedPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase);
        }

        ~MetadataShadowCopyProvider()
        {
            DisposeShadowCopies();
            DeleteShadowCopyDirectory();
        }

        /// <summary>
        /// Clears shadow-copy cache, disposes all allocated metadata, and attempts to delete copied files.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            lock (Guard)
            {
                DisposeShadowCopies();
                _shadowCopies.Clear();
            }

            DeleteShadowCopyDirectory();
        }

        private void DisposeShadowCopies()
        {
            foreach (var entry in _shadowCopies.Values)
            {
                // metadata file handles have been disposed already, but the xml doc file handle hasn't:
                entry.Public.DisposeFileHandles();

                // dispose metadata images:
                entry.Private.Dispose();
            }
        }

        private void DeleteShadowCopyDirectory()
        {
            var directory = ShadowCopyDirectory;
            if (Directory.Exists(directory))
            {
                try
                {
                    // First, strip the read-only bit off of any files.
                    var directoryInfo = new DirectoryInfo(directory);
                    foreach (var fileInfo in directoryInfo.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories))
                    {
                        StripReadOnlyAttributeFromFile(fileInfo);
                    }

                    // Second, delete everything.
                    Directory.Delete(directory, recursive: true);
                }
                catch
                {
                }
            }
        }

        private static void StripReadOnlyAttributeFromFile(FileInfo fileInfo)
        {
            try
            {
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }
            catch
            {
                // There are many reasons this could fail. Just ignore it and move on.
            }
        }

        /// <summary>
        /// Gets or creates metadata for specified file path.
        /// </summary>
        /// <param name="fullPath">Full path to an assembly manifest module file or a standalone module file.</param>
        /// <param name="kind">Metadata kind (assembly or module).</param>
        /// <returns>Metadata for the specified file.</returns>
        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        public Metadata GetMetadata(string fullPath, MetadataImageKind kind)
        {
            if (NeedsShadowCopy(fullPath))
            {
                return GetMetadataShadowCopyNoCheck(fullPath, kind).Metadata;
            }

            FileKey key = FileKey.Create(fullPath);

            lock (Guard)
            {
                CacheEntry<Metadata> existing;
                if (_noShadowCopyCache.TryGetValue(key, out existing))
                {
                    return existing.Public;
                }
            }

            Metadata newMetadata;
            if (kind == MetadataImageKind.Assembly)
            {
                newMetadata = AssemblyMetadata.CreateFromFile(fullPath);
            }
            else
            {
                newMetadata = ModuleMetadata.CreateFromFile(fullPath);
            }

            // the files are locked (memory mapped) now
            key = FileKey.Create(fullPath);

            lock (Guard)
            {
                CacheEntry<Metadata> existing;
                if (_noShadowCopyCache.TryGetValue(key, out existing))
                {
                    newMetadata.Dispose();
                    return existing.Public;
                }

                Metadata publicMetadata = newMetadata.Copy();
                _noShadowCopyCache.Add(key, new CacheEntry<Metadata>(publicMetadata, newMetadata));
                return publicMetadata;
            }
        }

        /// <summary>
        /// Gets or creates a copy of specified assembly or standalone module.
        /// </summary>
        /// <param name="fullPath">Full path to an assembly manifest module file or a standalone module file.</param>
        /// <param name="kind">Metadata kind (assembly or module).</param>
        /// <returns>
        /// Copy of the specified file, or null if the file doesn't need a copy (<see cref="NeedsShadowCopy"/>). 
        /// Returns the same object if called multiple times with the same path.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        public MetadataShadowCopy GetMetadataShadowCopy(string fullPath, MetadataImageKind kind)
        {
            return NeedsShadowCopy(fullPath) ? GetMetadataShadowCopyNoCheck(fullPath, kind) : null;
        }

        private MetadataShadowCopy GetMetadataShadowCopyNoCheck(string fullPath, MetadataImageKind kind)
        {
            if (kind < MetadataImageKind.Assembly || kind > MetadataImageKind.Module)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            FileKey key = FileKey.Create(fullPath);

            lock (Guard)
            {
                CacheEntry<MetadataShadowCopy> existing;
                if (CopyExistsOrIsSuppressed(key, out existing))
                {
                    return existing.Public;
                }
            }

            CacheEntry<MetadataShadowCopy> newCopy = CreateMetadataShadowCopy(fullPath, kind);

            // last-write timestamp is copied from the original file at the time the snapshot was made:
            bool fault = true;
            try
            {
                key = new FileKey(fullPath, FileUtilities.GetFileTimeStamp(newCopy.Public.PrimaryModule.FullPath));
                fault = false;
            }
            finally
            {
                if (fault)
                {
                    newCopy.Private.Dispose();
                }
            }

            lock (Guard)
            {
                CacheEntry<MetadataShadowCopy> existing;
                if (CopyExistsOrIsSuppressed(key, out existing))
                {
                    newCopy.Private.Dispose();
                    return existing.Public;
                }

                _shadowCopies.Add(key, newCopy);
            }

            return newCopy.Public;
        }

        private bool CopyExistsOrIsSuppressed(FileKey key, out CacheEntry<MetadataShadowCopy> existing)
        {
            if (_lazySuppressedFiles != null && _lazySuppressedFiles.Contains(key.FullPath))
            {
                existing = default(CacheEntry<MetadataShadowCopy>);
                return true;
            }

            return _shadowCopies.TryGetValue(key, out existing);
        }

        /// <summary>
        /// Suppresses shadow-copying of specified path.
        /// </summary>
        /// <param name="originalPath">Full path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="originalPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="originalPath"/> is not an absolute path.</exception>
        /// <remarks>
        /// Doesn't affect files that have already been shadow-copied.
        /// </remarks>
        public void SuppressShadowCopy(string originalPath)
        {
            RequireAbsolutePath(originalPath, nameof(originalPath));

            lock (Guard)
            {
                if (_lazySuppressedFiles == null)
                {
                    _lazySuppressedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                _lazySuppressedFiles.Add(originalPath);
            }
        }

        /// <summary>
        /// Determines whether given file is a candidate for shadow-copy.
        /// </summary>
        /// <param name="fullPath">An absolute path.</param>
        /// <returns>True if the shadow-copy policy applies to the specified path.</returns>
        /// <exception cref="NullReferenceException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not absolute.</exception>
        public bool NeedsShadowCopy(string fullPath)
        {
            RequireAbsolutePath(fullPath, nameof(fullPath));
            string directory = Path.GetDirectoryName(fullPath);

            // do not shadow-copy shadow-copies:
            string referencesDir = ShadowCopyDirectory;
            if (referencesDir != null && directory.StartsWith(referencesDir, StringComparison.Ordinal))
            {
                return false;
            }

            return !_noShadowCopyDirectories.Any(dir => directory.StartsWith(dir, StringComparison.Ordinal));
        }

        private CacheEntry<MetadataShadowCopy> CreateMetadataShadowCopy(string originalPath, MetadataImageKind kind)
        {
            int attempts = 10;
            while (true)
            {
                try
                {
                    if (ShadowCopyDirectory == null)
                    {
                        ShadowCopyDirectory = CreateUniqueDirectory(_baseDirectory);
                    }

                    // Create directory for the assembly.
                    // If the assembly has any modules they have to be copied to the same directory 
                    // and have the same names as specified in metadata.
                    string assemblyCopyDir = CreateUniqueDirectory(ShadowCopyDirectory);
                    string shadowCopyPath = Path.Combine(assemblyCopyDir, Path.GetFileName(originalPath));

                    FileShadowCopy documentationFileCopy = TryCopyDocumentationFile(originalPath, assemblyCopyDir, _documentationCommentsCulture);

                    var manifestModuleCopyStream = CopyFile(originalPath, shadowCopyPath);
                    var manifestModuleCopy = new FileShadowCopy(manifestModuleCopyStream, originalPath, shadowCopyPath);

                    Metadata privateMetadata;
                    if (kind == MetadataImageKind.Assembly)
                    {
                        privateMetadata = CreateAssemblyMetadata(manifestModuleCopyStream, originalPath, shadowCopyPath);
                    }
                    else
                    {
                        privateMetadata = CreateModuleMetadata(manifestModuleCopyStream);
                    }

                    var publicMetadata = privateMetadata.Copy();
                    return new CacheEntry<MetadataShadowCopy>(new MetadataShadowCopy(manifestModuleCopy, documentationFileCopy, publicMetadata), privateMetadata);
                }
                catch (DirectoryNotFoundException)
                {
                    // the shadow copy directory has been deleted - try to copy all files again
                    if (!Directory.Exists(ShadowCopyDirectory))
                    {
                        ShadowCopyDirectory = null;
                        if (attempts-- > 0)
                        {
                            continue;
                        }
                    }

                    throw;
                }
            }
        }

        private AssemblyMetadata CreateAssemblyMetadata(FileStream manifestModuleCopyStream, string originalPath, string shadowCopyPath)
        {
            // We don't need to use the global metadata cache here since the shadow copy 
            // won't change and is private to us - only users of the same shadow copy provider see it.

            ImmutableArray<ModuleMetadata>.Builder moduleBuilder = null;

            bool fault = true;
            ModuleMetadata manifestModule = null;
            try
            {
                manifestModule = CreateModuleMetadata(manifestModuleCopyStream);

                string originalDirectory = null, shadowCopyDirectory = null;
                foreach (string moduleName in manifestModule.GetModuleNames())
                {
                    if (moduleBuilder == null)
                    {
                        moduleBuilder = ImmutableArray.CreateBuilder<ModuleMetadata>();
                        moduleBuilder.Add(manifestModule);
                        originalDirectory = Path.GetDirectoryName(originalPath);
                        shadowCopyDirectory = Path.GetDirectoryName(shadowCopyPath);
                    }

                    FileStream moduleCopyStream = CopyFile(
                        originalPath: Path.Combine(originalDirectory, moduleName),
                        shadowCopyPath: Path.Combine(shadowCopyDirectory, moduleName));

                    moduleBuilder.Add(CreateModuleMetadata(moduleCopyStream));
                }

                var modules = (moduleBuilder != null) ? moduleBuilder.ToImmutable() : ImmutableArray.Create(manifestModule);

                fault = false;
                return AssemblyMetadata.Create(modules);
            }
            finally
            {
                if (fault)
                {
                    if (manifestModule != null)
                    {
                        manifestModule.Dispose();
                    }

                    if (moduleBuilder != null)
                    {
                        for (int i = 1; i < moduleBuilder.Count; i++)
                        {
                            moduleBuilder[i].Dispose();
                        }
                    }
                }
            }
        }

        private static ModuleMetadata CreateModuleMetadata(FileStream stream)
        {
            // The Stream is held by the ModuleMetadata to read metadata on demand.
            // We hand off the responsibility for closing the stream to the metadata object.
            return ModuleMetadata.CreateFromStream(stream, leaveOpen: false);
        }

        private string CreateUniqueDirectory(string basePath)
        {
            int attempts = 10;
            while (true)
            {
                string dir = Path.Combine(basePath, Guid.NewGuid().ToString());
                if (File.Exists(dir) || Directory.Exists(dir))
                {
                    // try a different name (guid):
                    continue;
                }

                try
                {
                    Directory.CreateDirectory(dir);
                    return dir;
                }
                catch (IOException)
                {
                    // Some other process might have created a file of the same name after we checked for its existence.
                    if (File.Exists(dir))
                    {
                        continue;
                    }

                    // This file might also have been deleted by now. So try again for a while and then give up.
                    if (--attempts == 0)
                    {
                        throw;
                    }
                }
            }
        }

        private static FileShadowCopy TryCopyDocumentationFile(string originalAssemblyPath, string assemblyCopyDirectory, CultureInfo docCultureOpt)
        {
            // Note: Doc comments are not supported for netmodules.

            string assemblyDirectory = Path.GetDirectoryName(originalAssemblyPath);
            string assemblyFileName = Path.GetFileName(originalAssemblyPath);

            string xmlSubdirectory;
            string xmlFileName;
            if (docCultureOpt == null ||
                !TryFindCollocatedDocumentationFile(assemblyDirectory, assemblyFileName, docCultureOpt, out xmlSubdirectory, out xmlFileName))
            {
                return null;
            }

            if (!xmlSubdirectory.IsEmpty())
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(assemblyCopyDirectory, xmlSubdirectory));
                }
                catch
                {
                    return null;
                }
            }

            string xmlCopyPath = Path.Combine(assemblyCopyDirectory, xmlSubdirectory, xmlFileName);
            string xmlOriginalPath = Path.Combine(assemblyDirectory, xmlSubdirectory, xmlFileName);

            var xmlStream = CopyFile(xmlOriginalPath, xmlCopyPath, fileMayNotExist: true);

            return (xmlStream != null) ? new FileShadowCopy(xmlStream, xmlOriginalPath, xmlCopyPath) : null;
        }

        private static bool TryFindCollocatedDocumentationFile(
            string assemblyDirectory,
            string assemblyFileName,
            CultureInfo culture,
            out string docSubdirectory,
            out string docFileName)
        {
            Debug.Assert(assemblyDirectory != null);
            Debug.Assert(assemblyFileName != null);
            Debug.Assert(culture != null);

            // 1. Look in subdirectories based on the current culture
            docFileName = Path.ChangeExtension(assemblyFileName, ".xml");

            while (culture != CultureInfo.InvariantCulture)
            {
                docSubdirectory = culture.Name;
                if (File.Exists(Path.Combine(assemblyDirectory, docSubdirectory, docFileName)))
                {
                    return true;
                }

                culture = culture.Parent;
            }

            // 2. Look in the same directory as the assembly itself
            docSubdirectory = string.Empty;
            if (File.Exists(Path.Combine(assemblyDirectory, docFileName)))
            {
                return true;
            }

            docFileName = null;
            return false;
        }

        private static FileStream CopyFile(string originalPath, string shadowCopyPath, bool fileMayNotExist = false)
        {
            try
            {
                File.Copy(originalPath, shadowCopyPath, overwrite: true);
                StripReadOnlyAttributeFromFile(new FileInfo(shadowCopyPath));
                return new FileStream(shadowCopyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception e) when (fileMayNotExist && (e is FileNotFoundException || e is DirectoryNotFoundException))
            {
                return null;
            }
        }

        #region Test hooks

        // for testing only
        internal int CacheSize
        {
            get { return _shadowCopies.Count; }
        }

        #endregion
    }
}
