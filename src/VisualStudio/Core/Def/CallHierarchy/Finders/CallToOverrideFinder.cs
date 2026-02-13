// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders;

internal sealed class CallToOverrideFinder : AbstractCallFinder
{
    public CallToOverrideFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
        : base(symbol, projectId, asyncListener, provider)
    {
    }

    public override string DisplayName => EditorFeaturesResources.Calls_To_Overrides;

    protected override Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
    {
        // Use shared helper to find callers to overrides
        return CallHierarchyHelpers.FindCallersToOverridesAsync(symbol, project.Solution, documents, cancellationToken)
            .ContinueWith(t => (IEnumerable<SymbolCallerInfo>)t.Result, cancellationToken);
    }
}
