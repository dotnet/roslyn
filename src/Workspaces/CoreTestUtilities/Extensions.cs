// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    /// <summary>
    /// this is a helper collection for unit test. just packaging checksum collection with actual items.
    /// </summary>
    internal class ChecksumObjectCollection<T> : RemotableData, IEnumerable<T> where T : ChecksumWithChildren
    {
        public ImmutableArray<T> Children { get; }

        public ChecksumObjectCollection(SerializationValidator validator, ChecksumCollection collection)
            : base(collection.Checksum, collection.GetWellKnownSynchronizationKind())
        {
            // using .Result here since we don't want to convert all calls to this to async.
            // and none of ChecksumWithChildren actually use async
            Children = ImmutableArray.CreateRange(collection.Select(c => validator.GetValueAsync<T>(c).Result));
        }

        public int Count => Children.Length;

        public T this[int index] => Children[index];

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // somehow, ImmutableArray<T>.Enumerator doesn't implement IEnumerator<T>
        public IEnumerator<T> GetEnumerator() => Children.Select(t => t).GetEnumerator();

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            => throw new NotImplementedException("should not be called");
    }
}
