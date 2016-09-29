// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// serialize and deserialize objects to straem.
    /// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
    /// </summary>
    internal partial class Serializer
    {
        private const byte ChecksumKind = 0;
        private const byte ChecksumCollectionKind = 1;

        private static readonly ImmutableDictionary<string, Func<Serializer, string, object[], ChecksumObjectWithChildren>> s_creatorMap = CreateCreatorMap();

        public void SerializeChecksumObjectWithChildren(ChecksumObjectWithChildren checksumObject, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteString(checksumObject.Kind);
            checksumObject.Checksum.WriteTo(writer);

            writer.WriteInt32(checksumObject.Children.Length);
            foreach (var child in checksumObject.Children)
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
                    writer.WriteByte(ChecksumCollectionKind);
                    SerializeChecksumObjectWithChildren(checksumCollection, writer, cancellationToken);
                    continue;
                }

                throw ExceptionUtilities.UnexpectedValue(child);
            }
        }

        private ChecksumObjectWithChildren DeserializeChecksumObjectWithChildren(ObjectReader reader, CancellationToken cancellationToken)
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

                if (childKind == ChecksumCollectionKind)
                {
                    children[i] = DeserializeChecksumObjectWithChildren(reader, cancellationToken);
                    continue;
                }

                throw ExceptionUtilities.UnexpectedValue(childKind);
            }

            var checksumObject = s_creatorMap[kind](this, kind, children);
            Contract.ThrowIfFalse(checksum.Equals(checksumObject.Checksum));

            return checksumObject;
        }

        private static ImmutableDictionary<string, Func<Serializer, string, object[], ChecksumObjectWithChildren>> CreateCreatorMap()
        {
            return ImmutableDictionary<string, Func<Serializer, string, object[], ChecksumObjectWithChildren>>.Empty
                .Add(SolutionChecksumObject.Name, (serializer, kind, children) => new SolutionChecksumObject(serializer, children))
                .Add(ProjectChecksumObject.Name, (serializer, kind, children) => new ProjectChecksumObject(serializer, children))
                .Add(DocumentChecksumObject.Name, (serializer, kind, children) => new DocumentChecksumObject(serializer, children))
                .Add(WellKnownChecksumObjects.Projects, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children))
                .Add(WellKnownChecksumObjects.Documents, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children))
                .Add(WellKnownChecksumObjects.TextDocuments, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children))
                .Add(WellKnownChecksumObjects.ProjectReferences, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children))
                .Add(WellKnownChecksumObjects.MetadataReferences, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children))
                .Add(WellKnownChecksumObjects.AnalyzerReferences, (serializer, kind, children) => new ChecksumCollection(serializer, kind, children));
        }
    }
}
