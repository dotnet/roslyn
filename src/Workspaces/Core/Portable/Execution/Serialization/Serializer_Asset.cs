// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// serialize and deserialize objects to stream.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// </summary>
    internal partial class Serializer
    {
        public void Serialize(SolutionSnapshotInfo info, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Serialize(info.Id, writer, cancellationToken);

            // TODO: figure out a way to send version info over as well.
            //       right now, version get updated automatically, so 2 can't be exactly match
            // info.Version.WriteTo(writer);
            writer.WriteString(info.FilePath);
        }

        private SolutionSnapshotInfo DeserializeSolutionSnapshotInfo(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var solutionId = DeserializeSolutionId(reader, cancellationToken);
            // var version = VersionStamp.ReadFrom(reader);
            var filePath = reader.ReadString();

            return new SolutionSnapshotInfo(solutionId, VersionStamp.Create(), filePath);
        }

        public void Serialize(ProjectSnapshotInfo info, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Serialize(info.Id, writer, cancellationToken);

            // TODO: figure out a way to send version info over as well
            // info.Version.WriteTo(writer);

            writer.WriteString(info.Name);
            writer.WriteString(info.AssemblyName);
            writer.WriteString(info.Language);
            writer.WriteString(info.FilePath);
            writer.WriteString(info.OutputFilePath);
        }

        private ProjectSnapshotInfo DeserializeProjectSnapshotInfo(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectId = DeserializeProjectId(reader, cancellationToken);

            // var version = VersionStamp.ReadFrom(reader);
            var name = reader.ReadString();
            var assemblyName = reader.ReadString();
            var language = reader.ReadString();
            var filePath = reader.ReadString();
            var outputFilePath = reader.ReadString();

            return new ProjectSnapshotInfo(projectId, VersionStamp.Create(), name, assemblyName, language, filePath, outputFilePath);
        }

        public void Serialize(DocumentSnapshotInfo info, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Serialize(info.Id, writer, cancellationToken);

            writer.WriteString(info.Name);
            writer.WriteArray(info.Folders.ToArray());
            writer.WriteInt32((int)info.SourceCodeKind);
            writer.WriteString(info.FilePath);
            writer.WriteBoolean(info.IsGenerated);
        }

        private DocumentSnapshotInfo DeserializeDocumentSnapshotInfo(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documentId = DeserializeDocumentId(reader, cancellationToken);

            var name = reader.ReadString();
            var folders = reader.ReadArray<string>();
            var sourceCodeKind = reader.ReadInt32();
            var filePath = reader.ReadString();
            var isGenerated = reader.ReadBoolean();

            return new DocumentSnapshotInfo(documentId, name, folders, (SourceCodeKind)sourceCodeKind, filePath, isGenerated);
        }

        public void Serialize(ITemporaryTextStorage2 storage, SourceText text, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32((int)text.ChecksumAlgorithm);
            writer.WriteString(text.Encoding?.WebName);

            // TODO: refactor this part in its own abstraction (Bits) that has multiple sub types
            //       rather than using enums
            if (storage != null && storage.Name != null)
            {
                writer.WriteInt32((int)SerializationKinds.MemoryMapFile);
                writer.WriteString(storage.Name);
                writer.WriteInt64(storage.Size);
                return;
            }

            writer.WriteInt32((int)SerializationKinds.Bits);
            writer.WriteString(text.ToString());
        }

        private SourceText DeserializeSourceText(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // REVIEW: why IDE services doesnt care about checksumAlgorithm?
            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var webName = reader.ReadString();
            var encoding = webName == null ? null : Encoding.GetEncoding(webName);

            var kind = (SerializationKinds)reader.ReadInt32();
            if (kind == SerializationKinds.MemoryMapFile)
            {
                var name = reader.ReadString();
                var size = reader.ReadInt64();

                var tempService = _workspaceServices.GetService<ITemporaryStorageService>() as ITemporaryStorageService2;
                var storage = tempService.AttachTemporaryTextStorage(name, size, encoding, cancellationToken);

                return storage.ReadText(cancellationToken);
            }

            // TODO: should include version info here as well?

            var textService = _workspaceServices.GetService<ITextFactoryService>();
            using (var textReader = new StringReader(reader.ReadString()))
            {
                return textService.CreateText(textReader, encoding, cancellationToken);
            }
        }

        public void Serialize(string language, CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: language specific, should I put this ability in compilation layer?
            writer.WriteString(language);

            var service = GetSerializationService(language);
            service.WriteTo(options, writer, cancellationToken);
        }

        private CompilationOptions DeserializeCompilationOptions(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = reader.ReadString();

            var service = GetSerializationService(language);
            return service.ReadCompilationOptionsFrom(reader, cancellationToken);
        }

        public void Serialize(string language, ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: language specific, should I put this ability in compilation layer?
            writer.WriteString(language);

            var service = GetSerializationService(language);
            service.WriteTo(options, writer, cancellationToken);
        }

        private ParseOptions DeserializeParseOptions(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = reader.ReadString();

            var service = GetSerializationService(language);
            return service.ReadParseOptionsFrom(reader, cancellationToken);
        }

        public void Serialize(ProjectReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Serialize(reference.ProjectId, writer, cancellationToken);
            writer.WriteArray(reference.Aliases.ToArray());
            writer.WriteBoolean(reference.EmbedInteropTypes);
        }

        private ProjectReference DeserializeProjectReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectId = DeserializeProjectId(reader, cancellationToken);
            var aliases = reader.ReadArray<string>();
            var embedInteropTypes = reader.ReadBoolean();

            return new ProjectReference(projectId, aliases.ToImmutableArrayOrEmpty(), embedInteropTypes);
        }

        public void Serialize(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HostSerializationService.WriteTo(reference, writer, cancellationToken);
        }

        private MetadataReference DeserializeMetadataReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return HostSerializationService.ReadMetadataReferenceFrom(reader, cancellationToken);
        }

        public void Serialize(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HostSerializationService.WriteTo(reference, writer, cancellationToken);
        }

        private AnalyzerReference DeserializeAnalyzerReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return HostSerializationService.ReadAnalyzerReferenceFrom(reader, cancellationToken);
        }

        public void Serialize(SolutionId solutionId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteArray(solutionId.Id.ToByteArray());
            writer.WriteString(solutionId.DebugName);
        }

        private SolutionId DeserializeSolutionId(ObjectReader reader, CancellationToken cancellationToken)
        {
            var guid = new Guid(reader.ReadArray<byte>());
            var debugName = reader.ReadString();

            return SolutionId.CreateFromSerialized(guid, debugName);
        }

        public void Serialize(ProjectId projectId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteArray(projectId.Id.ToByteArray());
            writer.WriteString(projectId.DebugName);
        }

        private ProjectId DeserializeProjectId(ObjectReader reader, CancellationToken cancellationToken)
        {
            var guid = new Guid(reader.ReadArray<byte>());
            var debugName = reader.ReadString();

            return ProjectId.CreateFromSerialized(guid, debugName);
        }

        public void Serialize(DocumentId documentId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            Serialize(documentId.ProjectId, writer, cancellationToken);

            writer.WriteArray(documentId.Id.ToByteArray());
            writer.WriteString(documentId.DebugName);
        }

        private DocumentId DeserializeDocumentId(ObjectReader reader, CancellationToken cancellationToken)
        {
            var projectId = DeserializeProjectId(reader, cancellationToken);

            var guid = new Guid(reader.ReadArray<byte>());
            var debugName = reader.ReadString();

            return DocumentId.CreateFromSerialized(projectId, guid, debugName);
        }
    }
}
