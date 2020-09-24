// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal interface IFindUsagesContext
    {
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Used for clients that are finding usages to push information about how far along they
        /// are in their search.
        /// </summary>
        IStreamingProgressTracker ProgressTracker { get; }

        /// <summary>
        /// Report a message to be displayed to the user.
        /// </summary>
        ValueTask ReportMessageAsync(string message);

        /// <summary>
        /// Set the title of the window that results are displayed in.
        /// </summary>
        ValueTask SetSearchTitleAsync(string title);

        ValueTask OnDefinitionFoundAsync(DefinitionItem definition);
        ValueTask OnReferenceFoundAsync(SourceReferenceItem reference);

        [Obsolete("Use ProgressTracker instead", error: false)]
        ValueTask ReportProgressAsync(int current, int maximum);
    }
}
