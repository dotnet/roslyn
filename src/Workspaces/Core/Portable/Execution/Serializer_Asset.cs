// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// serialize and deserialize objects to stream.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// </summary>
    internal partial class Serializer
    {
        public void SerializeSourceText(ITemporaryStorageWithName storage, SourceText text, ObjectWriter writer, CancellationToken cancellationToken)
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

        public void SerializeCompilationOptions(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = options.Language;

            // TODO: once compiler team adds ability to serialize compilation options to ObjectWriter directly, we won't need this.
            writer.WriteString(language);

            var service = GetOptionsSerializationService(language);
            service.WriteTo(options, writer, cancellationToken);
        }

        private CompilationOptions DeserializeCompilationOptions(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = reader.ReadString();

            var service = GetOptionsSerializationService(language);
            return service.ReadCompilationOptionsFrom(reader, cancellationToken);
        }

        public void SerializeParseOptions(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = options.Language;

            // TODO: once compiler team adds ability to serialize parse options to ObjectWriter directly, we won't need this.
            writer.WriteString(language);

            var service = GetOptionsSerializationService(language);
            service.WriteTo(options, writer, cancellationToken);
        }

        private ParseOptions DeserializeParseOptions(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = reader.ReadString();

            var service = GetOptionsSerializationService(language);
            return service.ReadParseOptionsFrom(reader, cancellationToken);
        }

        public void SerializeProjectReference(ProjectReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            reference.ProjectId.WriteTo(writer);
            writer.WriteValue(reference.Aliases.ToArray());
            writer.WriteBoolean(reference.EmbedInteropTypes);
        }

        private ProjectReference DeserializeProjectReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectId = ProjectId.ReadFrom(reader);
            var aliases = reader.ReadArray<string>();
            var embedInteropTypes = reader.ReadBoolean();

            return new ProjectReference(projectId, aliases.ToImmutableArrayOrEmpty(), embedInteropTypes);
        }

        public void SerializeMetadataReference(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _hostSerializationService.WriteTo(reference, writer, cancellationToken);
        }

        private MetadataReference DeserializeMetadataReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hostSerializationService.ReadMetadataReferenceFrom(reader, cancellationToken);
        }

        public void SerializeAnalyzerReference(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _hostSerializationService.WriteTo(reference, writer, cancellationToken);
        }

        private AnalyzerReference DeserializeAnalyzerReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hostSerializationService.ReadAnalyzerReferenceFrom(reader, cancellationToken);
        }
    }
}
