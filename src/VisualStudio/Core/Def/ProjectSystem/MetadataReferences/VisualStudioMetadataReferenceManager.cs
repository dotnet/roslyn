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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

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
    private static readonly Guid s_IID_IMetaDataImport = new("7DAC8207-D3AE-4c75-9B67-92801A497D44");

    private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new();

    /// <summary>
    /// Mapping from an <see cref="AssemblyMetadata"/> we created, to the identifiers identifying the memory mapped
    /// files (mmf) corresponding to that assembly and all the modules within it.  This is kept around to make OOP
    /// syncing more efficient. Specifically, since we know we dumped the assembly into an mmf, we can just send the mmf
    /// name/offset/length to the remote process, and it can map that same memory in directly, instead of needing the
    /// host to send the entire contents of the assembly over the channel to the OOP process.
    /// </summary>
    private static readonly ConditionalWeakTable<AssemblyMetadata, IReadOnlyList<TemporaryStorageHandle>> s_metadataToStorageHandles = new();

    private readonly MetadataCache _metadataCache = new();
    private readonly ImmutableArray<string> _runtimeDirectories;
    private readonly TemporaryStorageService _temporaryStorageService;

    internal IVsXMLMemberIndexService XmlMemberIndexService { get; }

    /// <summary>
    /// The smart open scope service. This can be null during shutdown when using the service might crash. Any
    /// use of this field or derived types should be synchronized with <see cref="_readerWriterLock"/> to ensure
    /// you don't grab the field and then use it while shutdown continues.
    /// </summary>
    private IVsSmartOpenScope? SmartOpenScopeServiceOpt { get; set; }

    private readonly ReaderWriterLockSlim _readerWriterLock = new();

    internal VisualStudioMetadataReferenceManager(
        IServiceProvider serviceProvider,
        TemporaryStorageService temporaryStorageService)
    {
        _runtimeDirectories = GetRuntimeDirectories();

        XmlMemberIndexService = (IVsXMLMemberIndexService)serviceProvider.GetService(typeof(SVsXMLMemberIndexService));
        Assumes.Present(XmlMemberIndexService);

        SmartOpenScopeServiceOpt = (IVsSmartOpenScope)serviceProvider.GetService(typeof(SVsSmartOpenScope));
        Assumes.Present(SmartOpenScopeServiceOpt);

        _temporaryStorageService = temporaryStorageService;
        Assumes.Present(_temporaryStorageService);
    }

    public void Dispose()
    {
        using (_readerWriterLock.DisposableWrite())
        {
            // IVsSmartOpenScope can't be used as we shutdown, and this is pretty commonly hit according to 
            // Windows Error Reporting as we try creating metadata for compilations.
            SmartOpenScopeServiceOpt = null;
        }
    }

    public IReadOnlyList<TemporaryStorageHandle>? GetStorageHandles(string fullPath, DateTime snapshotTimestamp)
    {
        var key = new FileKey(fullPath, snapshotTimestamp);
        // check existing metadata
        if (_metadataCache.TryGetMetadata(key, out var source) &&
            s_metadataToStorageHandles.TryGetValue(source, out var handles))
        {
            return handles;
        }

        return null;
    }

    public PortableExecutableReference CreateMetadataReferenceSnapshot(string filePath, MetadataReferenceProperties properties)
        => new VisualStudioMetadataReference.Snapshot(this, properties, filePath, fileChangeTrackerOpt: null);

    public void ClearCache()
        => _metadataCache.ClearCache();

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
        var key = new FileKey(fullPath, snapshotTimestamp);
        // check existing metadata
        if (_metadataCache.TryGetMetadata(key, out var metadata))
            return metadata;

        var newMetadata = GetMetadataWorker();

        if (!_metadataCache.GetOrAddMetadata(key, newMetadata, out metadata))
            newMetadata.Dispose();

        return metadata;

        AssemblyMetadata GetMetadataWorker()
        {
            if (VsSmartScopeCandidate(key.FullPath))
            {
                var newMetadata = CreateAssemblyMetadataFromMetadataImporter(key);
                return newMetadata;
            }
            else
            {
                // use temporary storage
                using var _ = ArrayBuilder<TemporaryStorageHandle>.GetInstance(out var storageHandles);
                var newMetadata = CreateAssemblyMetadata(key, key =>
                {
                    // <exception cref="IOException"/>
                    // <exception cref="BadImageFormatException" />
                    GetMetadataFromTemporaryStorage(key, out var storageHandle, out var metadata);
                    storageHandles.Add(storageHandle);
                    return metadata;
                });

                s_metadataToStorageHandles.Add(newMetadata, storageHandles.ToImmutable());

                return newMetadata;
            }
        }
    }

    private void GetMetadataFromTemporaryStorage(
        FileKey moduleFileKey, out TemporaryStorageHandle storageHandle, out ModuleMetadata metadata)
    {
        GetStorageInfoFromTemporaryStorage(moduleFileKey, out storageHandle, out var stream);

        unsafe
        {
            // For an unmanaged memory stream, ModuleMetadata can take ownership directly.
            metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
        }

        return;

        void GetStorageInfoFromTemporaryStorage(
            FileKey moduleFileKey, out TemporaryStorageHandle storageHandle, out UnmanagedMemoryStream stream)
        {
            int size;

            // Create a temp stream in memory to copy the metadata bytes into.
            using (var copyStream = SerializableBytes.CreateWritableStream())
            {
                // Open a file on disk, find the metadata section, copy those bytes into the temp stream, and release
                // the file immediately after.
                using (var fileStream = FileUtilities.OpenRead(moduleFileKey.FullPath))
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
                storageHandle = _temporaryStorageService.WriteToTemporaryStorage(copyStream, CancellationToken.None);
            }

            // Now, read the data from the memory-mapped-file back into a stream that we load into the metadata value.
            stream = _temporaryStorageService.ReadFromTemporaryStorageService(storageHandle.Identifier, CancellationToken.None);
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
    private AssemblyMetadata CreateAssemblyMetadataFromMetadataImporter(FileKey fileKey)
    {
        using var _ = ArrayBuilder<TemporaryStorageHandle>.GetInstance(out var storageHandles);
        var newMetadata = CreateAssemblyMetadata(fileKey, fileKey =>
        {
            var metadata = TryCreateModuleMetadataFromMetadataImporter(fileKey);

            // getting metadata didn't work out through importer. fallback to shadow copy one
            if (metadata == null)
            {
                GetMetadataFromTemporaryStorage(fileKey, out var storageHandle, out metadata);
                storageHandles.Add(storageHandle);
            }

            return metadata;
        });

        s_metadataToStorageHandles.Add(newMetadata, storageHandles.ToImmutable());

        return newMetadata;

        ModuleMetadata? TryCreateModuleMetadataFromMetadataImporter(FileKey moduleFileKey)
        {
            if (!TryGetFileMappingFromMetadataImporter(moduleFileKey, out var info, out var pImage, out var length))
            {
                return null;
            }

            Debug.Assert(pImage != IntPtr.Zero, "Base address should not be zero if GetFileFlatMapping call succeeded.");

            var metadata = ModuleMetadata.CreateFromImage(pImage, (int)length);
            s_lifetimeMap.Add(metadata, info);

            return metadata;
        }

        bool TryGetFileMappingFromMetadataImporter(FileKey fileKey, [NotNullWhen(true)] out IMetaDataInfo? info, out IntPtr pImage, out long length)
        {
            // We might not be able to use COM services to get this if VS is shutting down. We'll synchronize to make sure this
            // doesn't race against 
            using (_readerWriterLock.DisposableRead())
            {
                // here, we don't care about timestamp since all those bits should be part of Fx. and we assume that 
                // it won't be changed in the middle of VS running.
                var fullPath = fileKey.FullPath;

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
    private static AssemblyMetadata CreateAssemblyMetadata(
        FileKey fileKey,
        Func<FileKey, ModuleMetadata> moduleMetadataFactory)
    {
        var manifestModule = moduleMetadataFactory(fileKey);

        using var _ = ArrayBuilder<ModuleMetadata>.GetInstance(out var moduleBuilder);

        string? assemblyDir = null;
        foreach (var moduleName in manifestModule.GetModuleNames())
        {
            if (assemblyDir is null)
            {
                moduleBuilder.Add(manifestModule);
                assemblyDir = Path.GetDirectoryName(fileKey.FullPath);
            }

            // Suppression should be removed or addressed https://github.com/dotnet/roslyn/issues/41636
            var moduleFileKey = FileKey.Create(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName)!);
            var metadata = moduleMetadataFactory(moduleFileKey);

            moduleBuilder.Add(metadata);
        }

        if (moduleBuilder.Count == 0)
            moduleBuilder.Add(manifestModule);

        return AssemblyMetadata.Create(moduleBuilder.ToImmutable());
    }
}
