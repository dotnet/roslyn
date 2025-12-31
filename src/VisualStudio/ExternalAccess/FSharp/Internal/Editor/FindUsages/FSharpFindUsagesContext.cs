// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor.FindUsages;

internal class FSharpFindUsagesContext : IFSharpFindUsagesContext
{
    private readonly IFindUsagesContext _context;

    public FSharpFindUsagesContext(IFindUsagesContext context, CancellationToken cancellationToken)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public CancellationToken CancellationToken { get; }

    public Task OnDefinitionFoundAsync(FSharp.FindUsages.FSharpDefinitionItem definition)
    {
        return _context.OnDefinitionFoundAsync(definition.RoslynDefinitionItem, CancellationToken).AsTask();
    }

    public Task OnReferenceFoundAsync(FSharp.FindUsages.FSharpSourceReferenceItem reference)
    {
        return _context.OnReferencesFoundAsync(AsyncEnumerableFactory.SingletonAsync(reference.RoslynSourceReferenceItem), CancellationToken).AsTask();
    }

    public Task ReportMessageAsync(string message)
    {
        return _context.ReportNoResultsAsync(message, CancellationToken).AsTask();
    }

    public async Task ReportProgressAsync(int current, int maximum)
    {
    }

    public Task SetSearchTitleAsync(string title)
    {
        return _context.SetSearchTitleAsync(title, CancellationToken).AsTask();
    }
}
