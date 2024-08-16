// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal interface IRemoteSemanticClassificationService
{
    /// <summary>
    /// Gets the cached semantic classifications for the specified document and text spans.
    /// </summary>
    /// <param name="solutionChecksum">The checksum of the solution containing the document.</param>
    /// <param name="documentId">The ID of the document to get classified spans for.</param>
    /// <param name="textSpans">The non-intersecting portions of the document to get classified spans for.</param>
    /// <param name="type">The type of classified spans to get.</param>
    /// <param name="options">The options to use when getting classified spans.</param>
    /// <param name="isFullyLoaded">Whether or not the document is fully loaded.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The classified spans for the specified document and text spans.</returns>
    ValueTask<SerializableClassifiedSpans> GetClassificationsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        ImmutableArray<TextSpan> textSpans,
        ClassificationType type,
        ClassificationOptions options,
        bool isFullyLoaded,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tries to get cached semantic classifications for the specified document and the specified <paramref
    /// name="textSpans"/>.  Will return an empty array not able to.
    /// </summary>
    /// <param name="documentKey">The key of the document to get cached classified spans for.</param>
    /// <param name="textSpans">The non-intersecting portions of the document to get cached classified spans for.</param>
    /// <param name="type">The type of classified spans to get.</param>
    /// <param name="checksum">Pass in <see cref="DocumentStateChecksums.Text"/>.  This will ensure that the cached
    /// classifications are only returned if they match the content the file currently has.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The cached classified spans for the specified document and text spans.</returns>
    ValueTask<SerializableClassifiedSpans?> GetCachedClassificationsAsync(
        DocumentKey documentKey,
        ImmutableArray<TextSpan> textSpans,
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
        using var _1 = PooledDictionary<string, int>.GetInstance(out var classificationTypeToId);
        using var _2 = ArrayBuilder<string>.GetInstance(out var classificationTypes);
        var classificationTriples = new FixedSizeArrayBuilder<int>(classifiedSpans.Length * 3);

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
            classificationTriples.MoveToImmutable());
    }

    internal void Rehydrate(SegmentedList<ClassifiedSpan> classifiedSpans)
    {
        classifiedSpans.EnsureCapacity(classifiedSpans.Count + (ClassificationTriples.Length / 3));
        for (int i = 0, n = ClassificationTriples.Length; i < n; i += 3)
            classifiedSpans.Add(GetClassifiedSpanAt(i));
    }

    internal ImmutableArray<ClassifiedSpan> Rehydrate()
    {
        var classifiedSpans = new FixedSizeArrayBuilder<ClassifiedSpan>(this.ClassificationTriples.Length / 3);

        for (int i = 0, n = ClassificationTriples.Length; i < n; i += 3)
            classifiedSpans.Add(GetClassifiedSpanAt(i));

        return classifiedSpans.MoveToImmutable();
    }

    private ClassifiedSpan GetClassifiedSpanAt(int index)
        => new(
            ClassificationTypes[ClassificationTriples[index + 0]],
            new TextSpan(
                ClassificationTriples[index + 1],
                ClassificationTriples[index + 2]));
}
