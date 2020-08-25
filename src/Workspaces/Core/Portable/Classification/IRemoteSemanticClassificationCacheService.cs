// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IRemoteSemanticClassificationCacheService
    {
        Task<SerializableClassifiedSpans> GetCachedSemanticClassificationsAsync(
            SerializableDocumentKey documentKey,
            TextSpan textSpan,
            Checksum checksum,
            CancellationToken cancellationToken);

        Task StartCachingSemanticClassificationsAsync(CancellationToken cancellation);
    }
}
