using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Implements shadow-copying metadata file cache.
    /// </summary>
    public sealed class MetadataShadowCopyProvider : MetadataReferenceProvider
    {
        /// <summary>
        /// Specialize <see cref="PortableExecutableReference"/> with path being the original path of the copy.
        /// Logically this reference represents that file, the fact that we load the image from a copy is an implementation detail.
        /// </summary>
        private sealed class ShadowCopyReference : PortableExecutableReference
        {
            private readonly MetadataShadowCopyProvider provider;

            public ShadowCopyReference(MetadataShadowCopyProvider provider, string originalPath, MetadataReferenceProperties properties)
                : base(properties, originalPath)
            {
                Debug.Assert(originalPath != null);
                Debug.Assert(provider != null);

                this.provider = provider;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO (tomat): use file next to the dll (or shadow copy)
                return DocumentationProvider.Default;
            }

            protected override Metadata CreateMetadata()
            {
                return provider.GetMetadata(FullPath, Properties.Kind);
            }
        }

        private readonly string baseDirectory;

        // Directory where assemblies are copied. Must contain nothing but shadow-copied assemblies.
        // Internal for testing.
        internal string ShadowCopyDirectory;

        private readonly IEnumerable<string> noShadowCopyDirectories;

        private static readonly IEnumerable<string> frameworkNoShadowCopyDirectories =
            GlobalAssemblyCache.RootLocations.
            Concat(RuntimeEnvironment.GetRuntimeDirectory()).
//            Concat(Path.GetDirectoryName(typeof(Microsoft.CSharp.RuntimeHelpers.SessionHelpers).Assembly.Location)).
            Select(path => NormalizePath(path));

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
        private readonly Dictionary<FileKey, CacheEntry<MetadataShadowCopy>> shadowCopies = new Dictionary<FileKey, CacheEntry<MetadataShadowCopy>>();

        // Cache for files that are not shadow-copied:
        // (path, last write timestamp) -> (public metadata, private metadata instance that owns the PE image)
        private readonly Dictionary<FileKey, CacheEntry<Metadata>> noShadowCopyCache = new Dictionary<FileKey, CacheEntry<Metadata>>();

        // files that should not be copied:
        private HashSet<string> lazySuppressedFiles;

        private object Guard { get { return shadowCopies; } }

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
                CompilerFileUtilities.RequireAbsolutePath(directory, "directory");
                this.baseDirectory = directory;
            }
            else
            {
                this.baseDirectory = Path.Combine(Path.GetTempPath(), "Roslyn", "MetadataShadowCopyProvider");
            }

            this.noShadowCopyDirectories =
                (noShadowCopyDirectories != null ? noShadowCopyDirectories.Select(dir => NormalizePath(dir)).ToArray() : SpecializedCollections.EmptyArray<string>())
                .Concat(frameworkNoShadowCopyDirectories);

            // We want to be sure to delete the shadow-copied files when the process goes away. Frankly
            // there's nothing we can do if the process is forcefully quit or goes down in a completely
            // uncontrolled manner (like a stack overflow). When the process goes down in a controlled
            // manned, we should generally expect this event to be called.
            AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
        }

        ~MetadataShadowCopyProvider()
        {
            DisposeShadowCopies();

            DeleteShadowCopyDirectory();
        }

        private void HandleProcessExit(object sender, EventArgs e)
        {
            ClearCache();

            AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
        }

        // Normalizes given path so that we can use it for sub-directory check.
        private static string NormalizePath(string path)
        {
            return FileUtilities.NormalizeAbsolutePath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Determine whether given path is under the shadow-copy directory managed by this shadow-copy provider.
        /// </summary>
        /// <param name="fullPath">Absolute path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        public bool IsShadowCopy(string fullPath)
        {
            CompilerFileUtilities.RequireAbsolutePath(fullPath, "fullPath");

            string directory = ShadowCopyDirectory;
            return directory != null && NormalizePath(fullPath).StartsWith(directory, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clears shadow-copy cache, disposes all allocated metadata, and attempts to delete copied files.
        /// </summary>
        public override void ClearCache()
        {
            lock (Guard)
            {
                DisposeShadowCopies();

                shadowCopies.Clear();
            }

            DeleteShadowCopyDirectory();
        }

        private void DisposeShadowCopies()
        {
            foreach (var entry in shadowCopies.Values)
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
        public Metadata GetMetadata(string fullPath, MetadataImageKind kind)
        {
            if (NeedsShadowCopy(fullPath))
            {
                return GetMetadataShadowCopyNoCheck(fullPath, kind).Metadata;
            }

            FileKey key = new FileKey(fullPath);

            lock (Guard)
            {
                CacheEntry<Metadata> existing;
                if (noShadowCopyCache.TryGetValue(key, out existing))
                {
                    return existing.Public;
                }
            }

            Metadata newMetadata;
            if (kind == MetadataImageKind.Assembly)
            {
                newMetadata = new AssemblyMetadata(fullPath);
            }
            else
            {
                newMetadata = new ModuleMetadata(fullPath);
            }

            // the files are locked (memory mapped) now
            key = new FileKey(fullPath);

            lock (Guard)
            {
                CacheEntry<Metadata> existing;
                if (noShadowCopyCache.TryGetValue(key, out existing))
                {
                    newMetadata.Dispose();
                    return existing.Public;
                }

                Metadata publicMetadata = newMetadata.Copy();
                noShadowCopyCache.Add(key, new CacheEntry<Metadata>(publicMetadata, newMetadata));
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
        public MetadataShadowCopy GetMetadataShadowCopy(string fullPath, MetadataImageKind kind)
        {
            return NeedsShadowCopy(fullPath) ? GetMetadataShadowCopyNoCheck(fullPath, kind) : null;
        }

        private MetadataShadowCopy GetMetadataShadowCopyNoCheck(string fullPath, MetadataImageKind kind)
        {
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            FileKey key = new FileKey(fullPath);

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
            key = new FileKey(fullPath, FileKey.GetTimeStamp(newCopy.Public.PrimaryModule.FullPath));

            lock (Guard)
            {
                CacheEntry<MetadataShadowCopy> existing;
                if (CopyExistsOrIsSuppressed(key, out existing))
                {
                    newCopy.Private.Dispose();
                    return existing.Public;
                }

                shadowCopies.Add(key, newCopy);
            }

            return newCopy.Public;
        }

        private bool CopyExistsOrIsSuppressed(FileKey key, out CacheEntry<MetadataShadowCopy> existing)
        {
            if (lazySuppressedFiles != null && lazySuppressedFiles.Contains(key.FullPath))
            {
                existing = default(CacheEntry<MetadataShadowCopy>);
                return true;
            }

            return shadowCopies.TryGetValue(key, out existing);
        }

        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
        {
            CompilerFileUtilities.RequireAbsolutePath(fullPath, "fullPath");

            // return a new reference - whenever we are asked for a reference the consumer wants a new snapshot
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
            CompilerFileUtilities.RequireAbsolutePath(originalPath, "originalPath");

            lock (Guard)
            {
                if (lazySuppressedFiles == null)
                {
                    lazySuppressedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                lazySuppressedFiles.Add(originalPath);
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
            CompilerFileUtilities.RequireAbsolutePath(fullPath, "fullPath");
            string directory = Path.GetDirectoryName(fullPath);

            // do not shadow-copy shadow-copies:
            string referencesDir = ShadowCopyDirectory;
            if (referencesDir != null && directory.StartsWith(referencesDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !noShadowCopyDirectories.Any(dir => directory.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
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
                        ShadowCopyDirectory = CreateUniqueDirectory(baseDirectory);
                    }

                    // Create directory for the assembly.
                    // If the assembly has any modules they have to be copied to the same directory 
                    // and have the same names as specified in metadata.
                    string assemblyDir = CreateUniqueDirectory(ShadowCopyDirectory);
                    string shadowCopyPath = Path.Combine(assemblyDir, Path.GetFileName(originalPath));
                    var manifestModuleCopy = CopyFile(originalPath, shadowCopyPath);
                    Debug.Assert(manifestModuleCopy != null);

                    ShadowCopy documentationFileCopy = null;
                    string xmlDocumentationFilePath;
                    if (TryFindXmlDocumentationFile(originalPath, out xmlDocumentationFilePath))
                    {
                        // TODO (tomat): how do doc comments work for multi-module assembly?
                        var xmlShadowCopyPath = Path.ChangeExtension(shadowCopyPath, ".xml");
                        documentationFileCopy = CopyFile(xmlDocumentationFilePath, xmlShadowCopyPath, fileMayNotExist: true);
                    }

                    Metadata privateMetadata;
                    if (kind == MetadataImageKind.Assembly)
                    {
                        privateMetadata = CreateAssemblyMetadata(manifestModuleCopy);
                    }
                    else
                    {
                        privateMetadata = CreateModuleMetadata(manifestModuleCopy);
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

        private AssemblyMetadata CreateAssemblyMetadata(ShadowCopy manifestModuleCopy)
        {
            // We don't need to use the global metadata cache here since the shadow copy 
            // won't change and is private to us - only users of the same shadow copy provider see it.

            ArrayBuilder<ModuleMetadata> moduleBuilder = null;

            bool fault = true;
            ModuleMetadata manifestModule = null;
            try
            {
                manifestModule = CreateModuleMetadata(manifestModuleCopy);

                string originalDirectory = null, shadowCopyDirectory = null;
                foreach (string moduleName in manifestModule.GetModuleNames())
                {
                    if (moduleBuilder == null)
                    {
                        moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
                        moduleBuilder.Add(manifestModule);
                        originalDirectory = Path.GetDirectoryName(manifestModuleCopy.OriginalPath);
                        shadowCopyDirectory = Path.GetDirectoryName(manifestModuleCopy.FullPath);
                    }

                    string originalPath = Path.Combine(originalDirectory, moduleName);
                    string shadowCopyPath = Path.Combine(shadowCopyDirectory, moduleName);

                    var moduleCopy = CopyFile(originalPath, shadowCopyPath);
                    moduleBuilder.Add(CreateModuleMetadata(moduleCopy));
                }

                var modules = (moduleBuilder != null) ? moduleBuilder.ToImmutable() : ImmutableArray.Create(manifestModule);

                fault = false;
                return new AssemblyMetadata(modules);
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

                if (moduleBuilder != null)
                {
                    moduleBuilder.Free();
                }
            }
        }

        private static ModuleMetadata CreateModuleMetadata(ShadowCopy copy)
        {
            // The Stream can be held by the ModuleMetadata to read MD on demand.
            // The constructor call is now a hand off.
            return new ModuleMetadata(copy.FullPath, copy.Stream, leaveOpen: false, readLazily: true, entireImageRequired: false);
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

        private ShadowCopy CopyFile(string originalPath, string shadowCopyPath, bool fileMayNotExist = false)
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
                FileUtilities.PrepareDeleteOnCloseStreamForDisposal(stream);
#else
                var stream = new FileStream(shadowCopyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
                return new ShadowCopy(stream, originalPath, shadowCopyPath);
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
            get { return shadowCopies.Count; }
        }

        #endregion
    }
}
