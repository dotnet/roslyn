// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

using static TemporaryStorageService;

/// <summary>
/// Manages metadata references for VS projects. 
/// </summary>
/// <remarks>
/// They monitor changes in the underlying files and provide snapshot references (subclasses of <see cref="PortableExecutableReference"/>) 
/// that can be passed to the compiler. These snapshot references serve the underlying metadata blobs from a VS-wide storage, if possible, 
/// from <see cref="ITemporaryStorageServiceInternal"/>.
/// </remarks>
internal sealed partial class VisualStudioMetadataReferenceManager : IWorkspaceService, IDisposable
{
    private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new();

    /// <summary>
    /// Mapping from an <see cref="AssemblyMetadata"/> we created, to the identifiers identifying the memory mapped
    /// files (mmf) corresponding to that assembly and all the modules within it.  This is kept around to make OOP
    /// syncing more efficient. Specifically, since we know we dumped the assembly into an mmf, we can just send the mmf
    /// name/offset/length to the remote process, and it can map that same memory in directly, instead of needing the
    /// host to send the entire contents of the assembly over the channel to the OOP process.
    /// </summary>
    private static readonly ConditionalWeakTable<AssemblyMetadata, IReadOnlyList<TemporaryStorageStreamHandle>> s_metadataToStorageHandles = new();

    private readonly object _metadataCacheLock = new();

    /// <summary>
    /// Access locked with <see cref="_metadataCacheLock"/>.  Maps from file path to metadata and the last time the
    /// metadata was written to.  We keep this around until we see the file has changed on disk, at which point we'll
    /// compute the new metadata and update this cache, allowing the old metadata to be released.  Note: this does mean
    /// that metadata that is no longer used, will be kept around indefinitely.
    /// </summary>
    private readonly Dictionary<string, (DateTime lastWriteTime, AssemblyMetadata metadata)> _metadataCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly ImmutableArray<string> _runtimeDirectories;
    private readonly TemporaryStorageService _temporaryStorageService;
    private readonly IVsXMLMemberIndexService _xmlMemberIndexService;
    private readonly ReaderWriterLockSlim _smartOpenScopeLock = new();

    /// <summary>
    /// The smart open scope service. This can be null during shutdown when using the service might crash. Any
    /// use of this field or derived types should be synchronized with <see cref="_smartOpenScopeLock"/> to ensure
    /// you don't grab the field and then use it while shutdown continues.
    /// </summary>
    private IVsSmartOpenScope? SmartOpenScopeServiceOpt { get; set; }

    public VisualStudioMetadataReferenceManager(
        IServiceProvider serviceProvider,
        TemporaryStorageService temporaryStorageService)
    {
        _runtimeDirectories = GetRuntimeDirectories();

        _xmlMemberIndexService = (IVsXMLMemberIndexService)serviceProvider.GetService(typeof(SVsXMLMemberIndexService));
        Assumes.Present(_xmlMemberIndexService);

        SmartOpenScopeServiceOpt = (IVsSmartOpenScope)serviceProvider.GetService(typeof(SVsSmartOpenScope));
        Assumes.Present(SmartOpenScopeServiceOpt);

        _temporaryStorageService = temporaryStorageService;
        Assumes.Present(_temporaryStorageService);
    }

    public void Dispose()
    {
        using (_smartOpenScopeLock.DisposableWrite())
        {
            // IVsSmartOpenScope can't be used as we shutdown, and this is pretty commonly hit according to 
            // Windows Error Reporting as we try creating metadata for compilations.
            SmartOpenScopeServiceOpt = null;
        }
    }

    private bool TryGetMetadata(string filePath, DateTime lastWriteTime, [NotNullWhen(true)] out AssemblyMetadata? metadata)
    {
        lock (_metadataCacheLock)
        {
            if (_metadataCache.TryGetValue(filePath, out var tuple) &&
                tuple.lastWriteTime == lastWriteTime)
            {
                metadata = tuple.metadata;
                return true;
            }
        }

        metadata = null;
        return false;
    }

    public IReadOnlyList<TemporaryStorageStreamHandle>? GetStorageHandles(string fullPath, DateTime snapshotTimestamp)
    {
        if (TryGetMetadata(fullPath, snapshotTimestamp, out var metadata) &&
            s_metadataToStorageHandles.TryGetValue(metadata, out var handles))
        {
            return handles;
        }

        return null;
    }

    public PortableExecutableReference CreateMetadataReferenceSnapshot(string filePath, MetadataReferenceProperties properties)
        => new VisualStudioPortableExecutableReference(this, properties, filePath, fileChangeTracker: null);

    private bool VsSmartScopeCandidate(string fullPath)
        => _runtimeDirectories.Any(static (d, fullPath) => fullPath.StartsWith(d, StringComparison.OrdinalIgnoreCase), fullPath);

    internal static IEnumerable<string> GetReferencePaths()
    {
        // TODO:
        // WORKAROUND: properly enumerate them
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0");
    }

    private static ImmutableArray<string> GetRuntimeDirectories()
    {
        return GetReferencePaths().Concat(
            new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                RuntimeEnvironment.GetRuntimeDirectory()
            }).Select(FileUtilities.NormalizeDirectoryPath).ToImmutableArray();
    }

    /// <exception cref="IOException"/>
    /// <exception cref="BadImageFormatException" />
    internal Metadata GetMetadata(string fullPath, DateTime snapshotTimestamp)
    {
        // check existing metadata
        if (!TryGetMetadata(fullPath, snapshotTimestamp, out var metadata))
        {
            // wasn't in the cache.  create a new instance.
            metadata = GetMetadataWorker(fullPath);
            Contract.ThrowIfNull(metadata);

            lock (_metadataCacheLock)
            {
                // Now try to create and add the metadata to the cache. If we fail to add it (because some other thread
                // beat us to this), then Dispose the metadata we just created and will return the existing metadata
                // instead.
                if (TryGetMetadata(fullPath, snapshotTimestamp, out var cachedMetadata))
                {
                    metadata.Dispose();
                    return cachedMetadata;
                }

                // don't use "Add" since key might already exist with stale metadata
                _metadataCache[fullPath] = (snapshotTimestamp, metadata);
                return metadata;
            }
        }

        return metadata;

        AssemblyMetadata GetMetadataWorker(string fullPath)
        {
            var (metadata, handles) = VsSmartScopeCandidate(fullPath)
                ? CreateAssemblyMetadataFromMetadataImporter(fullPath)
                : CreateAssemblyMetadata(fullPath, fullPath => GetMetadataFromTemporaryStorage(fullPath, _temporaryStorageService));

            if (handles != null)
                s_metadataToStorageHandles.Add(metadata, handles);

            return metadata;
        }
    }

    private static (ModuleMetadata metadata, TemporaryStorageStreamHandle storageHandle) GetMetadataFromTemporaryStorage(
        string fullPath, TemporaryStorageService temporaryStorageService)
    {
        GetStorageInfoFromTemporaryStorage(fullPath, temporaryStorageService, out var storageHandle, out var stream);

        unsafe
        {
            // For an unmanaged memory stream, ModuleMetadata can take ownership directly. Passing in stream.Dispose
            // here will also ensure that as long as this metdata is alive, we'll keep the memory-mapped-file it points
            // to alive.
            var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
            return (metadata, storageHandle);
        }

        static void GetStorageInfoFromTemporaryStorage(
            string fullPath, TemporaryStorageService temporaryStorageService, out TemporaryStorageStreamHandle storageHandle, out UnmanagedMemoryStream stream)
        {
            int size;

            // Create a temp stream in memory to copy the metadata bytes into.
            using (var copyStream = SerializableBytes.CreateWritableStream())
            {
                // Open a file on disk, find the metadata section, copy those bytes into the temp stream, and release
                // the file immediately after.
                using (var fileStream = FileUtilities.OpenRead(fullPath))
                {
                    var headers = new PEHeaders(fileStream);

                    var offset = headers.MetadataStartOffset;
                    size = headers.MetadataSize;

                    // given metadata contains no metadata info.
                    // throw bad image format exception so that we can show right diagnostic to user.
                    if (size <= 0)
                    {
                        throw new BadImageFormatException();
                    }

                    StreamCopy(fileStream, copyStream, offset, size);
                }

                // Now, copy over the metadata bytes into a memory mapped file.  This will keep it fixed in a single
                // location, so we can create a metadata value wrapping that.  This will also let us share the memory
                // for that metadata value with our OOP process.
                copyStream.Position = 0;
                storageHandle = temporaryStorageService.WriteToTemporaryStorage(copyStream);
            }

            // Now, read the data from the memory-mapped-file back into a stream that we load into the metadata value.
            // The ITemporaryStorageStreamHandle should have given us an UnmanagedMemoryStream
            // since this only runs on Windows for VS.
            stream = (UnmanagedMemoryStream)storageHandle.ReadFromTemporaryStorage();

            // stream size must be same as what metadata reader said the size should be.
            Contract.ThrowIfFalse(stream.Length == size);
        }

        static void StreamCopy(Stream source, Stream destination, int start, int length)
        {
            source.Position = start;

            var buffer = SharedPools.ByteArray.Allocate();

            int read;
            var left = length;
            while ((read = source.Read(buffer, 0, Math.Min(left, buffer.Length))) != 0)
            {
                destination.Write(buffer, 0, read);
                left -= read;
            }

            SharedPools.ByteArray.Free(buffer);
        }
    }

    /// <exception cref="IOException"/>
    /// <exception cref="BadImageFormatException" />
    private (AssemblyMetadata assemblyMetadata, IReadOnlyList<TemporaryStorageStreamHandle>? handles) CreateAssemblyMetadataFromMetadataImporter(string fullPath)
    {
        return CreateAssemblyMetadata(fullPath, fullPath =>
        {
            var metadata = TryCreateModuleMetadataFromMetadataImporter(fullPath);
            if (metadata != null)
                return (metadata, storageHandle: null);

            // getting metadata didn't work out through importer. fallback to shadow copy one
            return GetMetadataFromTemporaryStorage(fullPath, _temporaryStorageService);
        });

        ModuleMetadata? TryCreateModuleMetadataFromMetadataImporter(string fullPath)
        {
            if (!TryGetFileMappingFromMetadataImporter(fullPath, out var info, out var pImage, out var length))
            {
                return null;
            }

            Debug.Assert(pImage != IntPtr.Zero, "Base address should not be zero if GetFileFlatMapping call succeeded.");

            var metadata = ModuleMetadata.CreateFromImage(pImage, (int)length);
            s_lifetimeMap.Add(metadata, info);

            return metadata;
        }

        bool TryGetFileMappingFromMetadataImporter(string fullPath, [NotNullWhen(true)] out IMetaDataInfo? info, out IntPtr pImage, out long length)
        {
            // We might not be able to use COM services to get this if VS is shutting down. We'll synchronize to make sure this
            // doesn't race against 
            using (_smartOpenScopeLock.DisposableRead())
            {
                info = null;
                pImage = default;
                length = default;

                if (SmartOpenScopeServiceOpt == null)
                {
                    return false;
                }

                if (ErrorHandler.Failed(SmartOpenScopeServiceOpt.OpenScope(fullPath, (uint)CorOpenFlags.ReadOnly, s_IID_IMetaDataImport, out var ppUnknown)))
                {
                    return false;
                }

                info = ppUnknown as IMetaDataInfo;
                if (info == null)
                {
                    return false;
                }

                return ErrorHandler.Succeeded(info.GetFileMapping(out pImage, out length, out var mappingType)) && mappingType == CorFileMapping.Flat;
            }
        }
    }

    /// <exception cref="IOException"/>
    /// <exception cref="BadImageFormatException" />
    private static (AssemblyMetadata assemblyMetadata, IReadOnlyList<TemporaryStorageStreamHandle>? handles) CreateAssemblyMetadata(
        string fullPath,
        Func<string, (ModuleMetadata moduleMetadata, TemporaryStorageStreamHandle? storageHandle)> moduleMetadataFactory)
    {
        var (manifestModule, manifestHandle) = moduleMetadataFactory(fullPath);
        var moduleNames = manifestModule.GetModuleNames();

        var modules = new FixedSizeArrayBuilder<ModuleMetadata>(1 + moduleNames.Length);
        var handles = new FixedSizeArrayBuilder<TemporaryStorageStreamHandle?>(1 + moduleNames.Length);

        modules.Add(manifestModule);
        handles.Add(manifestHandle);

        string? assemblyDir = null;
        foreach (var moduleName in moduleNames)
        {
            assemblyDir ??= Path.GetDirectoryName(fullPath);

            // Suppression should be removed or addressed https://github.com/dotnet/roslyn/issues/41636
            var moduleFullPath = PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName)!;

            var (moduleMetadata, moduleHandle) = moduleMetadataFactory(moduleFullPath);
            modules.Add(moduleMetadata);
            handles.Add(moduleHandle);
        }

        var assembly = AssemblyMetadata.Create(modules.MoveToImmutable());

        // If we got any null handles, then we weren't able to map this whole assembly into memory mapped files. So we
        // can't use those to transfer over the data efficiently to the OOP process.  In that case, we don't store the
        // handles at all.
        var storageHandles = handles.MoveToImmutable();
        return (assembly, storageHandles.Any(h => h is null) ? null : storageHandles);
    }

    public static class TestAccessor
    {
        public static (AssemblyMetadata assemblyMetadata, IReadOnlyList<TemporaryStorageStreamHandle>? handles) CreateAssemblyMetadata(
            string fullPath, TemporaryStorageService temporaryStorageService)
            => VisualStudioMetadataReferenceManager.CreateAssemblyMetadata(fullPath, fullPath => GetMetadataFromTemporaryStorage(fullPath, temporaryStorageService));
    }
}
