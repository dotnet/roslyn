// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IRemoteSemanticClassificationService
    {
        ValueTask<SerializableClassifiedSpans> GetClassificationsAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            TextSpan span,
            ClassificationType type,
            ClassificationOptions options,
            bool isFullyLoaded,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tries to get cached semantic classifications for the specified document and the specified <paramref
        /// name="textSpan"/>.  Will return an empty array not able to.
        /// </summary>
        /// <param name="checksum">Pass in <see cref="DocumentStateChecksums.Text"/>.  This will ensure that the cached
        /// classifications are only returned if they match the content the file currently has.</param>
        ValueTask<SerializableClassifiedSpans?> GetCachedClassificationsAsync(
            DocumentKey documentKey,
            TextSpan textSpan,
            ClassificationType type,
            Checksum checksum,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// For space efficiency, we encode classified spans as triples of ints in one large array.  The
    /// first int is the index of classification type in <see cref="ClassificationTypes"/>, and the
    /// second and third ints encode the span.
    /// </summary>
    [DataContract]
    internal sealed class SerializableClassifiedSpans(ImmutableArray<string> classificationTypes, ImmutableArray<int> classificationTriples)
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<string> ClassificationTypes = classificationTypes;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<int> ClassificationTriples = classificationTriples;

        internal static SerializableClassifiedSpans Dehydrate(ImmutableArray<ClassifiedSpan> classifiedSpans)
        {
            using var _ = PooledDictionary<string, int>.GetInstance(out var classificationTypeToId);
            return Dehydrate(classifiedSpans, classificationTypeToId);
        }

        private static SerializableClassifiedSpans Dehydrate(ImmutableArray<ClassifiedSpan> classifiedSpans, Dictionary<string, int> classificationTypeToId)
        {
            using var _1 = ArrayBuilder<string>.GetInstance(out var classificationTypes);
            using var _2 = ArrayBuilder<int>.GetInstance(capacity: classifiedSpans.Length * 3, out var classificationTriples);

            foreach (var classifiedSpan in classifiedSpans)
            {
                var type = classifiedSpan.ClassificationType;
                if (!classificationTypeToId.TryGetValue(type, out var id))
                {
                    id = classificationTypes.Count;
                    classificationTypes.Add(type);
                    classificationTypeToId.Add(type, id);
                }

                var textSpan = classifiedSpan.TextSpan;
                classificationTriples.Add(id);
                classificationTriples.Add(textSpan.Start);
                classificationTriples.Add(textSpan.Length);
            }

            return new SerializableClassifiedSpans(
                classificationTypes.ToImmutableAndClear(),
                classificationTriples.ToImmutableAndClear());
        }

        internal void Rehydrate(SegmentedList<ClassifiedSpan> classifiedSpans)
        {
            for (int i = 0, n = ClassificationTriples.Length; i < n; i += 3)
            {
                classifiedSpans.Add(new ClassifiedSpan(
                    ClassificationTypes[ClassificationTriples[i + 0]],
                    new TextSpan(
                        ClassificationTriples[i + 1],
                        ClassificationTriples[i + 2])));
            }
        }
    }
}
