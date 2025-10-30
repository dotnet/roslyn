// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Test.Utilities.FindUsages;

internal sealed class FindUsagesTestContext : FindUsagesContext
{
    private readonly object _gate = new();

    public readonly List<DefinitionItem> Definitions = [];
    public readonly List<SourceReferenceItem> References = [];

    public bool ShouldShow(DefinitionItem definition)
    {
        if (References.Any(r => r.Definition == definition))
            return true;

        return definition.DisplayIfNoReferences;
    }

    public override ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Definitions.Add(definition);
        }

        return default;
    }

    public override async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
    {
        await foreach (var reference in references)
        {
            lock (_gate)
            {
                References.Add(reference);
            }
        }
    }
}
