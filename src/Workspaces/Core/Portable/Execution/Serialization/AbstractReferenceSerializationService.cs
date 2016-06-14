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

namespace Microsoft.CodeAnalysis.Execution.Serialization
{
    internal abstract class AbstractReferenceSerializationService : IReferenceSerializationService
    {
        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new ConditionalWeakTable<Metadata, object>();

        private readonly ITemporaryStorageService _service;

        protected AbstractReferenceSerializationService(ITemporaryStorageService service)
        {
            _service = service;
        }

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
            throw new NotImplementedException();
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

        public abstract void WriteTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken);
        public abstract AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken);

        protected void WritePortableExecutableReferenceHeaderTo(PortableExecutableReference reference, SerializationKinds kind, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(nameof(PortableExecutableReference));
            writer.WriteInt32((int)kind);

            WriteTo(reference.Properties, writer, cancellationToken);
            writer.WriteString(reference.FilePath);
        }

        private Checksum CreatePortableExecutableReferenceChecksum(PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
            {
                WritePortableExecutableReferenceTo(reference, writer, cancellationToken);

                stream.Position = 0;
                return Checksum.Create(stream);
            }
        }

        private void WritePortableExecutableReferenceTo(PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            WritePortableExecutableReferenceHeaderTo(reference, SerializationKinds.Bits, writer, cancellationToken);
            WritePortableExecutableReferenceMetadataTo(reference, writer, cancellationToken);

            // TODO: what I should do with documentation provider? it is not exposed outside
        }

        private void WritePortableExecutableReferenceMetadataTo(PortableExecutableReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            var metadata = GetMetadata(reference);
            WriteTo(metadata, writer, cancellationToken);
        }

        private PortableExecutableReference ReadPortableExecutableReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = (SerializationKinds)reader.ReadInt32();
            if (kind == SerializationKinds.Bits || kind == SerializationKinds.MemoryMapFile)
            {
                var properties = ReadMetadataReferencePropertiesFrom(reader, cancellationToken);

                // TODO: find a way to deal with documentation
                var filePath = reader.ReadString();

                var tuple = ReadMetadataFrom(reader, kind, cancellationToken);

                // TODO: deal with xml document provider properly
                return new SerializedMetadataReference(properties, filePath, tuple.Item1, tuple.Item2, XmlDocumentationProvider.Default);
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private void WriteTo(MetadataReferenceProperties properties, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteInt32((int)properties.Kind);
            writer.WriteArray(properties.Aliases.ToArray());
            writer.WriteBoolean(properties.EmbedInteropTypes);
        }

        private MetadataReferenceProperties ReadMetadataReferencePropertiesFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = (MetadataImageKind)reader.ReadInt32();
            var aliases = reader.ReadArray<string>().ToImmutableArrayOrEmpty();
            var embedInteropTypes = reader.ReadBoolean();

            return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
        }

        private void WriteTo(Metadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
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

        private bool TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(ISupportTemporaryStorage reference, ObjectWriter writer, CancellationToken cancellationToken)
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
                    var storage2 = storage as ITemporaryStreamStorage2;
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

        private ValueTuple<Metadata, ImmutableArray<ITemporaryStreamStorage>> ReadMetadataFrom(ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            var metadataKind = (MetadataImageKind)reader.ReadInt32();
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

        private ValueTuple<ModuleMetadata, ITemporaryStreamStorage> ReadModuleMetadataFrom(ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
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

        private void GetTemporaryStorage(ObjectReader reader, SerializationKinds kind, out ITemporaryStreamStorage storage, out long length, CancellationToken cancellationToken)
        {
            if (kind == SerializationKinds.Bits)
            {
                storage = _service.CreateTemporaryStreamStorage(cancellationToken);
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
                var service2 = _service as ITemporaryStorageService2;
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
            var memory = stream as MemoryStream;
            if (memory != null && PortableShim.MemoryStream.GetBuffer != null)
            {
                pinnedObject = new PinnedObject((byte[])PortableShim.MemoryStream.GetBuffer.Invoke(memory, null), length);
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

            var length = reader.ReadInt32();

            // TODO: array type. Assert that it is byte array!
            var unused = reader.ReadInt32();

            for (var i = 0; i < length; i++)
            {
                stream.WriteByte((byte)reader.ReadValue());
            }
        }

        private void WriteTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteInt32((int)metadata.Kind);

            var metadataReader = GetMetadataReader(metadata);
            WriteTo(metadataReader, writer, cancellationToken);
        }

        private void WriteTo(MetadataReader reader, ObjectWriter writer, CancellationToken cancellationToken)
        {
            var blockFieldInfo = reader.GetType().GetTypeInfo().GetDeclaredField("Block");
            var block = blockFieldInfo.GetValue(reader);

            // once things become public API, change it to copy stream over byte* and length from metadata reader
            var toArrayFieldInfo = block.GetType().GetTypeInfo().GetDeclaredMethod("ToArray");
            var array = (byte[])toArrayFieldInfo.Invoke(block, null);

            writer.WriteArray(array);
        }

        private MetadataReader GetMetadataReader(ModuleMetadata metadata)
        {
            // TODO: right now, use reflection, but this API will be added as public API soon. when that happen, remove reflection
            var metadataReaderPropertyInfo = metadata.GetType().GetTypeInfo().GetDeclaredProperty("MetadataReader");
            var metadataReader = (MetadataReader)metadataReaderPropertyInfo.GetValue(metadata);
            return metadataReader;
        }

        private Metadata GetMetadata(PortableExecutableReference reference)
        {
            // TODO: right now, use reflection, but this API will be added as public API soon. when that happen, remove reflection
            var methodInfo = reference.GetType().GetTypeInfo().GetDeclaredMethod("GetMetadataImpl");
            return (Metadata)methodInfo.Invoke(reference, null);
        }

        private sealed class PinnedObject : IDisposable
        {
            private readonly GCHandle _gcHandle;
            private readonly long _length;

            public PinnedObject(byte[] array, long length)
            {
                _gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                _length = length;
            }

            internal IntPtr GetPointer()
            {
                return _gcHandle.AddrOfPinnedObject();
            }

            private void Dispose(bool disposing)
            {
                if (_gcHandle.IsAllocated)
                {
                    _gcHandle.Free();
                }
            }

            ~PinnedObject()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
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