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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Manages metadata references for VS projects. 
    /// </summary>
    /// <remarks>
    /// They monitor changes in the underlying files and provide snapshot references (subclasses of <see cref="PortableExecutableReference"/>) 
    /// that can be passed to the compiler. These snapshot references serve the underlying metadata blobs from a VS-wide storage, if possible, 
    /// from <see cref="ITemporaryStorageServiceInternal"/>.
    /// </remarks>
    internal sealed partial class VisualStudioMetadataReferenceManager : IWorkspaceService
    {
        private static readonly Guid s_IID_IMetaDataImport = new("7DAC8207-D3AE-4c75-9B67-92801A497D44");
        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new();

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

        internal IVsFileChangeEx FileChangeService { get; }

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

            FileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
            Assumes.Present(FileChangeService);
            _temporaryStorageService = temporaryStorageService;
            Assumes.Present(_temporaryStorageService);
        }

        internal IEnumerable<ITemporaryStreamStorageInternal>? GetStorages(string fullPath, DateTime snapshotTimestamp)
        {
            var key = new FileKey(fullPath, snapshotTimestamp);
            // check existing metadata
            if (_metadataCache.TryGetSource(key, out var source))
            {
                if (source is RecoverableMetadataValueSource metadata)
                {
                    return metadata.GetStorages();
                }
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
            {
                return metadata;
            }

            if (VsSmartScopeCandidate(key.FullPath) && TryCreateAssemblyMetadataFromMetadataImporter(key, out var newMetadata))
            {
                var metadataValueSource = new ConstantValueSource<Optional<AssemblyMetadata>>(newMetadata);
                if (!_metadataCache.GetOrAddMetadata(key, metadataValueSource, out metadata))
                {
                    newMetadata.Dispose();
                }

                return metadata;
            }

            // use temporary storage
            var storages = new List<TemporaryStorageService.TemporaryStreamStorage>();
            newMetadata = CreateAssemblyMetadataFromTemporaryStorage(key, storages);

            // don't dispose assembly metadata since it shares module metadata
            if (!_metadataCache.GetOrAddMetadata(key, new RecoverableMetadataValueSource(newMetadata, storages), out metadata))
            {
                newMetadata.Dispose();
            }

            // guarantee that the metadata is alive while we add the source to the cache
            GC.KeepAlive(newMetadata);

            return metadata;
        }

        /// <exception cref="IOException"/>
        /// <exception cref="BadImageFormatException" />
        private AssemblyMetadata CreateAssemblyMetadataFromTemporaryStorage(
            FileKey fileKey, List<TemporaryStorageService.TemporaryStreamStorage> storages)
        {
            var moduleMetadata = CreateModuleMetadataFromTemporaryStorage(fileKey, storages);
            return CreateAssemblyMetadata(fileKey, moduleMetadata, storages, CreateModuleMetadataFromTemporaryStorage);
        }

        private ModuleMetadata CreateModuleMetadataFromTemporaryStorage(
            FileKey moduleFileKey, List<TemporaryStorageService.TemporaryStreamStorage>? storages)
        {
            GetStorageInfoFromTemporaryStorage(moduleFileKey, out var storage, out var stream);

            unsafe
            {
                // For an unmanaged memory stream, ModuleMetadata can take ownership directly.
                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);

                // hold onto storage if requested
                storages?.Add(storage);

                return metadata;
            }
        }

        private void GetStorageInfoFromTemporaryStorage(
            FileKey moduleFileKey, out TemporaryStorageService.TemporaryStreamStorage storage, out UnmanagedMemoryStream stream)
        {
            int size;
            using (var copyStream = SerializableBytes.CreateWritableStream())
            {
                // open a file and let it go as soon as possible
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

                // copy over the data to temp storage and let pooled stream go
                storage = _temporaryStorageService.CreateTemporaryStreamStorage();

                copyStream.Position = 0;
                storage.WriteStream(copyStream);
            }

            // get stream that owns the underlying unmanaged memory.
            stream = storage.ReadStream(CancellationToken.None);

            // stream size must be same as what metadata reader said the size should be.
            Contract.ThrowIfFalse(stream.Length == size);

            return;
        }

        private static void StreamCopy(Stream source, Stream destination, int start, int length)
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

        /// <exception cref="IOException"/>
        /// <exception cref="BadImageFormatException" />
        private bool TryCreateAssemblyMetadataFromMetadataImporter(FileKey fileKey, [NotNullWhen(true)] out AssemblyMetadata? metadata)
        {
            metadata = null;

            var manifestModule = TryCreateModuleMetadataFromMetadataImporter(fileKey);
            if (manifestModule == null)
            {
                return false;
            }

            metadata = CreateAssemblyMetadata(fileKey, manifestModule, storages: null, CreateModuleMetadata);
            return true;
        }

        private ModuleMetadata? TryCreateModuleMetadataFromMetadataImporter(FileKey moduleFileKey)
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

        private ModuleMetadata CreateModuleMetadata(FileKey moduleFileKey, List<TemporaryStorageService.TemporaryStreamStorage>? storages)
        {
            var metadata = TryCreateModuleMetadataFromMetadataImporter(moduleFileKey);
            // getting metadata didn't work out through importer. fallback to shadow copy one
            metadata ??= CreateModuleMetadataFromTemporaryStorage(moduleFileKey, storages);

            return metadata;
        }

        private bool TryGetFileMappingFromMetadataImporter(FileKey fileKey, [NotNullWhen(true)] out IMetaDataInfo? info, out IntPtr pImage, out long length)
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

        /// <exception cref="IOException"/>
        /// <exception cref="BadImageFormatException" />
        private static AssemblyMetadata CreateAssemblyMetadata(
            FileKey fileKey, ModuleMetadata manifestModule, List<TemporaryStorageService.TemporaryStreamStorage>? storages,
            Func<FileKey, List<TemporaryStorageService.TemporaryStreamStorage>?, ModuleMetadata> moduleMetadataFactory)
        {
            var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();

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
                var metadata = moduleMetadataFactory(moduleFileKey, storages);

                moduleBuilder.Add(metadata);
            }

            if (moduleBuilder.Count == 0)
            {
                moduleBuilder.Add(manifestModule);
            }

            return AssemblyMetadata.Create(
                moduleBuilder.ToImmutableAndFree());
        }

        public void DisconnectFromVisualStudioNativeServices()
        {
            using (_readerWriterLock.DisposableWrite())
            {
                // IVsSmartOpenScope can't be used as we shutdown, and this is pretty commonly hit according to 
                // Windows Error Reporting as we try creating metadata for compilations.
                SmartOpenScopeServiceOpt = null;
            }
        }
    }
}
