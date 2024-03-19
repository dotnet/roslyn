// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

internal interface IFindUsagesContext
{
    /// <summary>
    /// Used for clients that are finding usages to push information about how far along they
    /// are in their search.
    /// </summary>
    IStreamingProgressTracker ProgressTracker { get; }

    /// <summary>
    /// Report a message that the find operation returned no results.
    /// </summary>
    ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Report a message to be displayed to the user. This may appear to the user in the results
    /// UI in some fashion (for example: in an info-bar).
    /// </summary>
    ValueTask ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken);

    /// <summary>
    /// Set the title of the window that results are displayed in.
    /// </summary>
    ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken);

    ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken);
    ValueTask OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken);
}
