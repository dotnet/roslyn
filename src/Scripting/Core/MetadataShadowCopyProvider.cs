// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Implements shadow-copying metadata file cache.
    /// </summary>
    internal sealed class MetadataShadowCopyProvider : MetadataFileReferenceProvider, IDisposable
    {
        /// <summary>
        /// Specialize <see cref="PortableExecutableReference"/> with path being the original path of the copy.
        /// Logically this reference represents that file, the fact that we load the image from a copy is an implementation detail.
        /// </summary>
        private sealed class ShadowCopyReference : PortableExecutableReference
        {
            private readonly MetadataShadowCopyProvider _provider;

            public ShadowCopyReference(MetadataShadowCopyProvider provider, string originalPath, MetadataReferenceProperties properties)
                : base(properties, originalPath)
            {
                Debug.Assert(originalPath != null);
                Debug.Assert(provider != null);

                _provider = provider;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO (tomat): use file next to the dll (or shadow copy)
                return DocumentationProvider.Default;
            }

            protected override Metadata GetMetadataImpl()
            {
                return _provider.GetMetadata(FilePath, Properties.Kind);
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return new ShadowCopyReference(_provider, this.FilePath, properties);
            }
        }

        // normalized absolute path
        private readonly string _baseDirectory;

        // Normalized absolute path to a directory where assemblies are copied. Must contain nothing but shadow-copied assemblies.
        // Internal for testing.
        internal string ShadowCopyDirectory;

        // normalized absolute paths
        private readonly IEnumerable<string> _noShadowCopyDirectories;

        private static readonly ImmutableArray<string> s_systemNoShadowCopyDirectories = ImmutableArray.Create(
            FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
            FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
            FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
            FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory()));

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

        private object Guard { get { return _shadowCopies; } }

        /// <summary>
        /// Creates an instance of <see cref="MetadataShadowCopyProvider"/>.
        /// </summary>
        /// <param name="directory">The directory to use to store file copies.</param>
        /// <param name="noShadowCopyDirectories">Directories to exclude from shadow-copying.</param>
        /// <exception cref="ArgumentNullException"><paramref name="directory"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="directory"/> is not an absolute path.</exception>
        public MetadataShadowCopyProvider(string directory = null, IEnumerable<string> noShadowCopyDirectories = null)
        {
            if (directory != null)
            {
                RequireAbsolutePath(directory, "directory");
                try
                {
                    _baseDirectory = FileUtilities.NormalizeDirectoryPath(directory);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(e.Message, "directory");
                }
            }
            else
            {
                _baseDirectory = Path.Combine(Path.GetTempPath(), "Roslyn", "MetadataShadowCopyProvider");
            }

            var normalizedDirs = s_systemNoShadowCopyDirectories;
            if (noShadowCopyDirectories != null)
            {
                try
                {
                    normalizedDirs = normalizedDirs.AddRange(noShadowCopyDirectories.Select(FileUtilities.NormalizeDirectoryPath));
                }
                catch (Exception e)
                {
                    throw new ArgumentException(e.Message, "noShadowCopyDirectories");
                }
            }

            _noShadowCopyDirectories = normalizedDirs;

            // We want to be sure to delete the shadow-copied files when the process goes away. Frankly
            // there's nothing we can do if the process is forcefully quit or goes down in a completely
            // uncontrolled manner (like a stack overflow). When the process goes down in a controlled
            // manned, we should generally expect this event to be called.
            AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
        }

        private static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException(Microsoft.CodeAnalysis.Scripting.ScriptingResources.AbsolutePathExpected, argumentName);
            }
        }

        private void HandleProcessExit(object sender, EventArgs e)
        {
            Dispose();
            AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
        }

        /// <summary>
        /// Determine whether given path is under the shadow-copy directory managed by this shadow-copy provider.
        /// </summary>
        /// <param name="fullPath">Absolute path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        public bool IsShadowCopy(string fullPath)
        {
            RequireAbsolutePath(fullPath, "fullPath");

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

        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
        {
            RequireAbsolutePath(fullPath, "fullPath");
            return new ShadowCopyReference(this, fullPath, properties);
        }

        /// <summary>
        /// Suppresses shadow-coping of specified path.
        /// </summary>
        /// <param name="originalPath">Full path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="originalPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="originalPath"/> is not an absolute path.</exception>
        /// <remarks>
        /// Doesn't affect files that have already been shadow-copied.
        /// </remarks>
        public void SuppressShadowCopy(string originalPath)
        {
            RequireAbsolutePath(originalPath, "originalPath");

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
            RequireAbsolutePath(fullPath, "fullPath");
            string directory = Path.GetDirectoryName(fullPath);

            // do not shadow-copy shadow-copies:
            string referencesDir = ShadowCopyDirectory;
            if (referencesDir != null && directory.StartsWith(referencesDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !_noShadowCopyDirectories.Any(dir => directory.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
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
                    string assemblyDir = CreateUniqueDirectory(ShadowCopyDirectory);
                    string shadowCopyPath = Path.Combine(assemblyDir, Path.GetFileName(originalPath));

                    ShadowCopy documentationFileCopy = null;
                    string xmlOriginalPath;
                    if (TryFindXmlDocumentationFile(originalPath, out xmlOriginalPath))
                    {
                        // TODO (tomat): how do doc comments work for multi-module assembly?
                        var xmlCopyPath = Path.ChangeExtension(shadowCopyPath, ".xml");
                        var xmlStream = CopyFile(xmlOriginalPath, xmlCopyPath, fileMayNotExist: true);
                        if (xmlStream != null)
                        {
                            documentationFileCopy = new ShadowCopy(xmlStream, xmlOriginalPath, xmlCopyPath);
                        }
                    }

                    var manifestModuleCopyStream = CopyFile(originalPath, shadowCopyPath);
                    var manifestModuleCopy = new ShadowCopy(manifestModuleCopyStream, originalPath, shadowCopyPath);

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

        private bool TryFindXmlDocumentationFile(string assemblyFilePath, out string xmlDocumentationFilePath)
        {
            xmlDocumentationFilePath = null;

            // Look for the documentation xml in subdirectories based on the current 
            // culture
            // TODO: This logic is somewhat duplicated between here and 
            // Roslyn.Utilities.FilePathUtilities.TryFindXmlDocumentationFile

            string candidateFilePath = string.Empty;
            string xmlFileName = Path.ChangeExtension(Path.GetFileName(assemblyFilePath), ".xml");
            string originalDirectory = Path.GetDirectoryName(assemblyFilePath);

            var culture = CultureInfo.CurrentCulture;
            while (culture != CultureInfo.InvariantCulture)
            {
                candidateFilePath = Path.Combine(originalDirectory, culture.Name, xmlFileName);
                if (File.Exists(candidateFilePath))
                {
                    xmlDocumentationFilePath = candidateFilePath;
                    return true;
                }

                culture = culture.Parent;
            }

            // The documentation xml was not found in a subdirectory for the current culture, so 
            // check the directory containing the assembly itself
            candidateFilePath = Path.ChangeExtension(assemblyFilePath, ".xml");

            if (File.Exists(candidateFilePath))
            {
                xmlDocumentationFilePath = candidateFilePath;
                return true;
            }

            return false;
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

        private FileStream CopyFile(string originalPath, string shadowCopyPath, bool fileMayNotExist = false)
        {
            try
            {
                File.Copy(originalPath, shadowCopyPath, overwrite: true);

                StripReadOnlyAttributeFromFile(new FileInfo(shadowCopyPath));

                // tomat: Ideally we would mark the handle as "delete on close". Unfortunately any subsequent attempt to create a handle to the file is denied if we do so.
                // The only way to read the file is via the handle we open here, however we need to use APIs that take a path to the shadow copy and open it for read
                // (e.g. Assembly.Load, VS XML doc caching service, etc.).
#if DELETE_ON_CLOSE
                // deletes the file if the shadow copy cache isn't explicitly cleared
                var stream = new FileStream(shadowCopyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, bufferSize: 0x1000, options: FileOptions.DeleteOnClose);
                
                // this prevents us to reopen the file, so we need to use the above stream later on
                PathUtilities.PrepareDeleteOnCloseStreamForDisposal(stream);
#else
                return new FileStream(shadowCopyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
            }
            catch (FileNotFoundException)
            {
                if (!fileMayNotExist)
                {
                    throw;
                }
            }

            return null;
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
