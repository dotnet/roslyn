// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TagHelperDiscoverer(ImmutableArray<TagHelperProducer> producers, bool includeDocumentation, bool excludeHidden)
{
    private readonly int _cacheKey = GetCacheKey(producers, includeDocumentation, excludeHidden);

    /// <summary>
    ///  Generates a unique integer cache key based on the specified set of TagHelper producers and option flags.
    /// </summary>
    /// <remarks>
    ///  The generated cache key is intended for efficient lookup scenarios where the combination of
    ///  producers and options must be uniquely identified. The method supports up to 30 distinct TagHelperProducer
    ///  kinds.
    /// </remarks>
    private static int GetCacheKey(ImmutableArray<TagHelperProducer> producers, bool includeDocumentation, bool excludeHidden)
    {
        Debug.Assert(producers.Length <= 30, "Too many TagHelperProducer kinds to fit in a cache key.");

        var key = 0;

        if (includeDocumentation)
        {
            key |= 1 << 0;
        }

        if (excludeHidden)
        {
            key |= 1 << 1;
        }

        foreach (var producer in producers)
        {
            key |= 1 << ((int)producer.Kind + 2);
        }

        return key;
    }

    public TagHelperCollection GetTagHelpers(IAssemblySymbol assembly, CancellationToken cancellationToken = default)
    {
        if (producers.IsDefaultOrEmpty)
        {
            return TagHelperCollection.Empty;
        }

        // Optimization: Check to see if this assembly might contain tag helpers before doing any work.
        var assemblySymbolData = SymbolCache.GetAssemblySymbolData(assembly);
        if (!assemblySymbolData.MightContainTagHelpers)
        {
            return TagHelperCollection.Empty;
        }

        // Check to see if we already have tag helpers cached for this assembly
        // and use the cached versions if we do. Roslyn shares PE assembly symbols
        // across compilations, so this ensures that we don't produce new tag helpers
        // for the same assemblies over and over again.

        if (assemblySymbolData.TryGetTagHelpers(_cacheKey, out var tagHelpers))
        {
            return tagHelpers;
        }

        // We don't have tag helpers cached for this assembly, so we have to discover them.
        var builder = new TagHelperCollection.RefBuilder();
        try
        {
            // First, let producers add any static tag helpers they might have.
            // Also, capture any producers that need to analyze types.
            using var _ = ArrayPool<TagHelperProducer>.Shared.GetPooledArraySpan(
                minimumLength: producers.Length, clearOnReturn: true, out var typeProducers);

            var index = 0;
            var includeNestedTypes = false;

            foreach (var producer in producers)
            {
                if (producer.SupportsStaticTagHelpers)
                {
                    producer.AddStaticTagHelpers(assembly, ref builder);
                }

                if (producer.SupportsTypes)
                {
                    typeProducers[index++] = producer;
                    includeNestedTypes |= producer.SupportsNestedTypes;
                }
            }

            typeProducers = typeProducers[..index];

            cancellationToken.ThrowIfCancellationRequested();

            // Did another discovery request for the same assembly finish and
            // cache the result while we were producing static tag helpers?
            if (assemblySymbolData.TryGetTagHelpers(_cacheKey, out tagHelpers))
            {
                return tagHelpers;
            }

            // Now, walk all types in the assembly and let producers add tag helpers.
            using var stack = new MemoryBuilder<INamespaceOrTypeSymbol>(initialCapacity: 32, clearArray: true);

            stack.Push(assembly.GlobalNamespace);

            while (!stack.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var namespaceOrType = stack.Pop();

                switch (namespaceOrType.Kind)
                {
                    case SymbolKind.Namespace:
                        var members = namespaceOrType.GetMembers();

                        // Note: Push members onto the stack in reverse to ensure
                        // that they're popped off and processed in the correct order.
                        for (var i = members.Length - 1; i >= 0; i--)
                        {
                            // Namespaces members are only ever namespaces or types.
                            stack.Push((INamespaceOrTypeSymbol)members[i]);
                        }

                        break;

                    case SymbolKind.NamedType:
                        var typeSymbol = (INamedTypeSymbol)namespaceOrType;

                        foreach (var producer in typeProducers)
                        {
                            if (producer.IsCandidateType(typeSymbol))
                            {
                                producer.AddTagHelpersForType(typeSymbol, ref builder, cancellationToken);
                            }
                        }

                        if (includeNestedTypes && namespaceOrType.DeclaredAccessibility == Accessibility.Public)
                        {
                            var typeMembers = namespaceOrType.GetTypeMembers();

                            // Note: Push members onto the stack in reverse to ensure
                            // that they're popped off and processed in the correct order.
                            for (var i = typeMembers.Length - 1; i >= 0; i--)
                            {
                                stack.Push(typeMembers[i]);
                            }
                        }

                        break;
                }
            }


            return assemblySymbolData.AddTagHelpers(_cacheKey, builder.ToCollection());
        }
        finally
        {
            builder.Dispose();
        }
    }
}
