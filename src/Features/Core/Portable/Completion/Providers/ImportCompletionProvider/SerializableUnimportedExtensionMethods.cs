// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    [DataContract]
    internal sealed class SerializableUnimportedExtensionMethods(
        ImmutableArray<SerializableImportCompletionItem> completionItems,
        bool isPartialResult,
        TimeSpan getSymbolsTime,
        TimeSpan createItemsTime,
        TimeSpan? remoteAssetSyncTime)
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<SerializableImportCompletionItem> CompletionItems = completionItems;

        [DataMember(Order = 1)]
        public readonly bool IsPartialResult = isPartialResult;

        [DataMember(Order = 2)]
        public TimeSpan GetSymbolsTime { get; set; } = getSymbolsTime;

        [DataMember(Order = 3)]
        public readonly TimeSpan CreateItemsTime = createItemsTime;

        [DataMember(Order = 4)]
        public readonly TimeSpan? RemoteAssetSyncTime = remoteAssetSyncTime;
    }
}
