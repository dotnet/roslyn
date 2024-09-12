// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// A wrapper around an array of <see cref="Microsoft.CodeAnalysis.Checksum"/>s, which also combines the value into a
/// single aggregate checksum exposed through <see cref="Checksum"/>.
/// </summary>
internal readonly struct ChecksumCollection(ImmutableArray<Checksum> children) : IReadOnlyCollection<Checksum>
{
    /// <summary>
    /// Aggregate checksum produced from all the constituent checksums in <see cref="Children"/>.
    /// </summary>
    public Checksum Checksum { get; } = Checksum.Create(children);

    public int Count => children.Length;
    public Checksum this[int index] => children[index];
    public ImmutableArray<Checksum> Children => children;

    /// <summary>
    /// Enumerates the child checksums (found in <see cref="Children"/>) that make up this collection.   This is
    /// equivalent to directly enumerating the <see cref="Children"/> property.  Importantly, <see cref="Checksum"/> is
    /// not part of this enumeration.  <see cref="Checksum"/> is the checksum <em>produced</em> by all those child
    /// checksums.
    /// </summary>
    public ImmutableArray<Checksum>.Enumerator GetEnumerator()
        => children.GetEnumerator();

    IEnumerator<Checksum> IEnumerable<Checksum>.GetEnumerator()
    {
        foreach (var checksum in this)
            yield return checksum;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<Checksum>)this).GetEnumerator();

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        foreach (var checksum in this)
            checksums.AddIfNotNullChecksum(checksum);
    }

    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1333566", AllowGenericEnumeration = false)]
    internal static async Task FindAsync<TState>(
        TextDocumentStates<TState> documentStates,
        DocumentId? hintDocument,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken) where TState : TextDocumentState
    {
        if (hintDocument != null)
        {
            var state = documentStates.GetState(hintDocument);
            if (state != null)
            {
                Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksums));
                await stateChecksums.FindAsync(state, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            foreach (var (_, state) in documentStates.States)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (searchingChecksumsLeft.Count == 0)
                    return;

                Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksums));

                await stateChecksums.FindAsync(state, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal static void Find<T>(
        IReadOnlyList<T> values,
        ChecksumCollection checksums,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken) where T : class
    {
        Contract.ThrowIfFalse(values.Count == checksums.Children.Length);

        for (var i = 0; i < checksums.Children.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (searchingChecksumsLeft.Count == 0)
                return;

            var checksum = checksums.Children[i];
            if (searchingChecksumsLeft.Remove(checksum))
                result[checksum] = values[i];
        }
    }

    public void WriteTo(ObjectWriter writer)
        => writer.WriteArray(this.Children, static (w, c) => c.WriteTo(w));

    public static ChecksumCollection ReadFrom(ObjectReader reader)
        => new(reader.ReadArray(Checksum.ReadFrom));
}
