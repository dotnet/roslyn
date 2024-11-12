// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal interface IRemoteExtensionMethodImportCompletionService
{
    ValueTask<SerializableUnimportedExtensionMethods?> GetUnimportedExtensionMethodsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        int position,
        string receiverTypeSymbolKeyData,
        ImmutableArray<string> namespaceInScope,
        ImmutableArray<string> targetTypesSymbolKeyData,
        bool forceCacheCreation,
        bool hideAdvancedMembers,
        CancellationToken cancellationToken);

    ValueTask WarmUpCacheAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);
}
