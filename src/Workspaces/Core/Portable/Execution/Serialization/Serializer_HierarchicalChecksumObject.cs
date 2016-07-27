// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// serialize and deserialize objects to straem.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// </summary>
    internal partial class Serializer
    {
        public async Task SerializeAsync(SolutionChecksumObject snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            snapshotId.Info.WriteTo(writer);
            await snapshotId.Projects.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        private SolutionChecksumObject DeserializeSolutionChecksumObject(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == SolutionChecksumObject.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var projects = DeserializeChecksumCollection(reader, cancellationToken);

            var snapshotId = new SolutionChecksumObject(this, info, projects);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public async Task SerializeAsync(ProjectChecksumObject snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            snapshotId.Info.WriteTo(writer);
            snapshotId.CompilationOptions.WriteTo(writer);
            snapshotId.ParseOptions.WriteTo(writer);

            await snapshotId.Documents.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);

            await snapshotId.ProjectReferences.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            await snapshotId.MetadataReferences.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            await snapshotId.AnalyzerReferences.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);

            await snapshotId.AdditionalDocuments.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        private ProjectChecksumObject DeserializeProjectChecksumObject(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == ProjectChecksumObject.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var compilationOptions = Checksum.ReadFrom(reader);
            var parseOptions = Checksum.ReadFrom(reader);

            var documents = DeserializeChecksumCollection(reader, cancellationToken);

            var projectReferences = DeserializeChecksumCollection(reader, cancellationToken);
            var metadataReferences = DeserializeChecksumCollection(reader, cancellationToken);
            var analyzerReferences = DeserializeChecksumCollection(reader, cancellationToken);

            var additionalDocuments = DeserializeChecksumCollection(reader, cancellationToken);

            var snapshotId = new ProjectChecksumObject(
                this,
                info, compilationOptions, parseOptions, documents,
                projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public void Serialize(DocumentChecksumObject snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            snapshotId.Info.WriteTo(writer);
            snapshotId.Text.WriteTo(writer);
        }

        private DocumentChecksumObject DeserializeDocumentChecksumObject(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == DocumentChecksumObject.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var text = Checksum.ReadFrom(reader);

            var snapshotId = new DocumentChecksumObject(this, info, text);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public void Serialize(ChecksumCollection snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            writer.WriteInt32(snapshotId.Objects.Length);
            foreach (var item in snapshotId.Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                item.WriteTo(writer);
            }
        }

        private ChecksumCollection DeserializeChecksumCollection(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = reader.ReadString();
            var checksum = Checksum.ReadFrom(reader);

            var length = reader.ReadInt32();
            var builder = ImmutableArray.CreateBuilder<Checksum>(length);

            for (var i = 0; i < length; i++)
            {
                builder.Add(Checksum.ReadFrom(reader));
            }

            var snapshotId = new ChecksumCollection(this, builder.MoveToImmutable(), kind);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        private string GetItemKind(string kind)
        {
            switch (kind)
            {
                case WellKnownChecksumObjects.Projects:
                    return ProjectChecksumObject.Name;
                case WellKnownChecksumObjects.Documents:
                    return DocumentChecksumObject.Name;
                case WellKnownChecksumObjects.TextDocuments:
                    return DocumentChecksumObject.Name;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
