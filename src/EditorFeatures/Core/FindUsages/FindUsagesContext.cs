﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal abstract class FindUsagesContext : IFindUsagesContext
    {
        public IStreamingProgressTracker ProgressTracker { get; }

        protected FindUsagesContext()
            => this.ProgressTracker = new StreamingProgressTracker(this.ReportProgressAsync);

        public virtual ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken) => default;

        public virtual ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken) => default;

        public virtual ValueTask OnCompletedAsync(CancellationToken cancellationToken) => default;

        public virtual ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken) => default;

        public virtual ValueTask OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken) => default;

        protected virtual ValueTask ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken) => default;
    }
}
