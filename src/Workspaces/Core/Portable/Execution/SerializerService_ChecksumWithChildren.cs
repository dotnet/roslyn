// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Execution;
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

        private static readonly ImmutableDictionary<WellKnownSynchronizationKind, Func<object[], ChecksumWithChildren>> s_creatorMap = CreateCreatorMap();

        public void SerializeChecksumWithChildren(ChecksumWithChildren checksums, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = checksums.GetWellKnownSynchronizationKind();
            writer.WriteInt32((int)kind);
            checksums.Checksum.WriteTo(writer);

            writer.WriteInt32(checksums.Children.Count);
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

        private ChecksumWithChildren DeserializeChecksumWithChildren(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = (WellKnownSynchronizationKind)reader.ReadInt32();
            var checksum = Checksum.ReadFrom(reader);

            var childrenCount = reader.ReadInt32();
            var children = new object[childrenCount];

            for (var i = 0; i < childrenCount; i++)
            {
                var childKind = reader.ReadByte();
                if (childKind == ChecksumKind)
                {
                    children[i] = Checksum.ReadFrom(reader);
                    continue;
                }

                if (childKind == ChecksumWithChildrenKind)
                {
                    children[i] = DeserializeChecksumWithChildren(reader, cancellationToken);
                    continue;
                }

                throw ExceptionUtilities.UnexpectedValue(childKind);
            }

            var checksums = s_creatorMap[kind](children);
            Contract.ThrowIfFalse(checksums.Checksum == checksum);

            return checksums;
        }

        private static ImmutableDictionary<WellKnownSynchronizationKind, Func<object[], ChecksumWithChildren>> CreateCreatorMap()
        {
            return ImmutableDictionary<WellKnownSynchronizationKind, Func<object[], ChecksumWithChildren>>.Empty
                .Add(WellKnownSynchronizationKind.SolutionState, children => new SolutionStateChecksums(children))
                .Add(WellKnownSynchronizationKind.ProjectState, children => new ProjectStateChecksums(children))
                .Add(WellKnownSynchronizationKind.DocumentState, children => new DocumentStateChecksums(children))
                .Add(WellKnownSynchronizationKind.Projects, children => new ProjectChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.Documents, children => new DocumentChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.TextDocuments, children => new TextDocumentChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.AnalyzerConfigDocuments, children => new AnalyzerConfigDocumentChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.ProjectReferences, children => new ProjectReferenceChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.MetadataReferences, children => new MetadataReferenceChecksumCollection(children))
                .Add(WellKnownSynchronizationKind.AnalyzerReferences, children => new AnalyzerReferenceChecksumCollection(children));
        }
    }
}
