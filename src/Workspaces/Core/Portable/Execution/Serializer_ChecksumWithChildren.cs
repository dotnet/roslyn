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
    internal partial class Serializer
    {
        private const byte ChecksumKind = 0;
        private const byte ChecksumWithChildrenKind = 1;

        private static readonly ImmutableDictionary<string, Func<object[], ChecksumWithChildren>> s_creatorMap = CreateCreatorMap();

        public void SerializeChecksumWithChildren(ChecksumWithChildren checksums, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = checksums.GetWellKnownSynchronizationKind();
            writer.WriteString(kind);
            checksums.Checksum.WriteTo(writer);

            writer.WriteInt32(checksums.Children.Count);
            foreach (var child in checksums.Children)
            {
                var checksum = child as Checksum;
                if (checksum != null)
                {
                    writer.WriteByte(ChecksumKind);
                    checksum.WriteTo(writer);
                    continue;
                }

                var checksumCollection = child as ChecksumCollection;
                if (checksumCollection != null)
                {
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

            var kind = reader.ReadString();
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

        private static ImmutableDictionary<string, Func<object[], ChecksumWithChildren>> CreateCreatorMap()
        {
            return ImmutableDictionary<string, Func<object[], ChecksumWithChildren>>.Empty
                .Add(WellKnownSynchronizationKinds.SolutionState, children => new SolutionStateChecksums(children))
                .Add(WellKnownSynchronizationKinds.ProjectState, children => new ProjectStateChecksums(children))
                .Add(WellKnownSynchronizationKinds.DocumentState, children => new DocumentStateChecksums(children))
                .Add(WellKnownSynchronizationKinds.Projects, children => new ProjectChecksumCollection(children))
                .Add(WellKnownSynchronizationKinds.Documents, children => new DocumentChecksumCollection(children))
                .Add(WellKnownSynchronizationKinds.TextDocuments, children => new TextDocumentChecksumCollection(children))
                .Add(WellKnownSynchronizationKinds.ProjectReferences, children => new ProjectReferenceChecksumCollection(children))
                .Add(WellKnownSynchronizationKinds.MetadataReferences, children => new MetadataReferenceChecksumCollection(children))
                .Add(WellKnownSynchronizationKinds.AnalyzerReferences, children => new AnalyzerReferenceChecksumCollection(children));
        }
    }
}
