// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal sealed class ExtensionMemberImportCompletionCacheEntry
{
    public Checksum Checksum { get; }
    public string Language { get; }

    /// <summary>
    /// Mapping from the name of receiver type to extension method symbol infos.
    /// </summary>
    public MultiDictionary<string, DeclaredSymbolInfo> ReceiverTypeNameToExtensionMemberMap { get; }

    public bool ContainsExtensionMember => !ReceiverTypeNameToExtensionMemberMap.IsEmpty;

    private ExtensionMemberImportCompletionCacheEntry(
        Checksum checksum,
        string language,
        MultiDictionary<string, DeclaredSymbolInfo> receiverTypeNameToExtensionMemberMap)
    {
        Checksum = checksum;
        Language = language;
        ReceiverTypeNameToExtensionMemberMap = receiverTypeNameToExtensionMemberMap;
    }

    public sealed class Builder(Checksum checksum, string language, IEqualityComparer<string> comparer)
    {
        private readonly Checksum _checksum = checksum;
        private readonly string _language = language;

        private readonly MultiDictionary<string, DeclaredSymbolInfo> _mapBuilder = new(comparer);

        public ExtensionMemberImportCompletionCacheEntry ToCacheEntry()
            => new(_checksum, _language, _mapBuilder);

        public void AddItem(TopLevelSyntaxTreeIndex syntaxIndex)
        {
            foreach (var (receiverType, symbolInfoIndices) in syntaxIndex.ReceiverTypeNameToExtensionMemberMap)
            {
                foreach (var index in symbolInfoIndices)
                    _mapBuilder.Add(receiverType, syntaxIndex.DeclaredSymbolInfos[index]);
            }
        }
    }
}
