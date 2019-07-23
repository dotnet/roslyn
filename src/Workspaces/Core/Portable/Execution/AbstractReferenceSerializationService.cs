// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal abstract class AbstractReferenceSerializationService : IReferenceSerializationService
    {
        private const int MetadataFailed = int.MaxValue;
        private const string VisualStudioUnresolvedAnalyzerReference = "Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.VisualStudioAnalyzer+VisualStudioUnresolvedAnalyzerReference";

        protected const byte NoEncodingSerialization = 0;
        protected const byte EncodingSerialization = 1;

        private static readonly ConditionalWeakTable<Metadata, object> s_lifetimeMap = new ConditionalWeakTable<Metadata, object>();

        private readonly ITemporaryStorageService _storageService;
        private readonly IDocumentationProviderService _documentationService;

        protected AbstractReferenceSerializationService(
            ITemporaryStorageService storageService,
            IDocumentationProviderService documentationService)
        {
            _storageService = storageService;
            _documentationService = documentationService;
        }

        public virtual void WriteTo(Encoding encoding, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteByte(NoEncodingSerialization);
            writer.WriteString(encoding?.WebName);
        }

        public virtual Encoding ReadEncodingFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var serialized = reader.ReadByte();

            // portable layer doesn't support serialization
            Contract.ThrowIfFalse(serialized == NoEncodingSerialization);
            return ReadEncodingFrom(serialized, reader, cancellationToken);
        }

        protected Encoding ReadEncodingFrom(byte serialized, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (serialized != NoEncodingSerialization)
            {
                return null;
            }

            var webName = reader.ReadString();
            return webName == null ? null : Encoding.GetEncoding(webName);
        }

        protected abstract string GetAnalyzerAssemblyPath(AnalyzerFileReference reference);
        protected abstract AnalyzerReference GetAnalyzerReference(string displayPath, string assemblyPath);

        public Checksum CreateChecksum(MetadataReference reference, CancellationToken cancellationToken)
        {
            if (reference is PortableExecutableReference portable)
            {
                return CreatePortableExecutableReferenceChecksum(portable, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public Checksum CreateChecksum(AnalyzerReference reference, bool usePathFromAssembly, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);

            switch (reference)
            {
                case AnalyzerFileReference file:
                    WriteAnalyzerFileReferenceMvid(file, writer, usePathFromAssembly, cancellationToken);
                    break;

                case UnresolvedAnalyzerReference unresolved:
                    WriteUnresolvedAnalyzerReferenceTo(unresolved, writer);
                    break;

                case AnalyzerReference analyzerReference when analyzerReference.GetType().FullName == VisualStudioUnresolvedAnalyzerReference:
                    WriteUnresolvedAnalyzerReferenceTo(analyzerReference, writer);
                    break;

                case AnalyzerImageReference _:
                    // TODO: think a way to support this or a way to deal with this kind of situation.
                    // https://github.com/dotnet/roslyn/issues/15783
                    throw new NotSupportedException(nameof(AnalyzerImageReference));

                default:
                    throw ExceptionUtilities.UnexpectedValue(reference);
            }

            stream.Position = 0;
            return Checksum.Create(stream);
        }

        public void WriteTo(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (reference is PortableExecutableReference portable)
            {
                if (portable is ISupportTemporaryStorage supportTemporaryStorage)
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

        public void WriteTo(AnalyzerReference reference, ObjectWriter writer, bool usePathFromAssembly, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reference)
            {
                case AnalyzerFileReference file:
                    {
                        // fail to load analyzer assembly
                        var assemblyPath = usePathFromAssembly ? TryGetAnalyzerAssemblyPath(file) : file.FullPath;
                        if (assemblyPath == null)
                        {
                            WriteUnresolvedAnalyzerReferenceTo(reference, writer);
                            return;
                        }

                        writer.WriteString(nameof(AnalyzerFileReference));
                        writer.WriteInt32((int)SerializationKinds.FilePath);

                        // TODO: remove this kind of host specific knowledge from common layer.
                        //       but think moving it to host layer where this implementation detail actually exist.
                        //
                        // analyzer assembly path to load analyzer acts like
                        // snapshot version for analyzer (since it is based on shadow copy)
                        // we can't send over bits and load analyzer from memory (image) due to CLR not being able
                        // to find satellite dlls for analyzers.
                        writer.WriteString(file.FullPath);
                        writer.WriteString(assemblyPath);
                        return;
                    }

                case UnresolvedAnalyzerReference unresolved:
                    {
                        WriteUnresolvedAnalyzerReferenceTo(unresolved, writer);
                        return;
                    }

                case AnalyzerReference analyzerReference when analyzerReference.GetType().FullName == VisualStudioUnresolvedAnalyzerReference:
                    {
                        WriteUnresolvedAnalyzerReferenceTo(analyzerReference, writer);
                        return;
                    }

                case AnalyzerImageReference _:
                    {
                        // TODO: think a way to support this or a way to deal with this kind of situation.
                        // https://github.com/dotnet/roslyn/issues/15783
                        throw new NotSupportedException(nameof(AnalyzerImageReference));
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(reference);
            }
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

        private void WriteAnalyzerFileReferenceMvid(
            AnalyzerFileReference file, ObjectWriter writer, bool usePathFromAssembly, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // use actual assembly path rather than one returned from reference.FullPath if asked (usePathFromAssembly)
                // 2 can be different if analyzer loader used for the reference do something like shadow copying
                // otherwise, use reference.FullPath. we use usePathFromAssembly == false for vsix installed analyzer dlls
                // to make sure we don't load them up front and they don't get shadow copied.
                // TryGetAnalyzerAssemblyPath will load the given assembly to find out actual location where CLR
                // picked up the dll
                var assemblyPath = usePathFromAssembly ? TryGetAnalyzerAssemblyPath(file) : file.FullPath;

                using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                using var peReader = new PEReader(stream);

                var metadataReader = peReader.GetMetadataReader();

                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                var guid = metadataReader.GetGuid(mvidHandle);

                writer.WriteGuid(guid);
            }
            catch
            {
                // we can't load the assembly analyzer file reference is pointing to.
                // rather than crashing, handle it gracefully
                WriteUnresolvedAnalyzerReferenceTo(file, writer);
            }
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
            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);

            WritePortableExecutableReferencePropertiesTo(reference, writer, cancellationToken);
            WriteMvidsTo(TryGetMetadata(reference), writer, cancellationToken);

            stream.Position = 0;
            return Checksum.Create(stream);
        }

        private void WriteMvidsTo(Metadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
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
            catch (Exception ex) when (ex is BadImageFormatException ||
                                       ex is IOException ||
                                       ex is ObjectDisposedException)
            {
                modules = default;
                return false;
            }
        }

        private void WriteMvidTo(ModuleMetadata metadata, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)metadata.Kind);

            var metadataReader = metadata.GetMetadataReader();

            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            var guid = metadataReader.GetGuid(mvidHandle);

            writer.WriteGuid(guid);
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

        private bool TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(
            ISupportTemporaryStorage reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (_storageService == null)
            {
                return false;
            }

            var storages = reference.GetStorages();
            if (storages == null)
            {
                return false;
            }

            using var pooled = Creator.CreateList<(string name, long offset, long size)>();

            foreach (var storage in storages)
            {
                if (!(storage is ITemporaryStorageWithName storage2))
                {
                    return false;
                }

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

        private (Metadata metadata, ImmutableArray<ITemporaryStreamStorage> storages)? TryReadMetadataFrom(
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

                        pooledMetadata.Object.Add(ReadModuleMetadataFrom(reader, kind));
                    }

                    return (AssemblyMetadata.Create(pooledMetadata.Object), storages: default);
                }

                Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);
                return (ReadModuleMetadataFrom(reader, kind), storages: default);
            }

            if (metadataKind == MetadataImageKind.Assembly)
            {
                using var pooledMetadata = Creator.CreateList<ModuleMetadata>();
                using var pooledStorage = Creator.CreateList<ITemporaryStreamStorage>();

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

        private (ModuleMetadata metadata, ITemporaryStreamStorage storage) ReadModuleMetadataFrom(
            ObjectReader reader, SerializationKinds kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetTemporaryStorage(reader, kind, out var storage, out var length, cancellationToken);

            var storageStream = storage.ReadStream(cancellationToken);
            Contract.ThrowIfFalse(length == storageStream.Length);

            GetMetadata(storageStream, length, out var metadata, out var lifeTimeObject);

            // make sure we keep storageStream alive while Metadata is alive
            // we use conditional weak table since we can't control metadata liftetime
            s_lifetimeMap.Add(metadata, lifeTimeObject);

            return (metadata, storage);
        }

        private static ModuleMetadata ReadModuleMetadataFrom(ObjectReader reader, SerializationKinds kind)
        {
            Contract.ThrowIfFalse(SerializationKinds.Bits == kind);

            var array = reader.ReadArray<byte>();
            var pinnedObject = new PinnedObject(array, array.Length);

            var metadata = ModuleMetadata.CreateFromMetadata(pinnedObject.GetPointer(), array.Length);

            // make sure we keep storageStream alive while Metadata is alive
            // we use conditional weak table since we can't control metadata liftetime
            s_lifetimeMap.Add(metadata, pinnedObject);

            return metadata;
        }

        private void GetTemporaryStorage(
            ObjectReader reader, SerializationKinds kind, out ITemporaryStreamStorage storage, out long length, CancellationToken cancellationToken)
        {
            if (kind == SerializationKinds.Bits)
            {
                storage = _storageService.CreateTemporaryStreamStorage(cancellationToken);
                using var stream = SerializableBytes.CreateWritableStream();

                CopyByteArrayToStream(reader, stream, cancellationToken);

                length = stream.Length;

                stream.Position = 0;
                storage.WriteStream(stream, cancellationToken);

                return;
            }

            if (kind == SerializationKinds.MemoryMapFile)
            {
                var service2 = _storageService as ITemporaryStorageService2;
                Contract.ThrowIfNull(service2);

                var name = reader.ReadString();
                var offset = reader.ReadInt64();
                var size = reader.ReadInt64();

                storage = service2.AttachTemporaryStreamStorage(name, offset, size, cancellationToken);
                length = size;

                return;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private void GetMetadata(Stream stream, long length, out ModuleMetadata metadata, out object lifeTimeObject)
        {
            if (stream is ISupportDirectMemoryAccess directAccess)
            {
                metadata = ModuleMetadata.CreateFromMetadata(directAccess.GetPointer(), (int)length);
                lifeTimeObject = stream;
                return;
            }

            PinnedObject pinnedObject;
            if (stream is MemoryStream memory &&
                memory.TryGetBuffer(out var buffer) &&
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
                MetadataReferenceProperties properties, string fullPath, DocumentationProvider initialDocumentation)
                : base(properties, fullPath, initialDocumentation)
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

        [DebuggerDisplay("{" + nameof(Display) + ",nq}")]
        private sealed class SerializedMetadataReference : PortableExecutableReference, ISupportTemporaryStorage
        {
            private readonly Metadata _metadata;
            private readonly ImmutableArray<ITemporaryStreamStorage> _storagesOpt;
            private readonly DocumentationProvider _provider;

            public SerializedMetadataReference(
                MetadataReferenceProperties properties, string fullPath,
                Metadata metadata, ImmutableArray<ITemporaryStreamStorage> storagesOpt, DocumentationProvider initialDocumentation)
                : base(properties, fullPath, initialDocumentation)
            {
                _metadata = metadata;
                _storagesOpt = storagesOpt;

                _provider = initialDocumentation;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // this uses documentation provider given at the constructor
                throw ExceptionUtilities.Unreachable;
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
                return _storagesOpt.IsDefault ? (IEnumerable<ITemporaryStreamStorage>)null : _storagesOpt;
            }
        }
    }
}
