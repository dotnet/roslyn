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
    internal sealed partial class DeclaredSymbolInfoIndex
    {
        private static readonly ConditionalWeakTable<Document, DeclaredSymbolInfoIndex?> s_documentToIndex = new();
        private static readonly ConditionalWeakTable<DocumentId, DeclaredSymbolInfoIndex?> s_documentIdToIndex = new();

        private readonly DeclarationInfo _declarationInfo;
        private readonly ExtensionMethodInfo _extensionMethodInfo;

        private readonly Lazy<HashSet<DeclaredSymbolInfo>> _declaredSymbolInfoSet;

        private DeclaredSymbolInfoIndex(
            Checksum checksum,
            DeclarationInfo declarationInfo,
            ExtensionMethodInfo extensionMethodInfo)
        {
            this.Checksum = checksum;
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

        public static async ValueTask<DeclaredSymbolInfoIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
        {
            var index = await GetIndexAsync(document, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(index);
            return index;
        }

        public static ValueTask<DeclaredSymbolInfoIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly: false, cancellationToken);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        public static async ValueTask<DeclaredSymbolInfoIndex?> GetIndexAsync(
            Document document,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
                return null;

            // See if we already cached an index with this direct document index.  If so we can just
            // return it with no additional work.
            if (!s_documentToIndex.TryGetValue(document, out var index))
            {
                index = await GetIndexWorkerAsync(document, loadOnly, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(index != null || loadOnly == true, "Result can only be null if 'loadOnly: true' was passed.");

                if (index == null && loadOnly)
                {
                    return null;
                }

                // Populate our caches with this data.
                s_documentToIndex.GetValue(document, _ => index);
                s_documentIdToIndex.Remove(document.Id);
                s_documentIdToIndex.GetValue(document.Id, _ => index);
            }

            return index;
        }

        private static async Task<DeclaredSymbolInfoIndex?> GetIndexWorkerAsync(
            Document document,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
                return null;

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // Check if we have an index for a previous version of this document.  If our
            // checksums match, we can just use that.
            if (s_documentIdToIndex.TryGetValue(document.Id, out var index) &&
                index?.Checksum == checksum)
            {
                // The previous index we stored with this documentId is still valid.  Just
                // return that.
                return index;
            }

            // What we have in memory isn't valid.  Try to load from the persistence service.
            index = await LoadAsync(document, checksum, cancellationToken).ConfigureAwait(false);
            if (index != null || loadOnly)
            {
                return index;
            }

            // alright, we don't have cached information, re-calculate them here.
            index = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await index.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return index;
        }

        private static async Task<DeclaredSymbolInfoIndex> CreateIndexAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.SupportsSyntaxTree);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return CreateIndex(document, root, checksum, cancellationToken);
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

        private readonly struct ExtensionMethodInfo
        {
            // We divide extension methods into two categories, simple and complex, for filtering purpose.
            // Whether a method is simple is determined based on if we can determine it's receiver type easily
            // with a pure text matching. For complex methods, we will need to rely on symbol to decide if it's 
            // feasible.
            //
            // Complex methods include:
            // - Method declared in the document which includes using alias directive
            // - Generic method where the receiver type is a type-paramter (e.g. List<T> would be considered simple, not complex)
            // - If the receiver type name is Pointer type (i.e. name of the type for the first parameter) 
            //
            // The rest of methods are considered simple.

            /// <summary>
            /// Name of the extension method's receiver type to the index of its DeclaredSymbolInfo in `_declarationInfo`.
            /// 
            /// For simple types, the receiver type name is it's metadata name. All predefined types are converted to its metadata form.
            /// e.g. int => Int32. For generic types, type parameters are ignored.
            /// 
            /// For complex types, the receiver type name is "".
            /// 
            /// For any kind of array types, it's "{element's receiver type name}[]".
            /// e.g. 
            /// int[][,] => "Int32[]"
            /// T (where T is a type parameter) => ""
            /// T[,] (where T is a type parameter) => "T[]"
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap { get; }

            public bool ContainsExtensionMethod => !ReceiverTypeNameToExtensionMethodMap.IsEmpty;

            public ExtensionMethodInfo(ImmutableDictionary<string, ImmutableArray<int>> receiverTypeNameToExtensionMethodMap)
            {
                ReceiverTypeNameToExtensionMethodMap = receiverTypeNameToExtensionMethodMap;
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(ReceiverTypeNameToExtensionMethodMap.Count);

                foreach (var (name, indices) in ReceiverTypeNameToExtensionMethodMap)
                {
                    writer.WriteString(name);
                    writer.WriteInt32(indices.Length);

                    foreach (var declaredSymbolInfoIndex in indices)
                        writer.WriteInt32(declaredSymbolInfoIndex);
                }
            }

            public static ExtensionMethodInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var receiverTypeNameToExtensionMethodMapBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<int>>();
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; ++i)
                    {
                        var typeName = reader.ReadString();
                        var arrayLength = reader.ReadInt32();
                        var arrayBuilder = ArrayBuilder<int>.GetInstance(arrayLength);

                        for (var j = 0; j < arrayLength; ++j)
                        {
                            var declaredSymbolInfoIndex = reader.ReadInt32();
                            arrayBuilder.Add(declaredSymbolInfoIndex);
                        }

                        receiverTypeNameToExtensionMethodMapBuilder[typeName] = arrayBuilder.ToImmutableAndFree();
                    }

                    return new ExtensionMethodInfo(receiverTypeNameToExtensionMethodMapBuilder.ToImmutable());
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
