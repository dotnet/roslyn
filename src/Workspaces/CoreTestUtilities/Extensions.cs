// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Execution
{
    internal static class Extensions
    {
        public static async Task<T> GetValueAsync<T>(this IRemotableDataService service, Checksum checksum)
        {
            var syncService = (RemotableDataServiceFactory.Service)service;
            var syncObject = syncService.GetRemotableData_TestOnly(checksum, CancellationToken.None);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                // serialize asset to bits
                await syncObject.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    // deserialize bits to object
                    var serializer = syncService.Serializer_TestOnly;
                    return serializer.Deserialize<T>(syncObject.Kind, reader, CancellationToken.None);
                }
            }
        }

        public static ChecksumObjectCollection<ProjectStateChecksums> ToProjectObjects(this ProjectChecksumCollection collection, IRemotableDataService service)
        {
            return new ChecksumObjectCollection<ProjectStateChecksums>(service, collection);
        }

        public static ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(this DocumentChecksumCollection collection, IRemotableDataService service)
        {
            return new ChecksumObjectCollection<DocumentStateChecksums>(service, collection);
        }

        public static ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(this TextDocumentChecksumCollection collection, IRemotableDataService service)
        {
            return new ChecksumObjectCollection<DocumentStateChecksums>(service, collection);
        }

        public static ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(this AnalyzerConfigDocumentChecksumCollection collection, IRemotableDataService service)
        {
            return new ChecksumObjectCollection<DocumentStateChecksums>(service, collection);
        }
    }

    /// <summary>
    /// this is a helper collection for unit test. just packaging checksum collection with actual items.
    /// </summary>
    internal class ChecksumObjectCollection<T> : RemotableData, IEnumerable<T> where T : ChecksumWithChildren
    {
        public ImmutableArray<T> Children { get; }

        public ChecksumObjectCollection(IRemotableDataService service, ChecksumCollection collection) : base(collection.Checksum, collection.GetWellKnownSynchronizationKind())
        {
            // using .Result here since we don't want to convert all calls to this to async.
            // and none of ChecksumWithChildren actually use async
            Children = ImmutableArray.CreateRange(collection.Select(c => service.GetValueAsync<T>(c).Result));
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

    internal sealed class AssetProvider : IAssetProvider
    {
        private readonly IRemotableDataService _service;

        public AssetProvider(IRemotableDataService service)
        {
            _service = service;
        }

        public Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            return _service.GetValueAsync<T>(checksum);
        }
    }

}
