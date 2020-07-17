// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class ExtensionMethodImportCompletionHelper
    {
        private readonly struct CacheEntry
        {
            public Checksum Checksum { get; }
            public string Language { get; }

            /// <summary>
            /// Mapping from the name of receiver type to extension method symbol infos.
            /// </summary>
            public readonly MultiDictionary<string, DeclaredSymbolInfo> ReceiverTypeNameToExtensionMethodMap { get; }

            public bool ContainsExtensionMethod => !ReceiverTypeNameToExtensionMethodMap.IsEmpty;

            private CacheEntry(
                Checksum checksum,
                string language,
                MultiDictionary<string, DeclaredSymbolInfo> receiverTypeNameToExtensionMethodMap)
            {
                Checksum = checksum;
                Language = language;
                ReceiverTypeNameToExtensionMethodMap = receiverTypeNameToExtensionMethodMap;
            }

            public class Builder
            {
                private readonly Checksum _checksum;
                private readonly string _language;

                private readonly MultiDictionary<string, DeclaredSymbolInfo> _mapBuilder;

                public Builder(Checksum checksum, string langauge, IEqualityComparer<string> comparer)
                {
                    _checksum = checksum;
                    _language = langauge;

                    _mapBuilder = new MultiDictionary<string, DeclaredSymbolInfo>(comparer);
                }

                public CacheEntry ToCacheEntry()
                {
                    return new CacheEntry(
                        _checksum,
                        _language,
                        _mapBuilder);
                }

                public void AddItem(SyntaxTreeIndex syntaxIndex)
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

        /// <summary>
        /// We don't use PE cache from the service, so just pass in type `object` for PE entries.
        /// </summary>
        [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<CacheEntry, object>), ServiceLayer.Editor), Shared]
        private sealed class CacheServiceFactory : AbstractImportCompletionCacheServiceFactory<CacheEntry, object>
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CacheServiceFactory()
            {
            }
        }

        private static IImportCompletionCacheService<CacheEntry, object> GetCacheService(Workspace workspace)
            => workspace.Services.GetRequiredService<IImportCompletionCacheService<CacheEntry, object>>();

        private static async Task<CacheEntry?> GetCacheEntryAsync(
            Project project,
            bool loadOnly,
            IImportCompletionCacheService<CacheEntry, object> cacheService,
            CancellationToken cancellationToken)
        {
            // While we are caching data from SyntaxTreeInfo, all the things we cared about here are actually based on sources symbols.
            // So using source symbol checksum would suffice.
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

            // Cache miss, create all requested items.
            if (!cacheService.ProjectItemsCache.TryGetValue(project.Id, out var cacheEntry) ||
                cacheEntry.Checksum != checksum ||
                cacheEntry.Language != project.Language)
            {
                var syntaxFacts = project.LanguageServices.GetRequiredService<ISyntaxFactsService>();
                var builder = new CacheEntry.Builder(checksum, project.Language, syntaxFacts.StringComparer);

                foreach (var document in project.Documents)
                {
                    // Don't look for extension methods in generated code.
                    if (document.State.Attributes.IsGenerated)
                    {
                        continue;
                    }

                    var info = await document.GetSyntaxTreeIndexAsync(loadOnly, cancellationToken).ConfigureAwait(false);
                    if (info == null)
                    {
                        return null;
                    }

                    if (info.ContainsExtensionMethod)
                    {
                        builder.AddItem(info);
                    }
                }

                cacheEntry = builder.ToCacheEntry();
                cacheService.ProjectItemsCache[project.Id] = cacheEntry;
            }

            return cacheEntry;
        }
    }
}
