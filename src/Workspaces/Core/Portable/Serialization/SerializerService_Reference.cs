// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService
    {
        private const int MetadataFailed = int.MaxValue;

        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new();

        public static Checksum CreateChecksum(MetadataReference reference, CancellationToken cancellationToken)
        {
            if (reference is PortableExecutableReference portable)
            {
                return CreatePortableExecutableReferenceChecksum(portable, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        private static bool IsAnalyzerReferenceWithShadowCopyLoader(AnalyzerFileReference reference)
            => reference.AssemblyLoader is ShadowCopyAnalyzerAssemblyLoader;

        public static Checksum CreateChecksum(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                switch (reference)
                {
                    case AnalyzerFileReference file:
                        writer.WriteString(file.FullPath);
                        writer.WriteBoolean(IsAnalyzerReferenceWithShadowCopyLoader(file));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(reference);
                }
            }

            stream.Position = 0;
            return Checksum.Create(stream);
        }

        public virtual void WriteMetadataReferenceTo(MetadataReference reference, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
        {
            if (reference is PortableExecutableReference portable)
            {
                if (portable is ISupportTemporaryStorage supportTemporaryStorage)
                {
                    if (TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(supportTemporaryStorage, writer, context, cancellationToken))
                    {
                        return;
                    }
                }

                WritePortableExecutableReferenceTo(portable, writer, cancellationToken);
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public virtual MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var type = reader.ReadString();
            if (type == nameof(PortableExecutableReference))
            {
                return ReadPortableExecutableReferenceFrom(reader, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        public virtual void WriteAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reference)
            {
                case AnalyzerFileReference file:
                    writer.WriteString(nameof(AnalyzerFileReference));
                    writer.WriteString(file.FullPath);
                    writer.WriteBoolean(IsAnalyzerReferenceWithShadowCopyLoader(file));
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(reference);
            }
        }

        public virtual AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = reader.ReadString();
            if (type == nameof(AnalyzerFileReference))
            {
                var fullPath = reader.ReadString();
                var shadowCopy = reader.ReadBoolean();
                return new AnalyzerFileReference(fullPath, _analyzerLoaderProvider.GetLoader(new AnalyzerAssemblyLoaderOptions(shadowCopy)));
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        protected static void WritePortableExecutableReferenceHeaderTo(
            PortableExecutableReference reference, SerializationKinds kind, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(nameof(PortableExecutableReference));
            writer.WriteInt32((int)kind);

            WritePortableExecutableReferencePropertiesTo(reference, writer, cancellationToken);
        }

        private static void WritePortableExecutableReferencePropertiesTo(PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteTo(reference.Properties, writer, cancellationToken);
            writer.WriteString(reference.FilePath);
        }

        private static Checksum CreatePortableExecutableReferenceChecksum(PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                WritePortableExecutableReferencePropertiesTo(reference, writer, cancellationToken);
                WriteMvidsTo(TryGetMetadata(reference), writer, cancellationToken);
            }

            stream.Position = 0;
            return Checksum.Create(stream);
        }

        private static void WriteMvidsTo(Metadata? metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (metadata == null)
            {
                // handle error case where we couldn't load metadata of the reference.
                // this basically won't write anything to writer
                return;
            }

            if (metadata is AssemblyMetadata assemblyMetadata)
            {
                if (!TryGetModules(assemblyMetadata, out var modules))
                {
                    // Gracefully bail out without writing anything to the writer.
                    return;
                }

                writer.WriteInt32((int)assemblyMetadata.Kind);

                writer.WriteInt32(modules.Length);

                foreach (var module in modules)
                {
                    WriteMvidTo(module, writer, cancellationToken);
                }

                return;
            }

            WriteMvidTo((ModuleMetadata)metadata, writer, cancellationToken);
        }

        private static bool TryGetModules(AssemblyMetadata assemblyMetadata, out ImmutableArray<ModuleMetadata> modules)
        {
            // Gracefully handle documented exceptions from 'GetModules' invocation.
            try
            {
                modules = assemblyMetadata.GetModules();
                return true;
            }
            catch (Exception ex) when (ex is BadImageFormatException or
                                       IOException or
                                       ObjectDisposedException)
            {
                modules = default;
                return false;
            }
        }

        private static void WriteMvidTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)metadata.Kind);

            var metadataReader = metadata.GetMetadataReader();

            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            var guid = metadataReader.GetGuid(mvidHandle);

            writer.WriteGuid(guid);
        }

        private static void WritePortableExecutableReferenceTo(
            PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WritePortableExecutableReferenceHeaderTo(reference, SerializationKinds.Bits, writer, cancellationToken);

            WriteTo(TryGetMetadata(reference), writer, cancellationToken);

            // TODO: what I should do with documentation provider? it is not exposed outside
        }

        private PortableExecutableReference ReadPortableExecutableReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = (SerializationKinds)reader.ReadInt32();
            if (kind is SerializationKinds.Bits or SerializationKinds.MemoryMapFile)
            {
                var properties = ReadMetadataReferencePropertiesFrom(reader, cancellationToken);

                var filePath = reader.ReadString();

                var tuple = TryReadMetadataFrom(reader, kind, cancellationToken);
                if (tuple == null)
                {
                    // TODO: deal with xml document provider properly
                    //       should we shadow copy xml doc comment?

                    // image doesn't exist
                    return new MissingMetadataReference(properties, filePath, XmlDocumentationProvider.Default);
                }

                // for now, we will use IDocumentationProviderService to get DocumentationProvider for metadata
                // references. if the service is not available, then use Default (NoOp) provider.
                // since xml doc comment is not part of solution snapshot, (like xml reference resolver or strong name
                // provider) this provider can also potentially provide content that is different than one in the host. 
                // an alternative approach of this is synching content of xml doc comment to remote host as well
                // so that we can put xml doc comment as part of snapshot. but until we believe that is necessary,
                // it will go with simpler approach
                var documentProvider = filePath != null && _documentationService != null ?
                    _documentationService.GetDocumentationProvider(filePath) : XmlDocumentationProvider.Default;

                return new SerializedMetadataReference(
                    properties, filePath, tuple.Value.metadata, tuple.Value.storages, documentProvider);
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private static void WriteTo(MetadataReferenceProperties properties, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)properties.Kind);
            writer.WriteValue(properties.Aliases.ToArray());
            writer.WriteBoolean(properties.EmbedInteropTypes);
        }

        private static MetadataReferenceProperties ReadMetadataReferencePropertiesFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = (MetadataImageKind)reader.ReadInt32();
            var aliases = reader.ReadArray<string>().ToImmutableArrayOrEmpty();
            var embedInteropTypes = reader.ReadBoolean();

            return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
        }

        private static void WriteTo(Metadata? metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (metadata == null)
            {
                // handle error case where metadata failed to load
                writer.WriteInt32(MetadataFailed);
                return;
            }

            if (metadata is AssemblyMetadata assemblyMetadata)
            {
                if (!TryGetModules(assemblyMetadata, out var modules))
                {
                    // Gracefully handle error case where unable to get modules.
                    writer.WriteInt32(MetadataFailed);
                    return;
                }

                writer.WriteInt32((int)assemblyMetadata.Kind);

                writer.WriteInt32(modules.Length);

                foreach (var module in modules)
                {
                    WriteTo(module, writer, cancellationToken);
                }

                return;
            }

            WriteTo((ModuleMetadata)metadata, writer, cancellationToken);
        }

        private static bool TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(
            ISupportTemporaryStorage reference, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
        {
            var storages = reference.GetStorages();
            if (storages == null)
            {
                return false;
            }

            // Not clear if name should be allowed to be null here (https://github.com/dotnet/roslyn/issues/43037)
            using var pooled = Creator.CreateList<(string? name, long offset, long size)>();

            foreach (var storage in storages)
            {
                if (storage is not ITemporaryStorageWithName storage2)
                {
                    return false;
                }

                context.AddResource(storage);

                pooled.Object.Add((storage2.Name, storage2.Offset, storage2.Size));
            }

            WritePortableExecutableReferenceHeaderTo((PortableExecutableReference)reference, SerializationKinds.MemoryMapFile, writer, cancellationToken);

            writer.WriteInt32((int)MetadataImageKind.Assembly);
            writer.WriteInt32(pooled.Object.Count);

            foreach (var (name, offset, size) in pooled.Object)
            {
                writer.WriteInt32((int)MetadataImageKind.Module);
                writer.WriteString(name);
                writer.WriteInt64(offset);
                writer.WriteInt64(size);
            }

            return true;
        }

        private (Metadata metadata, ImmutableArray<ITemporaryStreamStorageInternal> storages)? TryReadMetadataFrom(
            ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            var imageKind = reader.ReadInt32();
            if (imageKind == MetadataFailed)
            {
                // error case
                return null;
            }

            var metadataKind = (MetadataImageKind)imageKind;
            if (_storageService == null)
            {
                if (metadataKind == MetadataImageKind.Assembly)
                {
                    using var pooledMetadata = Creator.CreateList<ModuleMetadata>();

                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        metadataKind = (MetadataImageKind)reader.ReadInt32();
                        Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

#pragma warning disable CA2016 // https://github.com/dotnet/roslyn-analyzers/issues/4985
                        pooledMetadata.Object.Add(ReadModuleMetadataFrom(reader, kind));
#pragma warning restore CA2016 
                    }

                    return (AssemblyMetadata.Create(pooledMetadata.Object), storages: default);
                }

                Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);
#pragma warning disable CA2016 // https://github.com/dotnet/roslyn-analyzers/issues/4985
                return (ReadModuleMetadataFrom(reader, kind), storages: default);
#pragma warning restore CA2016
            }

            if (metadataKind == MetadataImageKind.Assembly)
            {
                using var pooledMetadata = Creator.CreateList<ModuleMetadata>();
                using var pooledStorage = Creator.CreateList<ITemporaryStreamStorageInternal>();

                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    metadataKind = (MetadataImageKind)reader.ReadInt32();
                    Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

                    var (metadata, storage) = ReadModuleMetadataFrom(reader, kind, cancellationToken);

                    pooledMetadata.Object.Add(metadata);
                    pooledStorage.Object.Add(storage);
                }

                return (AssemblyMetadata.Create(pooledMetadata.Object), pooledStorage.Object.ToImmutableArrayOrEmpty());
            }

            Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

            var moduleInfo = ReadModuleMetadataFrom(reader, kind, cancellationToken);
            return (moduleInfo.metadata, ImmutableArray.Create(moduleInfo.storage));
        }

        private (ModuleMetadata metadata, ITemporaryStreamStorageInternal storage) ReadModuleMetadataFrom(
            ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetTemporaryStorage(reader, kind, out var storage, out var length, cancellationToken);

            var storageStream = storage.ReadStream(cancellationToken);
            Contract.ThrowIfFalse(length == storageStream.Length);

            GetMetadata(storageStream, length, out var metadata, out var lifeTimeObject);

            // make sure we keep storageStream alive while Metadata is alive
            // we use conditional weak table since we can't control metadata liftetime
            if (lifeTimeObject != null)
                s_lifetimeMap.Add(metadata, lifeTimeObject);

            return (metadata, storage);
        }

        private static ModuleMetadata ReadModuleMetadataFrom(ObjectReader reader, SerializationKinds kind)
        {
            Contract.ThrowIfFalse(SerializationKinds.Bits == kind);

            var array = reader.ReadArray<byte>();
            var pinnedObject = new PinnedObject(array);

            var metadata = ModuleMetadata.CreateFromMetadata(pinnedObject.GetPointer(), array.Length);

            // make sure we keep storageStream alive while Metadata is alive
            // we use conditional weak table since we can't control metadata liftetime
            s_lifetimeMap.Add(metadata, pinnedObject);

            return metadata;
        }

        private void GetTemporaryStorage(
            ObjectReader reader, SerializationKinds kind, out ITemporaryStreamStorageInternal storage, out long length, CancellationToken cancellationToken)
        {
            if (kind == SerializationKinds.Bits)
            {
                storage = _storageService.CreateTemporaryStreamStorage();
                using var stream = SerializableBytes.CreateWritableStream();

                CopyByteArrayToStream(reader, stream, cancellationToken);

                length = stream.Length;

                stream.Position = 0;
                storage.WriteStream(stream, cancellationToken);

                return;
            }

            if (kind == SerializationKinds.MemoryMapFile)
            {
                var service2 = (ITemporaryStorageService2)_storageService;

                var name = reader.ReadString();
                var offset = reader.ReadInt64();
                var size = reader.ReadInt64();

                storage = service2.AttachTemporaryStreamStorage(name, offset, size);
                length = size;

                return;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private static void GetMetadata(Stream stream, long length, out ModuleMetadata metadata, out object? lifeTimeObject)
        {
            if (stream is UnmanagedMemoryStream unmanagedStream)
            {
                // For an unmanaged memory stream, ModuleMetadata can take ownership directly.
                unsafe
                {
                    metadata = ModuleMetadata.CreateFromMetadata(
                        (IntPtr)unmanagedStream.PositionPointer, (int)unmanagedStream.Length, unmanagedStream.Dispose);
                    lifeTimeObject = null;
                    return;
                }
            }

            PinnedObject pinnedObject;
            if (stream is MemoryStream memory &&
                memory.TryGetBuffer(out var buffer) &&
                buffer.Offset == 0)
            {
                pinnedObject = new PinnedObject(buffer.Array!);
            }
            else
            {
                var array = new byte[length];
                stream.Read(array, 0, (int)length);
                pinnedObject = new PinnedObject(array);
            }

            metadata = ModuleMetadata.CreateFromMetadata(pinnedObject.GetPointer(), (int)length);
            lifeTimeObject = pinnedObject;
        }

        private static void CopyByteArrayToStream(ObjectReader reader, Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: make reader be able to read byte[] chunk
            var content = reader.ReadArray<byte>();
            stream.Write(content, 0, content.Length);
        }

        private static void WriteTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteInt32((int)metadata.Kind);

            WriteTo(metadata.GetMetadataReader(), writer, cancellationToken);
        }

        private static unsafe void WriteTo(MetadataReader reader, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteValue(new ReadOnlySpan<byte>(reader.MetadataPointer, reader.MetadataLength));
        }

        private static void WriteUnresolvedAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer)
        {
            writer.WriteString(nameof(UnresolvedAnalyzerReference));
            writer.WriteString(reference.FullPath);
        }

        private static Metadata? TryGetMetadata(PortableExecutableReference reference)
        {
            try
            {
                return reference.GetMetadata();
            }
            catch
            {
                // we have a reference but the file the reference is pointing to
                // might not actually exist on disk.
                // in that case, rather than crashing, we will handle it gracefully.
                return null;
            }
        }

        private sealed class PinnedObject : IDisposable
        {
            // shouldn't be read-only since GCHandle is a mutable struct
            private GCHandle _gcHandle;

            public PinnedObject(byte[] array)
                => _gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            internal IntPtr GetPointer()
                => _gcHandle.AddrOfPinnedObject();

            private void OnDispose()
            {
                if (_gcHandle.IsAllocated)
                {
                    _gcHandle.Free();
                }
            }

            ~PinnedObject()
                => OnDispose();

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                OnDispose();
            }
        }

        private sealed class MissingMetadataReference : PortableExecutableReference
        {
            private readonly DocumentationProvider _provider;

            public MissingMetadataReference(
                MetadataReferenceProperties properties, string? fullPath, DocumentationProvider initialDocumentation)
                : base(properties, fullPath, initialDocumentation)
            {
                // TODO: doc comment provider is a bit weird.
                _provider = initialDocumentation;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO: properly implement this
                throw new NotImplementedException();
            }

            protected override Metadata GetMetadataImpl()
            {
                // we just throw "FileNotFoundException" even if it might not be actual reason
                // why metadata has failed to load. in this context, we don't care much on actual
                // reason. we just need to maintain failure when re-constructing solution to maintain
                // snapshot integrity. 
                //
                // if anyone care actual reason, he should get that info from original Solution.
                throw new FileNotFoundException(FilePath);
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
                => new MissingMetadataReference(properties, FilePath, _provider);
        }

        [DebuggerDisplay("{" + nameof(Display) + ",nq}")]
        private sealed class SerializedMetadataReference : PortableExecutableReference, ISupportTemporaryStorage
        {
            private readonly Metadata _metadata;
            private readonly ImmutableArray<ITemporaryStreamStorageInternal> _storagesOpt;
            private readonly DocumentationProvider _provider;

            public SerializedMetadataReference(
                MetadataReferenceProperties properties, string? fullPath,
                Metadata metadata, ImmutableArray<ITemporaryStreamStorageInternal> storagesOpt, DocumentationProvider initialDocumentation)
                : base(properties, fullPath, initialDocumentation)
            {
                _metadata = metadata;
                _storagesOpt = storagesOpt;

                _provider = initialDocumentation;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // this uses documentation provider given at the constructor
                throw ExceptionUtilities.Unreachable();
            }

            protected override Metadata GetMetadataImpl()
                => _metadata;

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
                => new SerializedMetadataReference(properties, FilePath, _metadata, _storagesOpt, _provider);

            public IReadOnlyList<ITemporaryStreamStorageInternal>? GetStorages()
                => _storagesOpt.IsDefault ? null : _storagesOpt;
        }
    }
}
