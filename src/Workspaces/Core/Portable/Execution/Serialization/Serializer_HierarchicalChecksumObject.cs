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
        public async Task SerializeAsync(SolutionSnapshotId snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            snapshotId.Info.WriteTo(writer);
            await snapshotId.Projects.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        private SolutionSnapshotId DeserializeSolutionSnapshotId(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == SolutionSnapshotId.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var projects = DeserializeSnapshotIdCollection<ProjectSnapshotId>(reader, cancellationToken);

            var snapshotId = new SolutionSnapshotId(this, info, projects);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public async Task SerializeAsync(ProjectSnapshotId snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
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

        private ProjectSnapshotId DeserializeProjectSnapshotId(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == ProjectSnapshotId.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var compilationOptions = Checksum.ReadFrom(reader);
            var parseOptions = Checksum.ReadFrom(reader);

            var documents = DeserializeSnapshotIdCollection<DocumentSnapshotId>(reader, cancellationToken);

            var projectReferences = DeserializeChecksumCollection(reader, cancellationToken);
            var metadataReferences = DeserializeChecksumCollection(reader, cancellationToken);
            var analyzerReferences = DeserializeChecksumCollection(reader, cancellationToken);

            var additionalDocuments = DeserializeSnapshotIdCollection<DocumentSnapshotId>(reader, cancellationToken);

            var snapshotId = new ProjectSnapshotId(
                this,
                info, compilationOptions, parseOptions, documents,
                projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public void Serialize(DocumentSnapshotId snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            snapshotId.Info.WriteTo(writer);
            snapshotId.Text.WriteTo(writer);
        }

        private DocumentSnapshotId DeserializeDocumentSnapshotId(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = reader.ReadString();
            Contract.ThrowIfFalse(kind == DocumentSnapshotId.Name);

            var checksum = Checksum.ReadFrom(reader);

            var info = Checksum.ReadFrom(reader);
            var text = Checksum.ReadFrom(reader);

            var snapshotId = new DocumentSnapshotId(this, info, text);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        public async Task SerializeAsync<T>(SnapshotIdCollection<T> snapshotId, ObjectWriter writer, CancellationToken cancellationToken)
            where T : ChecksumObject
        {
            writer.WriteString(snapshotId.Kind);
            snapshotId.Checksum.WriteTo(writer);

            writer.WriteInt32(snapshotId.Objects.Length);
            foreach (var item in snapshotId.Objects)
            {
                await item.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        private SnapshotIdCollection<T> DeserializeSnapshotIdCollection<T>(ObjectReader reader, CancellationToken cancellationToken)
            where T : ChecksumObject
        {
            var kind = reader.ReadString();
            var checksum = Checksum.ReadFrom(reader);

            var length = reader.ReadInt32();
            var builder = ImmutableArray.CreateBuilder<T>(length);

            var itemKind = GetItemKind(kind);
            for (var i = 0; i < length; i++)
            {
                builder.Add(Deserialize<T>(itemKind, reader, cancellationToken));
            }

            var snapshotId = new SnapshotIdCollection<T>(this, builder.ToImmutable(), kind);
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

            var snapshotId = new ChecksumCollection(this, builder.ToImmutable(), kind);
            Contract.ThrowIfFalse(checksum.Equals(snapshotId.Checksum));

            return snapshotId;
        }

        private string GetItemKind(string kind)
        {
            switch (kind)
            {
                case WellKnownChecksumObjects.Projects:
                    return ProjectSnapshotId.Name;
                case WellKnownChecksumObjects.Documents:
                    return DocumentSnapshotId.Name;
                case WellKnownChecksumObjects.TextDocuments:
                    return DocumentSnapshotId.Name;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
