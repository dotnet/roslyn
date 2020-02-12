// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface IRemoteExtensionMethodImportCompletionService
    {
        Task<(IList<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            int position,
            string receiverTypeSymbolKeyData,
            string[] namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken);
    }
}
