// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TagHelperDiscoveryService : RazorEngineFeatureBase, ITagHelperDiscoveryService
{
    private ImmutableArray<ITagHelperProducerFactory> _producerFactories;

    protected override void OnInitialized()
    {
        _producerFactories = Engine.GetFeatures<ITagHelperProducerFactory>();
    }

    public TagHelperCollection GetTagHelpers(
        Compilation compilation,
        TagHelperDiscoveryOptions options,
        CancellationToken cancellationToken = default)
        => GetTagHelpersForCompilation(compilation, options, cancellationToken);

    public TagHelperCollection GetTagHelpers(
        Compilation compilation,
        CancellationToken cancellationToken = default)
        => GetTagHelpersForCompilation(compilation, options: default, cancellationToken);

    private TagHelperCollection GetTagHelpersForCompilation(
        Compilation compilation,
        TagHelperDiscoveryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(compilation);

        if (!TryGetDiscoverer(compilation, options, out var discoverer))
        {
            return TagHelperCollection.Empty;
        }

        using var collections = new MemoryBuilder<TagHelperCollection>(initialCapacity: 512, clearArray: true);

        if (compilation.Assembly is { } compilationAssembly)
        {
            var collection = discoverer.GetTagHelpers(compilationAssembly, cancellationToken);
            if (!collection.IsEmpty)
            {
                collections.Append(collection);
            }
        }

        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol referenceAssembly)
            {
                var collection = discoverer.GetTagHelpers(referenceAssembly, cancellationToken);
                if (!collection.IsEmpty)
                {
                    collections.Append(collection);
                }
            }
        }

        return TagHelperCollection.Merge(collections.AsMemory().Span);
    }

    public bool TryGetDiscoverer(Compilation compilation, TagHelperDiscoveryOptions options, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer)
    {
        ArgHelper.ThrowIfNull(compilation);

        var excludeHidden = options.IsFlagSet(TagHelperDiscoveryOptions.ExcludeHidden);
        var includeDocumentation = options.IsFlagSet(TagHelperDiscoveryOptions.IncludeDocumentation);

        var producers = GetProducers(compilation, includeDocumentation, excludeHidden);

        if (producers.IsEmpty)
        {
            discoverer = default;
            return false;
        }

        discoverer = new TagHelperDiscoverer(producers, includeDocumentation, excludeHidden);
        return true;
    }

    public bool TryGetDiscoverer(Compilation compilation, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer)
        => TryGetDiscoverer(compilation, options: default, out discoverer);

    private ImmutableArray<TagHelperProducer> GetProducers(Compilation compilation, bool includeDocumentation, bool excludeHidden)
    {
        if (_producerFactories.IsDefaultOrEmpty)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<TagHelperProducer>(_producerFactories.Length);

        foreach (var factory in _producerFactories)
        {
            if (factory.TryCreate(compilation, includeDocumentation, excludeHidden, out var producer))
            {
                builder.Add(producer);
            }
        }

        return builder.ToImmutableAndClear();
    }
}
