// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// collection which children is checksum.
    /// </summary>
    internal abstract class ChecksumCollection : ChecksumWithChildren, IEnumerable<Checksum>
    {
        protected ChecksumCollection(WellKnownSynchronizationKind kind, Checksum[] checksums) : this(kind, (object[])checksums)
        {
        }

        protected ChecksumCollection(WellKnownSynchronizationKind kind, object[] checksums) : base(kind, checksums)
        {
        }

        public int Count => Children.Count;
        public Checksum this[int index] => (Checksum)Children[index];

        public IEnumerator<Checksum> GetEnumerator()
            => this.Children.Cast<Checksum>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        internal static async Task FindAsync<TKey, TValue>(
            ImmutableSortedDictionary<TKey, TValue> documentStates,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken) where TValue : TextDocumentState
        {
            foreach (var (_, state) in documentStates)
            {
                Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksums));

                await stateChecksums.FindAsync(state, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
                if (searchingChecksumsLeft.Count == 0)
                {
                    return;
                }
            }
        }

        internal static void Find<T>(
            IReadOnlyList<T> values,
            ChecksumWithChildren checksums,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(values.Count == checksums.Children.Count);

            for (var i = 0; i < checksums.Children.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (searchingChecksumsLeft.Count == 0)
                {
                    return;
                }

                var checksum = (Checksum)checksums.Children[i];
                var value = values[i];

                if (searchingChecksumsLeft.Remove(checksum))
                {
                    result[checksum] = value;
                }
            }
        }
    }

    // we have a type for each kind so that we can distinguish these later
    internal class ProjectChecksumCollection : ChecksumCollection
    {
        public ProjectChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public ProjectChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.ProjectChecksumCollection, checksums) { }
    }

    internal class DocumentChecksumCollection : ChecksumCollection
    {
        public DocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public DocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.DocumentChecksumCollection, checksums) { }
    }

    internal class TextDocumentChecksumCollection : ChecksumCollection
    {
        public TextDocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public TextDocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.TextDocumentChecksumCollection, checksums) { }
    }

    internal class AnalyzerConfigDocumentChecksumCollection : ChecksumCollection
    {
        public AnalyzerConfigDocumentChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public AnalyzerConfigDocumentChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.AnalyzerConfigDocumentChecksumCollection, checksums) { }
    }

    internal class ProjectReferenceChecksumCollection : ChecksumCollection
    {
        public ProjectReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public ProjectReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.ProjectReferenceChecksumCollection, checksums) { }
    }

    internal class MetadataReferenceChecksumCollection : ChecksumCollection
    {
        public MetadataReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public MetadataReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.MetadataReferenceChecksumCollection, checksums) { }
    }

    internal class AnalyzerReferenceChecksumCollection : ChecksumCollection
    {
        public AnalyzerReferenceChecksumCollection(Checksum[] checksums) : this((object[])checksums) { }
        public AnalyzerReferenceChecksumCollection(object[] checksums) : base(WellKnownSynchronizationKind.AnalyzerReferenceChecksumCollection, checksums) { }
    }
}
