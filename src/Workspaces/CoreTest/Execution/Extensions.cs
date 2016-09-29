// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
        public static ChecksumObjectCollection<ProjectChecksumObject> ToProjectObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Projects);
            return new ChecksumObjectCollection<ProjectChecksumObject>(service, collection);
        }

        public static ChecksumObjectCollection<DocumentChecksumObject> ToDocumentObjects(this ChecksumCollection collection, ISolutionChecksumService service)
        {
            Contract.ThrowIfFalse(collection.Kind == WellKnownChecksumObjects.Documents || collection.Kind == WellKnownChecksumObjects.TextDocuments);
            return new ChecksumObjectCollection<DocumentChecksumObject>(service, collection);
        }
    }

    /// <summary>
    /// this is a helper collection for unit test. just packaging checksum collection with actual items.
    /// 
    /// unlike ChecksumObjectWithChildren which can only have checksum or checksumCollection as its child, this lets another checksumObjectWithChildren as child as well
    /// </summary>
    internal class ChecksumObjectCollection<T> : ChecksumObject, IEnumerable<T> where T : ChecksumObject
    {
        public ImmutableArray<T> Children { get; }

        public ChecksumObjectCollection(ISolutionChecksumService service, ChecksumCollection collection) : base(collection.Checksum, collection.Kind)
        {
            Children = ImmutableArray.CreateRange(collection.Select(c => (T)service.GetChecksumObject(c, CancellationToken.None)));
        }

        public int Count => Children.Length;

        public T this[int index] => Children[index];

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // somehow, ImmutableArray<T>.Enumerator doesn't implement IEnumerator<T>
        public IEnumerator<T> GetEnumerator() => Children.Select(t => t).GetEnumerator();

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("should not be called");
        }
    }
}
