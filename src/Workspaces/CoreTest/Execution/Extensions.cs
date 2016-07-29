// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Execution
{
    internal static class Extensions
    {
        public static ChecksumObjects<ProjectChecksumObject> ToProjectObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Projects);
            return new ChecksumObjects<ProjectChecksumObject>(service, collection);
        }

        public static ChecksumObjects<DocumentChecksumObject> ToDocumentObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Documents || collection.Kind == WellKnownChecksumObjects.TextDocuments);
            return new ChecksumObjects<DocumentChecksumObject>(service, collection);
        }
    }

    internal class ChecksumObjects<T> : ChecksumObject where T : ChecksumObject
    {
        private readonly ChecksumCollection _collection;

        public ChecksumObjects(ISolutionChecksumService servie, ChecksumCollection collection) : base(collection.Checksum, collection.Kind)
        {
            _collection = collection;

            Objects = ImmutableArray.CreateRange(collection.Objects.Select(c => (T)servie.GetChecksumObject(c, CancellationToken.None)));
        }

        public ImmutableArray<T> Objects { get; }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("should not be called");
        }
    }
}
