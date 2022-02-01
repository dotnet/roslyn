// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class DeclaredSymbolInfoIndex : AbstractSyntaxIndex<DeclaredSymbolInfoIndex>
    {
        private readonly DeclarationInfo _declarationInfo;
        private readonly ExtensionMethodInfo _extensionMethodInfo;

        private readonly Lazy<HashSet<DeclaredSymbolInfo>> _declaredSymbolInfoSet;

        private DeclaredSymbolInfoIndex(
            Checksum? checksum,
            DeclarationInfo declarationInfo,
            ExtensionMethodInfo extensionMethodInfo)
            : base(checksum)
        {
            _declarationInfo = declarationInfo;
            _extensionMethodInfo = extensionMethodInfo;

            _declaredSymbolInfoSet = new(() => new(this.DeclaredSymbolInfos));
        }

        public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos => _declarationInfo.DeclaredSymbolInfos;

        /// <summary>
        /// Same as <see cref="DeclaredSymbolInfos"/>, just stored as a set for easy containment checks.
        /// </summary>
        public HashSet<DeclaredSymbolInfo> DeclaredSymbolInfoSet => _declaredSymbolInfoSet.Value;

        public ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap
            => _extensionMethodInfo.ReceiverTypeNameToExtensionMethodMap;

        public bool ContainsExtensionMethod => _extensionMethodInfo.ContainsExtensionMethod;

        public static ValueTask<DeclaredSymbolInfoIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
            => GetRequiredIndexAsync(document, ReadIndex, CreateIndex, cancellationToken);

        public static ValueTask<DeclaredSymbolInfoIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, ReadIndex, CreateIndex, cancellationToken);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        public static ValueTask<DeclaredSymbolInfoIndex?> GetIndexAsync(
            Document document, bool loadOnly, CancellationToken cancellationToken)
        {
            return GetIndexAsync(document, loadOnly, ReadIndex, CreateIndex, cancellationToken);
        }

        private static DeclaredSymbolInfoIndex CreateIndex(
            Document document, SyntaxNode root, Checksum checksum, CancellationToken cancellationToken)
        {
            var infoFactory = document.GetRequiredLanguageService<IDeclaredSymbolInfoFactoryService>();

            using var _1 = ArrayBuilder<DeclaredSymbolInfo>.GetInstance(out var declaredSymbolInfos);
            using var _2 = PooledDictionary<string, ArrayBuilder<int>>.GetInstance(out var extensionMethodInfo);
            try
            {
                infoFactory.AddDeclaredSymbolInfos(
                    document, root, declaredSymbolInfos, extensionMethodInfo, cancellationToken);

                return new DeclaredSymbolInfoIndex(
                    checksum,
                    new DeclarationInfo(declaredSymbolInfos.ToImmutable()),
                    new ExtensionMethodInfo(
                        extensionMethodInfo.ToImmutableDictionary(
                            static kvp => kvp.Key,
                            static kvp => kvp.Value.ToImmutable())));
            }
            finally
            {
                foreach (var (_, builder) in extensionMethodInfo)
                    builder.Free();
            }
        }
    }
}
