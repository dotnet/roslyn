﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
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
    /// from <see cref="ITemporaryStorageService"/>.
    /// </remarks>
    internal sealed partial class VisualStudioMetadataReferenceManager : IWorkspaceService
    {
        private static readonly Guid s_IID_IMetaDataImport = new Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44");
        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new ConditionalWeakTable<Metadata, object>();

        private readonly MetadataCache _metadataCache;
        private readonly ImmutableArray<string> _runtimeDirectories;

        private readonly ITemporaryStorageService _temporaryStorageService;

        internal IVsXMLMemberIndexService XmlMemberIndexService { get; }

        /// <summary>
        /// The smart open scope service. This can be null during shutdown when using the service might crash. Any
        /// use of this field or derived types should be synchronized with <see cref="_readerWriterLock"/> to ensure
        /// you don't grab the field and then use it while shutdown continues.
        /// </summary>
        private IVsSmartOpenScope SmartOpenScopeServiceOpt { get; set; }

        internal IVsFileChangeEx FileChangeService { get; }

        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();

        internal VisualStudioMetadataReferenceManager(IServiceProvider serviceProvider, ITemporaryStorageService temporaryStorageService)
        {
            _metadataCache = new MetadataCache();
            _runtimeDirectories = GetRuntimeDirectories();

            XmlMemberIndexService = (IVsXMLMemberIndexService)serviceProvider.GetService(typeof(SVsXMLMemberIndexService));
            SmartOpenScopeServiceOpt = (IVsSmartOpenScope)serviceProvider.GetService(typeof(SVsSmartOpenScope));

            FileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
            _temporaryStorageService = temporaryStorageService;

            Debug.Assert(XmlMemberIndexService != null);
            Debug.Assert(SmartOpenScopeServiceOpt != null);
            Debug.Assert(FileChangeService != null);
            Debug.Assert(temporaryStorageService != null);
        }

        internal IEnumerable<ITemporaryStreamStorage> GetStorages(string fullPath, DateTime snapshotTimestamp)
        {
            var key = new FileKey(fullPath, snapshotTimestamp);

            // check existing metadata
            ValueSource<AssemblyMetadata> source;
            if (_metadataCache.TryGetSource(key, out source))
            {
                var metadata = source as RecoverableMetadataValueSource;
                if (metadata != null)
                {
                    return metadata.GetStorages();
                }
            }

            return null;
        }

        public PortableExecutableReference CreateMetadataReferenceSnapshot(string filePath, MetadataReferenceProperties properties)
        {
            return new VisualStudioMetadataReference.Snapshot(this, properties, filePath);
        }

        public VisualStudioMetadataReference CreateMetadataReference(IVisualStudioHostProject hostProject, string filePath, MetadataReferenceProperties properties)
        {
            return new VisualStudioMetadataReference(this, hostProject, filePath, properties);
        }

        public void ClearCache()
        {
            _metadataCache.ClearCache();
        }

        private bool VsSmartScopeCandidate(string fullPath)
        {
            return _runtimeDirectories.Any(d => fullPath.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        }

        private static ImmutableArray<string> GetRuntimeDirectories()
        {
            return ReferencePathUtilities.GetReferencePaths().Concat(
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
            AssemblyMetadata metadata;
            if (_metadataCache.TryGetMetadata(key, out metadata))
            {
                return metadata;
            }

            AssemblyMetadata newMetadata;
            if (VsSmartScopeCandidate(key.FullPath) && TryCreateAssemblyMetadataFromMetadataImporter(key, out newMetadata))
            {
                if (!_metadataCache.TryGetOrAddMetadata(key, new WeakConstantValueSource<AssemblyMetadata>(newMetadata), out metadata))
                {
                    newMetadata.Dispose();
                }

                return metadata;
            }

            // use temporary storage
            var storages = new List<ITemporaryStreamStorage>();
            newMetadata = CreateAssemblyMetadataFromTemporaryStorage(key, storages);

            // don't dispose assembly metadata since it shares module metadata
            if (!_metadataCache.TryGetOrAddMetadata(key, new RecoverableMetadataValueSource(newMetadata, storages, s_lifetimeMap), out metadata))
            {
                newMetadata.Dispose();
            }

            return metadata;
        }

        /// <exception cref="IOException"/>
        /// <exception cref="BadImageFormatException" />
        private AssemblyMetadata CreateAssemblyMetadataFromTemporaryStorage(FileKey fileKey, List<ITemporaryStreamStorage> storages)
        {
            var moduleMetadata = CreateModuleMetadataFromTemporaryStorage(fileKey, storages);
            return CreateAssemblyMetadata(fileKey, moduleMetadata, storages, CreateModuleMetadataFromTemporaryStorage);
        }

        private ModuleMetadata CreateModuleMetadataFromTemporaryStorage(FileKey moduleFileKey, List<ITemporaryStreamStorage> storages)
        {
            ITemporaryStreamStorage storage;
            Stream stream;
            IntPtr pImage;
            GetStorageInfoFromTemporaryStorage(moduleFileKey, out storage, out stream, out pImage);

            var metadata = ModuleMetadata.CreateFromMetadata(pImage, (int)stream.Length);

            // first time, the metadata is created. tie lifetime.
            s_lifetimeMap.Add(metadata, stream);

            // hold onto storage if requested
            if (storages != null)
            {
                storages.Add(storage);
            }

            return metadata;
        }

        private void GetStorageInfoFromTemporaryStorage(FileKey moduleFileKey, out ITemporaryStreamStorage storage, out Stream stream, out IntPtr pImage)
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
                storage = _temporaryStorageService.CreateTemporaryStreamStorage(CancellationToken.None);

                copyStream.Position = 0;
                storage.WriteStream(copyStream);
            }

            // get stream that owns direct access memory
            stream = storage.ReadStream(CancellationToken.None);

            // stream size must be same as what metadata reader said the size should be.
            Contract.ThrowIfFalse(stream.Length == size);

            // under VS host, direct access should be supported
            var directAccess = (ISupportDirectMemoryAccess)stream;
            pImage = directAccess.GetPointer();
        }

        private void StreamCopy(Stream source, Stream destination, int start, int length)
        {
            source.Position = start;

            var buffer = SharedPools.ByteArray.Allocate();

            var read = 0;
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
        private bool TryCreateAssemblyMetadataFromMetadataImporter(FileKey fileKey, out AssemblyMetadata metadata)
        {
            metadata = default(AssemblyMetadata);

            var manifestModule = TryCreateModuleMetadataFromMetadataImporter(fileKey);
            if (manifestModule == null)
            {
                return false;
            }

            metadata = CreateAssemblyMetadata(fileKey, manifestModule, null, CreateModuleMetadata);
            return true;
        }

        private ModuleMetadata TryCreateModuleMetadataFromMetadataImporter(FileKey moduleFileKey)
        {
            IMetaDataInfo info;
            IntPtr pImage;
            long length;
            if (!TryGetFileMappingFromMetadataImporter(moduleFileKey, out info, out pImage, out length))
            {
                return null;
            }

            Contract.Requires(pImage != IntPtr.Zero, "Base address should not be zero if GetFileFlatMapping call succeeded.");

            var metadata = ModuleMetadata.CreateFromImage(pImage, (int)length);
            s_lifetimeMap.Add(metadata, info);

            return metadata;
        }

        private ModuleMetadata CreateModuleMetadata(FileKey moduleFileKey, List<ITemporaryStreamStorage> storages)
        {
            var metadata = TryCreateModuleMetadataFromMetadataImporter(moduleFileKey);
            if (metadata == null)
            {
                // getting metadata didn't work out through importer. fallback to shadow copy one
                metadata = CreateModuleMetadataFromTemporaryStorage(moduleFileKey, storages);
            }

            return metadata;
        }

        private bool TryGetFileMappingFromMetadataImporter(FileKey fileKey, out IMetaDataInfo info, out IntPtr pImage, out long length)
        {
            // We might not be able to use COM services to get this if VS is shutting down. We'll synchronize to make sure this
            // doesn't race against 
            using (_readerWriterLock.DisposableRead())
            {
                // here, we don't care about timestamp since all those bits should be part of Fx. and we assume that 
                // it won't be changed in the middle of VS running.
                var fullPath = fileKey.FullPath;

                info = default(IMetaDataInfo);
                pImage = default(IntPtr);
                length = default(long);

                if (SmartOpenScopeServiceOpt == null)
                {
                    return false;
                }

                var ppUnknown = default(object);
                if (ErrorHandler.Failed(SmartOpenScopeServiceOpt.OpenScope(fullPath, (uint)CorOpenFlags.ReadOnly, s_IID_IMetaDataImport, out ppUnknown)))
                {
                    return false;
                }

                info = ppUnknown as IMetaDataInfo;
                if (info == null)
                {
                    return false;
                }

                CorFileMapping mappingType;
                return ErrorHandler.Succeeded(info.GetFileMapping(out pImage, out length, out mappingType)) && mappingType == CorFileMapping.Flat;
            }
        }

        /// <exception cref="IOException"/>
        /// <exception cref="BadImageFormatException" />
        private AssemblyMetadata CreateAssemblyMetadata(
            FileKey fileKey, ModuleMetadata manifestModule, List<ITemporaryStreamStorage> storages,
            Func<FileKey, List<ITemporaryStreamStorage>, ModuleMetadata> moduleMetadataFactory)
        {
            var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();

            string assemblyDir = null;
            foreach (string moduleName in manifestModule.GetModuleNames())
            {
                if (moduleBuilder.Count == 0)
                {
                    moduleBuilder.Add(manifestModule);
                    assemblyDir = Path.GetDirectoryName(fileKey.FullPath);
                }

                var moduleFileKey = FileKey.Create(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName));
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