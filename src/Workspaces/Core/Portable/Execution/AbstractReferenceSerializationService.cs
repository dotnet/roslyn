// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal abstract class AbstractReferenceSerializationService : IReferenceSerializationService
    {
        private const int MetadataFailed = int.MaxValue;

        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new ConditionalWeakTable<Metadata, object>();

        private readonly ITemporaryStorageService _storageService;

        protected AbstractReferenceSerializationService(ITemporaryStorageService storageService)
        {
            _storageService = storageService;
        }

        protected abstract string GetAnalyzerAssemblyPath(AnalyzerFileReference reference);
        protected abstract AnalyzerReference GetAnalyzerReference(string displayPath, string assemblyPath);

        public Checksum CreateChecksum(MetadataReference reference, CancellationToken cancellationToken)
        {
            var portable = reference as PortableExecutableReference;
            if (portable != null)
            {
                return CreatePortableExecutableReferenceChecksum(portable, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public Checksum CreateChecksum(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
            {
                WriteTo(reference, writer, cancellationToken);

                stream.Position = 0;
                return Checksum.Create(stream);
            }
        }

        public void WriteTo(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            var portable = reference as PortableExecutableReference;
            if (portable != null)
            {
                var supportTemporaryStorage = portable as ISupportTemporaryStorage;
                if (supportTemporaryStorage != null)
                {
                    if (TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(supportTemporaryStorage, writer, cancellationToken))
                    {
                        return;
                    }
                }

                WritePortableExecutableReferenceTo(portable, writer, cancellationToken);
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var type = reader.ReadString();
            if (type == nameof(PortableExecutableReference))
            {
                return ReadPortableExecutableReferenceFrom(reader, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        public void WriteTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = reference as AnalyzerFileReference;
            if (file != null)
            {
                // fail to load analyzer assembly
                var assemblyPath = TryGetAnalyzerAssemblyPath(file);
                if (assemblyPath == null)
                {
                    WriteUnresolvedAnalyzerReferenceTo(reference, writer);
                    return;
                }

                writer.WriteString(nameof(AnalyzerFileReference));
                writer.WriteInt32((int)SerializationKinds.FilePath);

                writer.WriteString(file.FullPath);

                // TODO: remove this kind of host specific knowledge from common layer.
                //       but think moving it to host layer where this implementation detail actually exist.
                //
                // analyzer assembly path to load analyzer acts like
                // snapshot version for analyzer (since it is based on shadow copy)
                // we can't send over bits and load analyer from memory (image) due to CLR not being able
                // to find satellite dlls for analyzers.
                writer.WriteString(assemblyPath);
                return;
            }

            var unresolved = reference as UnresolvedAnalyzerReference;
            if (unresolved != null)
            {
                WriteUnresolvedAnalyzerReferenceTo(reference, writer);
                return;
            }

            var image = reference as AnalyzerImageReference;
            if (image != null)
            {
                // TODO: think a way to support this or a way to deal with this kind of situation.
                throw new NotSupportedException(nameof(AnalyzerImageReference));
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = reader.ReadString();
            if (type == nameof(AnalyzerFileReference))
            {
                var kind = (SerializationKinds)reader.ReadInt32();
                Contract.ThrowIfFalse(kind == SerializationKinds.FilePath);

                // display path
                var displayPath = reader.ReadString();
                var assemblyPath = reader.ReadString();

                return GetAnalyzerReference(displayPath, assemblyPath);
            }

            if (type == nameof(UnresolvedAnalyzerReference))
            {
                var fullPath = reader.ReadString();
                return new UnresolvedAnalyzerReference(fullPath);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        protected void WritePortableExecutableReferenceHeaderTo(
            PortableExecutableReference reference, SerializationKinds kind, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(nameof(PortableExecutableReference));
            writer.WriteInt32((int)kind);

            WritePortableExecutableReferencePropertiesTo(reference, writer, cancellationToken);
        }

        private void WritePortableExecutableReferencePropertiesTo(PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WriteTo(reference.Properties, writer, cancellationToken);
            writer.WriteString(reference.FilePath);
        }

        private Checksum CreatePortableExecutableReferenceChecksum(PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
            {
                WritePortableExecutableReferencePropertiesTo(reference, writer, cancellationToken);
                WriteMvidsTo(TryGetMetadata(reference), writer, cancellationToken);

                stream.Position = 0;
                return Checksum.Create(stream);
            }
        }

        private void WriteMvidsTo(Metadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (metadata == null)
            {
                // handle error case where we couldn't load metadata of the reference.
                // this basically won't write anything to writer
                return;
            }

            var assemblyMetadata = metadata as AssemblyMetadata;
            if (assemblyMetadata != null)
            {
                writer.WriteInt32((int)assemblyMetadata.Kind);

                var modules = assemblyMetadata.GetModules();
                writer.WriteInt32(modules.Length);

                foreach (var module in modules)
                {
                    WriteMvidTo(module, writer, cancellationToken);
                }

                return;
            }

            WriteMvidTo((ModuleMetadata)metadata, writer, cancellationToken);
        }

        private void WriteMvidTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)metadata.Kind);

            var metadataReader = metadata.GetMetadataReader();

            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            var guid = metadataReader.GetGuid(mvidHandle);

            writer.WriteValue(guid.ToByteArray());
        }

        private void WritePortableExecutableReferenceTo(
            PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WritePortableExecutableReferenceHeaderTo(reference, SerializationKinds.Bits, writer, cancellationToken);

            WriteTo(TryGetMetadata(reference), writer, cancellationToken);

            // TODO: what I should do with documentation provider? it is not exposed outside
        }

        private PortableExecutableReference ReadPortableExecutableReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = (SerializationKinds)reader.ReadInt32();
            if (kind == SerializationKinds.Bits || kind == SerializationKinds.MemoryMapFile)
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

                // TODO: deal with xml document provider properly
                //       should we shadow copy xml doc comment?
                return new SerializedMetadataReference(properties, filePath, tuple.Value.Item1, tuple.Value.Item2, XmlDocumentationProvider.Default);
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private void WriteTo(MetadataReferenceProperties properties, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)properties.Kind);
            writer.WriteValue(properties.Aliases.ToArray());
            writer.WriteBoolean(properties.EmbedInteropTypes);
        }

        private MetadataReferenceProperties ReadMetadataReferencePropertiesFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = (MetadataImageKind)reader.ReadInt32();
            var aliases = reader.ReadArray<string>().ToImmutableArrayOrEmpty();
            var embedInteropTypes = reader.ReadBoolean();

            return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
        }

        private void WriteTo(Metadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (metadata == null)
            {
                // handle error case where metadata failed to load
                writer.WriteInt32(MetadataFailed);
                return;
            }

            var assemblyMetadata = metadata as AssemblyMetadata;
            if (assemblyMetadata != null)
            {
                writer.WriteInt32((int)assemblyMetadata.Kind);

                var modules = assemblyMetadata.GetModules();
                writer.WriteInt32(modules.Length);

                foreach (var module in modules)
                {
                    WriteTo(module, writer, cancellationToken);
                }

                return;
            }

            WriteTo((ModuleMetadata)metadata, writer, cancellationToken);
        }

        private bool TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(
            ISupportTemporaryStorage reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            var storages = reference.GetStorages();
            if (storages == null)
            {
                return false;
            }

            using (var pooled = Creator.CreateList<ValueTuple<string, long>>())
            {
                foreach (var storage in storages)
                {
                    var storage2 = storage as ITemporaryStorageWithName;
                    if (storage2 == null)
                    {
                        return false;
                    }

                    pooled.Object.Add(ValueTuple.Create(storage2.Name, storage2.Size));
                }

                WritePortableExecutableReferenceHeaderTo((PortableExecutableReference)reference, SerializationKinds.MemoryMapFile, writer, cancellationToken);

                writer.WriteInt32((int)MetadataImageKind.Assembly);
                writer.WriteInt32(pooled.Object.Count);

                foreach (var tuple in pooled.Object)
                {
                    writer.WriteInt32((int)MetadataImageKind.Module);
                    writer.WriteString(tuple.Item1);
                    writer.WriteInt64(tuple.Item2);
                }

                return true;
            }
        }

        private ValueTuple<Metadata, ImmutableArray<ITemporaryStreamStorage>>? TryReadMetadataFrom(
            ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            var imageKind = reader.ReadInt32();
            if (imageKind == MetadataFailed)
            {
                // error case
                return null;
            }

            var metadataKind = (MetadataImageKind)imageKind;
            if (metadataKind == MetadataImageKind.Assembly)
            {
                using (var pooledMetadata = Creator.CreateList<ModuleMetadata>())
                using (var pooledStorage = Creator.CreateList<ITemporaryStreamStorage>())
                {
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        metadataKind = (MetadataImageKind)reader.ReadInt32();
                        Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

                        var tuple = ReadModuleMetadataFrom(reader, kind, cancellationToken);

                        pooledMetadata.Object.Add(tuple.Item1);
                        pooledStorage.Object.Add(tuple.Item2);
                    }

                    return ValueTuple.Create<Metadata, ImmutableArray<ITemporaryStreamStorage>>(AssemblyMetadata.Create(pooledMetadata.Object), pooledStorage.Object.ToImmutableArrayOrEmpty());
                }
            }

            Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

            var moduleInfo = ReadModuleMetadataFrom(reader, kind, cancellationToken);
            return ValueTuple.Create<Metadata, ImmutableArray<ITemporaryStreamStorage>>(moduleInfo.Item1, ImmutableArray.Create(moduleInfo.Item2));
        }

        private ValueTuple<ModuleMetadata, ITemporaryStreamStorage> ReadModuleMetadataFrom(
            ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ITemporaryStreamStorage storage;
            long length;
            GetTemporaryStorage(reader, kind, out storage, out length, cancellationToken);

            var storageStream = storage.ReadStream(cancellationToken);
            Contract.ThrowIfFalse(length == storageStream.Length);

            ModuleMetadata metadata;
            object lifeTimeObject;

            GetMetadata(storageStream, length, out metadata, out lifeTimeObject);

            // make sure we keep storageStream alive while Metadata is alive
            // we use conditional weak table since we can't control metadata liftetime
            s_lifetimeMap.Add(metadata, lifeTimeObject);

            return ValueTuple.Create(metadata, storage);
        }

        private void GetTemporaryStorage(
            ObjectReader reader, SerializationKinds kind, out ITemporaryStreamStorage storage, out long length, CancellationToken cancellationToken)
        {
            if (kind == SerializationKinds.Bits)
            {
                storage = _storageService.CreateTemporaryStreamStorage(cancellationToken);
                using (var stream = SerializableBytes.CreateWritableStream())
                {
                    CopyByteArrayToStream(reader, stream, cancellationToken);

                    length = stream.Length;

                    stream.Position = 0;
                    storage.WriteStream(stream, cancellationToken);
                }

                return;
            }

            if (kind == SerializationKinds.MemoryMapFile)
            {
                var service2 = _storageService as ITemporaryStorageService2;
                Contract.ThrowIfNull(service2);

                var name = reader.ReadString();
                var size = reader.ReadInt64();

                storage = service2.AttachTemporaryStreamStorage(name, size, cancellationToken);
                length = size;

                return;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private void GetMetadata(Stream stream, long length, out ModuleMetadata metadata, out object lifeTimeObject)
        {
            var directAccess = stream as ISupportDirectMemoryAccess;
            if (directAccess != null)
            {
                metadata = ModuleMetadata.CreateFromMetadata(directAccess.GetPointer(), (int)length);
                lifeTimeObject = stream;
                return;
            }

            PinnedObject pinnedObject;
            ArraySegment<byte> buffer;
            var memory = stream as MemoryStream;
            if (memory != null &&
                memory.TryGetBuffer(out buffer) &&
                buffer.Offset == 0)
            {
                pinnedObject = new PinnedObject(buffer.Array, buffer.Count);
            }
            else
            {
                var array = new byte[length];
                stream.Read(array, 0, (int)length);
                pinnedObject = new PinnedObject(array, length);
            }

            metadata = ModuleMetadata.CreateFromMetadata(pinnedObject.GetPointer(), (int)length);
            lifeTimeObject = pinnedObject;
        }

        private void CopyByteArrayToStream(ObjectReader reader, Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: make reader be able to read byte[] chunk
            var content = reader.ReadArray<byte>();
            stream.Write(content, 0, content.Length);
        }

        private void WriteTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteInt32((int)metadata.Kind);

            WriteTo(metadata.GetMetadataReader(), writer, cancellationToken);
        }

        private unsafe void WriteTo(MetadataReader reader, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = reader.MetadataLength;

            // TODO: any way to avoid allocating byte array here?
            var bytes = new byte[length];
            Marshal.Copy((IntPtr)reader.MetadataPointer, bytes, 0, length);

            writer.WriteValue(bytes);
        }

        private static void WriteUnresolvedAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer)
        {
            writer.WriteString(nameof(UnresolvedAnalyzerReference));
            writer.WriteString(reference.FullPath);
        }

        private static Metadata TryGetMetadata(PortableExecutableReference reference)
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

        private string TryGetAnalyzerAssemblyPath(AnalyzerFileReference file)
        {
            try
            {
                return GetAnalyzerAssemblyPath(file);
            }
            catch
            {
                // we can't load the assembly analyzer file reference is pointing to.
                // rather than crashing, handle it gracefully
                return null;
            }
        }

        private sealed class PinnedObject : IDisposable
        {
            private readonly GCHandle _gcHandle;

            public PinnedObject(byte[] array, long length)
            {
                _gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            }

            internal IntPtr GetPointer()
            {
                return _gcHandle.AddrOfPinnedObject();
            }

            private void OnDispose()
            {
                if (_gcHandle.IsAllocated)
                {
                    _gcHandle.Free();
                }
            }

            ~PinnedObject()
            {
                OnDispose();
            }

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
                MetadataReferenceProperties properties, string fullPath, DocumentationProvider initialDocumentation) :
                base(properties, fullPath, initialDocumentation)
            {
                // TODO: doc comment provider is a bit wierd.
                _provider = initialDocumentation;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO: properly implement this
                return null;
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
            {
                return new MissingMetadataReference(properties, FilePath, _provider);
            }
        }

        private sealed class SerializedMetadataReference : PortableExecutableReference, ISupportTemporaryStorage
        {
            private readonly Metadata _metadata;
            private readonly ImmutableArray<ITemporaryStreamStorage> _storagesOpt;
            private readonly DocumentationProvider _provider;

            public SerializedMetadataReference(
                MetadataReferenceProperties properties, string fullPath,
                Metadata metadata, ImmutableArray<ITemporaryStreamStorage> storagesOpt, DocumentationProvider initialDocumentation) :
                base(properties, fullPath, initialDocumentation)
            {
                // TODO: doc comment provider is a bit wierd.
                _metadata = metadata;
                _storagesOpt = storagesOpt;

                _provider = initialDocumentation;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO: properly implement this
                return null;
            }

            protected override Metadata GetMetadataImpl()
            {
                return _metadata;
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return new SerializedMetadataReference(properties, FilePath, _metadata, _storagesOpt, _provider);
            }

            public IEnumerable<ITemporaryStreamStorage> GetStorages()
            {
                return _storagesOpt;
            }
        }
    }
}