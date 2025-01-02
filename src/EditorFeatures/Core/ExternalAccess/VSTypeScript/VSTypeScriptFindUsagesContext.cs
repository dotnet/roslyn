// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

internal sealed class VSTypeScriptFindUsagesContext(FindUsagesContext underlyingObject) : IVSTypeScriptFindUsagesContext
{
    internal readonly FindUsagesContext UnderlyingObject = underlyingObject;

    public IVSTypeScriptStreamingProgressTracker ProgressTracker
        => new VSTypeScriptStreamingProgressTracker(UnderlyingObject.ProgressTracker);

    public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
        => UnderlyingObject.ReportNoResultsAsync(message, cancellationToken);

    public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
        => UnderlyingObject.SetSearchTitleAsync(title, cancellationToken);

    public ValueTask OnDefinitionFoundAsync(VSTypeScriptDefinitionItem definition, CancellationToken cancellationToken)
        => UnderlyingObject.OnDefinitionFoundAsync(definition.UnderlyingObject, cancellationToken);

    public ValueTask OnReferenceFoundAsync(VSTypeScriptSourceReferenceItem reference, CancellationToken cancellationToken)
        => UnderlyingObject.OnReferencesFoundAsync(IAsyncEnumerableExtensions.SingletonAsync(reference.UnderlyingObject), cancellationToken);

    public ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        => UnderlyingObject.OnCompletedAsync(cancellationToken);
}
