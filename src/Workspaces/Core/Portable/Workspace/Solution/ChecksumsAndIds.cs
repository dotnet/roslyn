// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// A paired list of <see cref="ProjectId"/>s, and the checksums for their corresponding <see
/// cref="ProjectStateChecksums"/>'s <see cref="ProjectStateChecksums.Checksum"/>.
/// </summary>
internal readonly struct ProjectChecksumsAndIds
{
    public readonly ChecksumCollection Checksums;
    public readonly ImmutableArray<ProjectId> Ids;

    public ProjectChecksumsAndIds(ChecksumCollection checksums, ImmutableArray<ProjectId> ids)
    {
        Contract.ThrowIfTrue(ids.Length != checksums.Children.Length);

        Checksums = checksums;
        Ids = ids;
    }

    public int Length => Ids.Length;
    public Checksum Checksum => Checksums.Checksum;

    public void WriteTo(ObjectWriter writer)
    {
        this.Checksums.WriteTo(writer);
        writer.WriteArray(this.Ids, static (writer, p) => p.WriteTo(writer));
    }

    public static ProjectChecksumsAndIds ReadFrom(ObjectReader reader)
    {
        return new(
            ChecksumCollection.ReadFrom(reader),
            reader.ReadArray(static reader => ProjectId.ReadFrom(reader)));
    }

    public Enumerator GetEnumerator()
        => new(this);

    public struct Enumerator(ProjectChecksumsAndIds checksumsAndIds)
    {
        private readonly ProjectChecksumsAndIds _checksumsAndIds = checksumsAndIds;
        private int _index = -1;

        public bool MoveNext()
            => ++_index < _checksumsAndIds.Length;

        public readonly (Checksum checksum, ProjectId id) Current
            => (_checksumsAndIds.Checksums.Children[_index], _checksumsAndIds.Ids[_index]);
    }
}

/// <summary>
/// A paired list of <see cref="DocumentId"/>s, and the checksums for their corresponding <see
/// cref="DocumentStateChecksums"/>'s <see cref="DocumentStateChecksums.Info"/> and <see
/// cref="DocumentStateChecksums.Text"/> checksums.
/// </summary>
internal readonly struct DocumentChecksumsAndIds
{
    public readonly Checksum Checksum;
    public readonly ChecksumCollection AttributeChecksums;
    public readonly ChecksumCollection TextChecksums;
    public readonly ImmutableArray<DocumentId> Ids;

    public DocumentChecksumsAndIds(ChecksumCollection attributeChecksums, ChecksumCollection textChecksums, ImmutableArray<DocumentId> ids)
    {
        Contract.ThrowIfTrue(ids.Length != attributeChecksums.Children.Length);
        Contract.ThrowIfTrue(ids.Length != textChecksums.Children.Length);

        AttributeChecksums = attributeChecksums;
        TextChecksums = textChecksums;
        Ids = ids;

        Checksum = Checksum.Create(attributeChecksums.Checksum, textChecksums.Checksum);
    }

    public int Length => Ids.Length;

    public void WriteTo(ObjectWriter writer)
    {
        this.AttributeChecksums.WriteTo(writer);
        this.TextChecksums.WriteTo(writer);
        writer.WriteArray(this.Ids, static (writer, id) => id.WriteTo(writer));
    }

    public static DocumentChecksumsAndIds ReadFrom(ObjectReader reader)
    {
        return new(
            ChecksumCollection.ReadFrom(reader),
            ChecksumCollection.ReadFrom(reader),
            reader.ReadArray(static reader => DocumentId.ReadFrom(reader)));
    }

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        this.AttributeChecksums.AddAllTo(checksums);
        this.TextChecksums.AddAllTo(checksums);
    }

    public Enumerator GetEnumerator()
        => new(this);

    public struct Enumerator(DocumentChecksumsAndIds checksumsAndIds)
    {
        private readonly DocumentChecksumsAndIds _checksumsAndIds = checksumsAndIds;
        private int _index = -1;

        public bool MoveNext()
            => ++_index < _checksumsAndIds.Length;

        public readonly (Checksum attributeChecksum, Checksum textChecksum, DocumentId id) Current
            => (_checksumsAndIds.AttributeChecksums.Children[_index],
                _checksumsAndIds.TextChecksums.Children[_index],
                _checksumsAndIds.Ids[_index]);
    }
}
