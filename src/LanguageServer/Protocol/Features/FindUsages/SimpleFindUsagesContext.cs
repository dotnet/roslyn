// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindUsages;

/// <summary>
/// Simple implementation of a <see cref="FindUsagesContext"/> that just aggregates the results
/// for consumers that just want the data once it is finally computed.
/// </summary>
internal sealed class SimpleFindUsagesContext : FindUsagesContext
{
    private readonly object _gate = new();

    private readonly ImmutableArray<DefinitionItem>.Builder _definitionItems =
        ImmutableArray.CreateBuilder<DefinitionItem>();

    private readonly ImmutableArray<SourceReferenceItem>.Builder _referenceItems =
        ImmutableArray.CreateBuilder<SourceReferenceItem>();

    public string Message { get; private set; }
    public string SearchTitle { get; private set; }

    public override ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken)
    {
        Message = message;
        return default;
    }

    public override ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
    {
        SearchTitle = title;
        return default;
    }

    public ImmutableArray<DefinitionItem> GetDefinitions()
    {
        lock (_gate)
        {
            return _definitionItems.ToImmutable();
        }
    }

    public ImmutableArray<SourceReferenceItem> GetReferences()
    {
        lock (_gate)
        {
            return _referenceItems.ToImmutable();
        }
    }

    public override ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _definitionItems.Add(definition);
        }

        return default;
    }

    public override async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
    {
        await foreach (var reference in references.ConfigureAwait(false))
        {
            lock (_gate)
            {
                _referenceItems.Add(reference);
            }
        }
    }
}
