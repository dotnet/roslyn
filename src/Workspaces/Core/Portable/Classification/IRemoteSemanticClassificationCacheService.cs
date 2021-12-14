//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Remote;
//using Microsoft.CodeAnalysis.Serialization;
//using Microsoft.CodeAnalysis.Storage;
//using Microsoft.CodeAnalysis.Text;

//namespace Microsoft.CodeAnalysis.Classification
//{
//    /// <summary>
//    /// Remote stubs used for a host to request cached semantic classifications from an OOP server.
//    /// </summary>
//    internal interface IRemoteSemanticClassificationCacheService
//    {
//        /// <remarks>
//        /// Note: this operates in a fire-and-forget fashion.  The request is sent tot he server, but will processed at
//        /// some time in the future.  Further requests to classify the same document will cause queued requests that
//        /// haven't been run to be abandoned.
//        /// </remarks>
//        ValueTask CacheSemanticClassificationsAsync(
//            PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken);

//        /// <summary>
//        /// Tries to get cached semantic classifications for the specified document and the specified <paramref
//        /// name="textSpan"/>.  Will return an empty array not able to.
//        /// </summary>
//        /// <param name="checksum">Pass in <see cref="DocumentStateChecksums.Text"/>.  This will ensure that the cached
//        /// classifications are only returned if they match the content the file currently has.</param>
//        ValueTask<SerializableClassifiedSpans?> GetCachedSemanticClassificationsAsync(
//            DocumentKey documentKey,
//            TextSpan textSpan,
//            Checksum checksum,
//            StorageDatabase database,
//            CancellationToken cancellationToken);
//    }
//}
