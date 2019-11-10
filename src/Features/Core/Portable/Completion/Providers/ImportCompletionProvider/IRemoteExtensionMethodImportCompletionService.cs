// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface IRemoteExtensionMethodImportCompletionService
    {
        Task<(IList<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsAsync(
            DocumentId documentId,
            int position,
            string receiverTypeSymbolKeyData,
            string[] namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken);
    }
}
