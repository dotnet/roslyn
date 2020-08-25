// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Service that can retrieve semantic classifications for a document cached during a previous session. This is
    /// intended to help populate semantic classifications for a host during the time while a solution is loading and
    /// semantics may be incomplete or unavailable.
    /// </summary>
    internal interface ISemanticClassificationCacheService : IWorkspaceService
    {
        /// <summary>
        /// Tries to get cached semantic classifications for the specified document and the specified <paramref
        /// name="textSpan"/>.  Will return an empty array not able to.
        /// </summary>
        /// <param name="checksum">Pass in <see cref="DocumentStateChecksums.Text"/>.  This will ensure that the cached
        /// classifications are only returned if they match the content the file currently has.</param>
        Task<ImmutableArray<ClassifiedSpan>> GetCachedSemanticClassificationsAsync(
            DocumentKey documentKey, TextSpan textSpan, Checksum checksum, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISemanticClassificationCacheService)), Shared]
    internal class DefaultSemanticClassificationCacheService : ISemanticClassificationCacheService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultSemanticClassificationCacheService()
        {
        }

        public Task<ImmutableArray<ClassifiedSpan>> GetCachedSemanticClassificationsAsync(DocumentKey documentKey, TextSpan textSpan, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<ClassifiedSpan>();
    }
}
