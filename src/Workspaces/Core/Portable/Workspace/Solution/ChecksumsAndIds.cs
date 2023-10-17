// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// A paired list of IDs (either <see cref="ProjectId"/>s or <see cref="DocumentId"/>s), and the checksums for their
/// corresponding <see cref="Project"/>s or <see cref="Document"/>s).
/// </summary>
internal readonly struct ChecksumsAndIds<TId> where TId : IObjectWritable
{
    public readonly ChecksumCollection Checksums;
    public readonly ImmutableArray<TId> Ids;

    private static readonly Func<ObjectReader, TId> s_readId;

    static ChecksumsAndIds()
    {
        if (typeof(TId) == typeof(ProjectId))
        {
            s_readId = reader => (TId)(object)ProjectId.ReadFrom(reader);
        }
        else if (typeof(TId) == typeof(DocumentId))
        {
            s_readId = reader => (TId)(object)DocumentId.ReadFrom(reader);
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public ChecksumsAndIds(ChecksumCollection checksums, ImmutableArray<TId> ids)
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
        writer.WriteArray(this.Ids, static (writer, value) => value.WriteTo(writer));
    }

    public static ChecksumsAndIds<TId> ReadFrom(ObjectReader reader)
    {
        return new(
            ChecksumCollection.ReadFrom(reader),
            reader.ReadArray(s_readId));
    }

    public Enumerator GetEnumerator()
        => new(this);

    public struct Enumerator(ChecksumsAndIds<TId> checksumsAndIds)
    {
        private readonly ChecksumsAndIds<TId> _checksumsAndIds = checksumsAndIds;
        private int _index = -1;

        public bool MoveNext()
            => ++_index < _checksumsAndIds.Length;

        public (Checksum checksum, TId id) Current
            => (_checksumsAndIds.Checksums.Children[_index], _checksumsAndIds.Ids[_index]);
    }
}
