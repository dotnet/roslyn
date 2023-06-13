// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// serialize and deserialize objects to straem.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// </summary>
    internal partial class SerializerService
    {
        private const byte ChecksumKind = 0;
        private const byte ChecksumWithChildrenKind = 1;

        private static readonly ImmutableDictionary<WellKnownSynchronizationKind, Func<ImmutableArray<object>, ChecksumWithChildren>> s_creatorMap = CreateCreatorMap();

        public void SerializeChecksumWithChildren(ChecksumWithChildren checksums, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = checksums.GetWellKnownSynchronizationKind();
            writer.WriteInt32((int)kind);
            checksums.Checksum.WriteTo(writer);

            writer.WriteInt32(checksums.Children.Length);
            foreach (var child in checksums.Children)
            {
                switch (child)
                {
                    case Checksum checksum:
                        writer.WriteByte(ChecksumKind);
                        checksum.WriteTo(writer);
                        continue;
                    case ChecksumCollection checksumCollection:
                        writer.WriteByte(ChecksumWithChildrenKind);
                        SerializeChecksumWithChildren(checksumCollection, writer, cancellationToken);
                        continue;
                }

                throw ExceptionUtilities.UnexpectedValue(child);
            }
        }

        private static ChecksumWithChildren DeserializeChecksumWithChildren(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = (WellKnownSynchronizationKind)reader.ReadInt32();
            var checksum = Checksum.ReadFrom(reader);

            var childrenCount = reader.ReadInt32();
            var children = ImmutableArray.CreateBuilder<object>(childrenCount);

            for (var i = 0; i < childrenCount; i++)
            {
                var childKind = reader.ReadByte();
                if (childKind == ChecksumKind)
                {
                    children.Add(Checksum.ReadFrom(reader));
                }
                else if (childKind == ChecksumWithChildrenKind)
                {
                    children.Add(DeserializeChecksumWithChildren(reader, cancellationToken));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(childKind);
                }
            }

            var checksums = s_creatorMap[kind](children.MoveToImmutable());
            Contract.ThrowIfFalse(checksums.Checksum == checksum);

            return checksums;
        }

        private static ImmutableDictionary<WellKnownSynchronizationKind, Func<ImmutableArray<object>, ChecksumWithChildren>> CreateCreatorMap()
        {
            return ImmutableDictionary<WellKnownSynchronizationKind, Func<ImmutableArray<object>, ChecksumWithChildren>>.Empty
                .Add(WellKnownSynchronizationKind.SolutionState, children => new SolutionStateChecksums(children))
                .Add(WellKnownSynchronizationKind.ProjectState, children => new ProjectStateChecksums(children))
                .Add(WellKnownSynchronizationKind.DocumentState, children => new DocumentStateChecksums(children))
                .Add(WellKnownSynchronizationKind.ChecksumCollection, children => new ChecksumCollection(children));
        }
    }
}
