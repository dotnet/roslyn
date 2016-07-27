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
        public static ChecksumCollectionWithActualData<ProjectChecksumObject> ToProjectObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Projects);
            return new ChecksumCollectionWithActualData<ProjectChecksumObject>(service, collection);
        }

        public static ChecksumCollectionWithActualData<DocumentChecksumObject> ToDocumentObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Documents || collection.Kind == WellKnownChecksumObjects.TextDocuments);
            return new ChecksumCollectionWithActualData<DocumentChecksumObject>(service, collection);
        }
    }

    /// <summary>
    /// this is a helper collection for unit test. just packaging checksum collection with actual items
    /// </summary>
    internal class ChecksumCollectionWithActualData<T> : ChecksumObject where T : ChecksumObject
    {
        public ChecksumCollectionWithActualData(ISolutionChecksumService service, ChecksumCollection collection) : base(collection.Checksum, collection.Kind)
        {
            Objects = ImmutableArray.CreateRange(collection.Objects.Select(c => (T)service.GetChecksumObject(c, CancellationToken.None)));
        }

        public ImmutableArray<T> Objects { get; }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("should not be called");
        }
    }
}
