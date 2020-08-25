// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface ISemanticClassificationCacheService
    {
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
