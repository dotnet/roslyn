// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal readonly struct ExtensionMethodImportCompletionCacheEntry
    {
        public Checksum Checksum { get; }
        public string Language { get; }

        /// <summary>
        /// Mapping from the name of receiver type to extension method symbol infos.
        /// </summary>
        public readonly MultiDictionary<string, DeclaredSymbolInfo> ReceiverTypeNameToExtensionMethodMap { get; }

        public bool ContainsExtensionMethod => !ReceiverTypeNameToExtensionMethodMap.IsEmpty;

        private ExtensionMethodImportCompletionCacheEntry(
            Checksum checksum,
            string language,
            MultiDictionary<string, DeclaredSymbolInfo> receiverTypeNameToExtensionMethodMap)
        {
            Checksum = checksum;
            Language = language;
            ReceiverTypeNameToExtensionMethodMap = receiverTypeNameToExtensionMethodMap;
        }

        public class Builder(Checksum checksum, string langauge, IEqualityComparer<string> comparer)
        {
            private readonly Checksum _checksum = checksum;
            private readonly string _language = langauge;

            private readonly MultiDictionary<string, DeclaredSymbolInfo> _mapBuilder = new MultiDictionary<string, DeclaredSymbolInfo>(comparer);

            public ExtensionMethodImportCompletionCacheEntry ToCacheEntry()
            {
                return new ExtensionMethodImportCompletionCacheEntry(
                    _checksum,
                    _language,
                    _mapBuilder);
            }

            public void AddItem(TopLevelSyntaxTreeIndex syntaxIndex)
            {
                foreach (var (receiverType, symbolInfoIndices) in syntaxIndex.ReceiverTypeNameToExtensionMethodMap)
                {
                    foreach (var index in symbolInfoIndices)
                    {
                        _mapBuilder.Add(receiverType, syntaxIndex.DeclaredSymbolInfos[index]);
                    }
                }
            }
        }
    }
}
