// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

using static TemporaryStorageService;

internal partial class SerializerService
{
    private const int MetadataFailed = int.MaxValue;

    /// <summary>
    /// Allow analyzer tests to exercise the oop codepaths, even though they're referring to in-memory instances of
    /// DiagnosticAnalyzers.  In that case, we'll just share the in-memory instance of the analyzer across the OOP
    /// boundary (which still runs in proc in tests), but we will still exercise all codepaths that use the RemoteClient
    /// as well as exercising all codepaths that send data across the OOP boundary.  Effectively, this allows us to
    /// pretend that a <see cref="AnalyzerImageReference"/> is a <see cref="AnalyzerFileReference"/> during tests.
    /// </summary>
    private static readonly object s_analyzerImageReferenceMapGate = new();
    private static IBidirectionalMap<AnalyzerImageReference, Guid> s_analyzerImageReferenceMap = BidirectionalMap<AnalyzerImageReference, Guid>.Empty;

    private static bool TryGetAnalyzerImageReferenceGuid(AnalyzerImageReference imageReference, out Guid guid)
    {
        lock (s_analyzerImageReferenceMapGate)
            return s_analyzerImageReferenceMap.TryGetValue(imageReference, out guid);
    }

    private static bool TryGetAnalyzerImageReferenceFromGuid(Guid guid, [NotNullWhen(true)] out AnalyzerImageReference? imageReference)
    {
        lock (s_analyzerImageReferenceMapGate)
            return s_analyzerImageReferenceMap.TryGetKey(guid, out imageReference);
    }

    private static Checksum CreateChecksum(MetadataReference reference)
    {
        if (reference is PortableExecutableReference portable)
            return CreatePortableExecutableReferenceChecksum(portable);

        throw ExceptionUtilities.UnexpectedValue(reference.GetType());
    }

    protected virtual Checksum CreateChecksum(AnalyzerReference reference)
    {
#if NET
        // If we're in the oop side and we're being asked to produce our local checksum (so we can compare it to the
        // host checksum), then we want to just defer to the underlying analyzer reference of our isolated reference.
        // This underlying reference corresponds to the reference that the host has, and we do not want to make any
        // changes as long as they're both in agreement.
        if (reference is IsolatedAnalyzerFileReference { UnderlyingAnalyzerFileReference: var underlyingReference })
            reference = underlyingReference;
#endif

        using var stream = SerializableBytes.CreateWritableStream();

        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            switch (reference)
            {
                case AnalyzerFileReference fileReference:
                    writer.WriteString(fileReference.OriginalFullPath ?? fileReference.FullPath);
                    writer.WriteGuid(TryGetAnalyzerFileReferenceMvid(fileReference));
                    break;

                case AnalyzerImageReference analyzerImageReference:
                    Contract.ThrowIfFalse(TryGetAnalyzerImageReferenceGuid(analyzerImageReference, out var guid), "AnalyzerImageReferences are only supported during testing");
                    writer.WriteGuid(guid);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(reference);
            }
        }

        stream.Position = 0;
        return Checksum.Create(stream);
    }

    protected virtual void WriteMetadataReferenceTo(MetadataReference reference, ObjectWriter writer)
    {
        if (reference is PortableExecutableReference portable)
        {
            if (portable is ISupportTemporaryStorage { StorageHandles: { Count: > 0 } handles } &&
                TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(portable, handles, writer))
            {
                return;
            }

            WritePortableExecutableReferenceTo(portable, writer);
            return;
        }

        throw ExceptionUtilities.UnexpectedValue(reference.GetType());
    }

    protected virtual MetadataReference ReadMetadataReferenceFrom(ObjectReader reader)
    {
        var type = reader.ReadString();
        if (type == nameof(PortableExecutableReference))
            return ReadPortableExecutableReferenceFrom(reader);

        throw ExceptionUtilities.UnexpectedValue(type);
    }

    protected virtual void WriteAnalyzerReferenceTo(AnalyzerReference reference, ObjectWriter writer)
    {
        switch (reference)
        {
            case AnalyzerFileReference fileReference:
                writer.WriteString(nameof(AnalyzerFileReference));
                var location = TryGetAssemblyLocation(fileReference);
                var (fullPath, originalFullPath) = string.IsNullOrEmpty(location)
                    ? (fileReference.FullPath, fileReference.OriginalFullPath)
                    : (location, fileReference.FullPath);
                writer.WriteString(fullPath);
                writer.WriteString(originalFullPath);

                // Note: it is intentional that we are not writing the MVID of the analyzer file reference over in (even
                // though we mixed it into the checksum).  We don't actually need the data on the other side as it will
                // be read out from the file itself.  So the flow is as follows when an analyzer-file-reference changes:
                //
                // 1. Change to file happens on disk and is detected by the host, which will reload the reference within it.
                // 2. When producing the checksum for the project, this analyzer file reference will not be found in the
                //    ChecksumCache, causing it to be recomputed (in `Checksum CreateChecksum(AnalyzerReference
                //    reference, CancellationToken cancellationToken)`.
                // 3. The checksum will be computed based on the file path and the MVID of the file.
                // 4. This will now cause a diff between the host and OOP.
                // 5. When OOP syncs with the host, it will create a fresh AnalyzerFileReference pointing to the right
                //    path, and specifying it wants to use the shadow copy loader.  The workspace snapshot will be
                //    updated to use this new reference.  Note: this is guaranteed, as `SolutionCompilationState
                //    WithProjectAnalyzerReferences(...)` uses reference-equality to determine if the analyzer is
                //    different, always picking up the new instances.
                // 6. When we actually need to load analyzers/generators in OOP it will then defer to the
                //    ShadowCopyAnalyzerAssemblyLoader.  This loader will *itself* then use the MVID of the file
                //    reference at the requested path to shadow copy to a new location specific to that mvid, ensuring
                //    that its data can be cleanly loaded in isolation from any prior version.
                break;

            case AnalyzerImageReference analyzerImageReference:
                Contract.ThrowIfFalse(TryGetAnalyzerImageReferenceGuid(analyzerImageReference, out var guid), "AnalyzerImageReferences are only supported during testing");
                writer.WriteString(nameof(AnalyzerImageReference));
                writer.WriteGuid(guid);
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(reference);
        }
    }

    protected virtual AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader)
    {
        switch (reader.ReadString())
        {
            case nameof(AnalyzerFileReference):
                // Rehydrate the analyzer file reference with the simple shared shadow copy loader.  Note: we won't
                // actually use this instance we create.  Instead, the caller will use create an IsolatedAssemblyReferenceSet
                // from these to ensure that all the types can be safely loaded into their own ALC.
                var fullPath = reader.ReadRequiredString();
                var originalFullPath = reader.ReadString();
                return new AnalyzerFileReference(fullPath, _analyzerLoaderProvider.SharedDirectLoader)
                {
                    OriginalFullPath = originalFullPath,
                };

            case nameof(AnalyzerImageReference):
                var guid = reader.ReadGuid();
                Contract.ThrowIfFalse(TryGetAnalyzerImageReferenceFromGuid(guid, out var analyzerImageReference));
                return analyzerImageReference;

            case var type:
                throw ExceptionUtilities.UnexpectedValue(type);
        }
    }

    protected static void WritePortableExecutableReferenceHeaderTo(
        PortableExecutableReference reference, SerializationKinds kind, ObjectWriter writer)
    {
        writer.WriteString(nameof(PortableExecutableReference));
        writer.WriteInt32((int)kind);

        WritePortableExecutableReferencePropertiesTo(reference, writer);
    }

    private static void WritePortableExecutableReferencePropertiesTo(PortableExecutableReference reference, ObjectWriter writer)
    {
        WriteTo(reference.Properties, writer);
        writer.WriteString(reference.FilePath);
    }

    private static Checksum CreatePortableExecutableReferenceChecksum(PortableExecutableReference reference)
    {
        using var stream = SerializableBytes.CreateWritableStream();

        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            WritePortableExecutableReferencePropertiesTo(reference, writer);
            WriteMvidsTo(TryGetMetadata(reference), writer);
        }

        stream.Position = 0;
        return Checksum.Create(stream);
    }

    private static void WriteMvidsTo(Metadata? metadata, ObjectWriter writer)
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
                WriteMvidTo(module, writer);

            return;
        }

        WriteMvidTo((ModuleMetadata)metadata, writer);
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

    private static void WriteMvidTo(ModuleMetadata metadata, ObjectWriter writer)
    {
        writer.WriteInt32((int)metadata.Kind);
        writer.WriteGuid(GetMetadataGuid(metadata));
    }

    private static Guid GetMetadataGuid(ModuleMetadata metadata)
    {
        var metadataReader = metadata.GetMetadataReader();
        var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
        var guid = metadataReader.GetGuid(mvidHandle);
        return guid;
    }

    private static void WritePortableExecutableReferenceTo(
        PortableExecutableReference reference, ObjectWriter writer)
    {
        WritePortableExecutableReferenceHeaderTo(reference, SerializationKinds.Bits, writer);
        WriteTo(TryGetMetadata(reference), writer);

        // TODO: what I should do with documentation provider? it is not exposed outside
    }

    private PortableExecutableReference ReadPortableExecutableReferenceFrom(ObjectReader reader)
    {
        var kind = (SerializationKinds)reader.ReadInt32();
        Contract.ThrowIfFalse(kind is SerializationKinds.Bits or SerializationKinds.MemoryMapFile);

        var properties = ReadMetadataReferencePropertiesFrom(reader);

        var filePath = reader.ReadString();

        if (TryReadMetadataFrom(reader, kind) is not (var metadata, var storageHandles))
        {
            // TODO: deal with xml document provider properly
            //       should we shadow copy xml doc comment?

            // image doesn't exist
            return new MissingMetadataReference(properties, filePath, DocumentationProvider.Default);
        }

        // for now, we will use IDocumentationProviderService to get DocumentationProvider for metadata
        // references. if the service is not available, then use Default (NoOp) provider.
        // since xml doc comment is not part of solution snapshot, (like xml reference resolver or strong name
        // provider) this provider can also potentially provide content that is different than one in the host. 
        // an alternative approach of this is synching content of xml doc comment to remote host as well
        // so that we can put xml doc comment as part of snapshot. but until we believe that is necessary,
        // it will go with simpler approach
        var documentProvider = filePath != null && _documentationService != null ?
            _documentationService.GetDocumentationProvider(filePath) : DocumentationProvider.Default;

        return new SerializedPortableExecutableReference(
            properties, filePath, metadata, storageHandles, documentProvider);
    }

    private static void WriteTo(MetadataReferenceProperties properties, ObjectWriter writer)
    {
        writer.WriteInt32((int)properties.Kind);
        writer.WriteArray(properties.Aliases, static (w, a) => w.WriteString(a));
        writer.WriteBoolean(properties.EmbedInteropTypes);
    }

    private static MetadataReferenceProperties ReadMetadataReferencePropertiesFrom(ObjectReader reader)
    {
        var kind = (MetadataImageKind)reader.ReadInt32();
        var aliases = reader.ReadArray(static r => r.ReadRequiredString());
        var embedInteropTypes = reader.ReadBoolean();

        return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
    }

    private static void WriteTo(Metadata? metadata, ObjectWriter writer)
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
                WriteTo(module, writer);

            return;
        }

        WriteTo((ModuleMetadata)metadata, writer);
    }

    private static bool TryWritePortableExecutableReferenceBackedByTemporaryStorageTo(
        PortableExecutableReference reference,
        IReadOnlyList<ITemporaryStorageStreamHandle> handles,
        ObjectWriter writer)
    {
        Contract.ThrowIfTrue(handles.Count == 0);

        WritePortableExecutableReferenceHeaderTo(reference, SerializationKinds.MemoryMapFile, writer);

        writer.WriteInt32((int)MetadataImageKind.Assembly);
        writer.WriteInt32(handles.Count);

        foreach (var handle in handles)
        {
            writer.WriteInt32((int)MetadataImageKind.Module);
            handle.Identifier.WriteTo(writer);
        }

        return true;
    }

    private (Metadata metadata, ImmutableArray<TemporaryStorageStreamHandle> storageHandles)? TryReadMetadataFrom(
        ObjectReader reader, SerializationKinds kind)
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
            var count = reader.ReadInt32();

            var allMetadata = new FixedSizeArrayBuilder<ModuleMetadata>(count);
            var allHandles = new FixedSizeArrayBuilder<TemporaryStorageStreamHandle>(count);

            for (var i = 0; i < count; i++)
            {
                metadataKind = (MetadataImageKind)reader.ReadInt32();
                Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

                var (metadata, storageHandle) = ReadModuleMetadataFrom(reader, kind);

                allMetadata.Add(metadata);
                allHandles.Add(storageHandle);
            }

            return (AssemblyMetadata.Create(allMetadata.MoveToImmutable()), allHandles.MoveToImmutable());
        }
        else
        {
            Contract.ThrowIfFalse(metadataKind == MetadataImageKind.Module);

            var moduleInfo = ReadModuleMetadataFrom(reader, kind);
            return (moduleInfo.metadata, [moduleInfo.storageHandle]);
        }
    }

    private (ModuleMetadata metadata, TemporaryStorageStreamHandle storageHandle) ReadModuleMetadataFrom(
        ObjectReader reader, SerializationKinds kind)
    {
        Contract.ThrowIfFalse(kind is SerializationKinds.Bits or SerializationKinds.MemoryMapFile);

        return kind == SerializationKinds.Bits
            ? ReadModuleMetadataFromBits()
            : ReadModuleMetadataFromMemoryMappedFile();

        (ModuleMetadata metadata, TemporaryStorageStreamHandle storageHandle) ReadModuleMetadataFromMemoryMappedFile()
        {
            // Host passed us a segment of its own memory mapped file.  We can just refer to that segment directly as it
            // will not be released by the host.
            var storageIdentifier = TemporaryStorageIdentifier.ReadFrom(reader);
            var storageHandle = TemporaryStorageService.GetStreamHandle(storageIdentifier);
            return ReadModuleMetadataFromStorage(storageHandle);
        }

        (ModuleMetadata metadata, TemporaryStorageStreamHandle storageHandle) ReadModuleMetadataFromBits()
        {
            // Host is sending us all the data as bytes.  Take that and write that out to a memory mapped file on the
            // server side so that we can refer to this data uniformly.
            using var stream = SerializableBytes.CreateWritableStream();
            CopyByteArrayToStream(reader, stream);

            var length = stream.Length;
            var storageHandle = _storageService.Value.WriteToTemporaryStorage(stream);
            Contract.ThrowIfTrue(length != storageHandle.Identifier.Size);
            return ReadModuleMetadataFromStorage(storageHandle);
        }

        (ModuleMetadata metadata, TemporaryStorageStreamHandle storageHandle) ReadModuleMetadataFromStorage(
            TemporaryStorageStreamHandle storageHandle)
        {
            // Now read in the module data using that identifier.  This will either be reading from the host's memory if
            // they passed us the information about that memory segment.  Or it will be reading from our own memory if they
            // sent us the full contents.
            //
            // The ITemporaryStorageStreamHandle should have given us an UnmanagedMemoryStream
            // since this only runs on Windows for VS.
            var unmanagedStream = (UnmanagedMemoryStream)storageHandle.ReadFromTemporaryStorage();
            Contract.ThrowIfFalse(storageHandle.Identifier.Size == unmanagedStream.Length);

            // For an unmanaged memory stream, ModuleMetadata can take ownership directly.  Stream will be kept alive as
            // long as the ModuleMetadata is alive due to passing its .Dispose method in as the onDispose callback of
            // the metadata.
            unsafe
            {
                var metadata = ModuleMetadata.CreateFromMetadata(
                    (IntPtr)unmanagedStream.PositionPointer, (int)unmanagedStream.Length, unmanagedStream.Dispose);
                return (metadata, storageHandle);
            }
        }
    }

    private static void CopyByteArrayToStream(ObjectReader reader, Stream stream)
    {
        // TODO: make reader be able to read byte[] chunk
        var content = reader.ReadByteArray();
        stream.Write(content, 0, content.Length);
    }

    private static void WriteTo(ModuleMetadata metadata, ObjectWriter writer)
    {
        writer.WriteInt32((int)metadata.Kind);
        WriteTo(metadata.GetMetadataReader(), writer);
    }

    private static unsafe void WriteTo(MetadataReader reader, ObjectWriter writer)
    {
        writer.WriteSpan(new ReadOnlySpan<byte>(reader.MetadataPointer, reader.MetadataLength));
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
            // We have a reference but the file the reference is pointing to might not actually exist on disk. In that
            // case, rather than crashing, we will handle it gracefully.
            return null;
        }
    }

    private static Guid TryGetAnalyzerFileReferenceMvid(AnalyzerFileReference file)
    {
        try
        {
            return AssemblyUtilities.ReadMvid(file.OriginalFullPath ?? file.FullPath);
        }
        catch
        {
            // We have a reference but the file the reference is pointing to might not actually exist on disk. In that
            // case, rather than crashing, we will handle it gracefully.
            return Guid.Empty;
        }
    }

    private static string? TryGetAssemblyLocation(AnalyzerFileReference file)
    {
        try
        {
            return file.GetAssembly().Location;
        }
        catch
        {
            return null;
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

    public static class TestAccessor
    {
        public static void AddAnalyzerImageReference(AnalyzerImageReference analyzerImageReference)
        {
            lock (s_analyzerImageReferenceMapGate)
            {
                if (!s_analyzerImageReferenceMap.ContainsKey(analyzerImageReference))
                    s_analyzerImageReferenceMap = s_analyzerImageReferenceMap.Add(analyzerImageReference, Guid.NewGuid());
            }
        }
    }
}
