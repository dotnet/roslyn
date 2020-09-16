// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
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
    internal partial class SerializerService
    {
        public void SerializeSourceText(SerializableSourceText text, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (text.Storage is not null)
            {
                writer.WriteInt32((int)text.Storage.ChecksumAlgorithm);
                writer.WriteEncoding(text.Storage.Encoding);

                writer.WriteInt32((int)SerializationKinds.MemoryMapFile);
                writer.WriteString(text.Storage.Name);
                writer.WriteInt64(text.Storage.Offset);
                writer.WriteInt64(text.Storage.Size);
            }
            else
            {
                RoslynDebug.AssertNotNull(text.Text);

                writer.WriteInt32((int)text.Text.ChecksumAlgorithm);
                writer.WriteEncoding(text.Text.Encoding);
                writer.WriteInt32((int)SerializationKinds.Bits);
                text.Text.WriteTo(writer, cancellationToken);
            }
        }

        private SerializableSourceText DeserializeSerializableSourceText(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var encoding = (Encoding)reader.ReadValue();

            var kind = (SerializationKinds)reader.ReadInt32();
            if (kind == SerializationKinds.MemoryMapFile)
            {
                var storage2 = (ITemporaryStorageService2)_storageService;

                var name = reader.ReadString();
                var offset = reader.ReadInt64();
                var size = reader.ReadInt64();

                var storage = storage2.AttachTemporaryTextStorage(name, offset, size, checksumAlgorithm, encoding, cancellationToken);
                if (storage is ITemporaryTextStorageWithName storageWithName)
                {
                    return new SerializableSourceText(storageWithName);
                }
                else
                {
                    return new SerializableSourceText(storage.ReadText(cancellationToken));
                }
            }

            Contract.ThrowIfFalse(kind == SerializationKinds.Bits);
            return new SerializableSourceText(SourceTextExtensions.ReadFrom(_textService, reader, encoding, cancellationToken));
        }

        private SourceText DeserializeSourceText(ObjectReader reader, CancellationToken cancellationToken)
        {
            var serializableSourceText = DeserializeSerializableSourceText(reader, cancellationToken);
            return serializableSourceText.Text ?? serializableSourceText.Storage!.ReadText(cancellationToken);
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

        private static ProjectReference DeserializeProjectReference(ObjectReader reader, CancellationToken cancellationToken)
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
            WriteTo(reference, writer, cancellationToken);
        }

        private MetadataReference DeserializeMetadataReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadMetadataReferenceFrom(reader, cancellationToken);
        }

        public void SerializeAnalyzerReference(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteTo(reference, writer, cancellationToken);
        }

        private AnalyzerReference DeserializeAnalyzerReference(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadAnalyzerReferenceFrom(reader, cancellationToken);
        }
    }
}
