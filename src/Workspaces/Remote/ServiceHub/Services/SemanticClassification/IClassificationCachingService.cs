// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal interface IClassificationCachingService : IWorkspaceService
{
    ValueTask<SerializableClassifiedSpans> GetClassificationsAsync(
        Document document,
        ImmutableArray<TextSpan> spans,
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
        SolutionServices workspaceServices,
        DocumentKey documentKey,
        ImmutableArray<TextSpan> textSpans,
        ClassificationType type,
        Checksum checksum,
        CancellationToken cancellationToken);
}
