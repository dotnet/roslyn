// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

internal abstract class FindUsagesContext : IFindUsagesContext
{
    public IStreamingProgressTracker ProgressTracker { get; }

    protected FindUsagesContext()
    {
        ProgressTracker = new StreamingProgressTracker(ReportProgressAsync);
    }

    public virtual ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken) => default;

    public virtual ValueTask ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken) => default;

    public virtual ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken) => default;

    public virtual ValueTask OnCompletedAsync(CancellationToken cancellationToken) => default;

    public virtual ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken) => default;

    public virtual ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken) => default;

    protected virtual ValueTask ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken) => default;
}
