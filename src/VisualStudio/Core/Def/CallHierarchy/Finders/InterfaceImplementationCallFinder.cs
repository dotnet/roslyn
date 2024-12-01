// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy.Finders;

internal class InterfaceImplementationCallFinder : AbstractCallFinder
{
    private readonly string _text;

    public InterfaceImplementationCallFinder(ISymbol symbol, ProjectId projectId, IAsynchronousOperationListener asyncListener, CallHierarchyProvider provider)
        : base(symbol, projectId, asyncListener, provider)
    {
        _text = string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, symbol.ToDisplayString());
    }

    public override string DisplayName => _text;

    public override string SearchCategory => CallHierarchyPredefinedSearchCategoryNames.InterfaceImplementations;

    protected override async Task<IEnumerable<SymbolCallerInfo>> GetCallersAsync(ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
    {
        var calls = await SymbolFinder.FindCallersAsync(symbol, project.Solution, documents, cancellationToken).ConfigureAwait(false);
        return calls.Where(c => c.IsDirect);
    }
}
