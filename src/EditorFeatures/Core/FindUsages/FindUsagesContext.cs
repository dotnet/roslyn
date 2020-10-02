// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal abstract class FindUsagesContext : IFindUsagesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        public IStreamingProgressTracker ProgressTracker { get; }

        protected FindUsagesContext()
            => this.ProgressTracker = new StreamingProgressTracker((current, max) => this.ReportProgressAsync(current, max).AsTask());

        public virtual ValueTask ReportMessageAsync(string message) => default;

        public virtual ValueTask SetSearchTitleAsync(string title) => default;

        public virtual ValueTask OnCompletedAsync() => default;

        public virtual ValueTask OnDefinitionFoundAsync(DefinitionItem definition) => default;

        public virtual ValueTask OnReferenceFoundAsync(SourceReferenceItem reference) => default;

        protected virtual ValueTask ReportProgressAsync(int current, int maximum) => default;

        ValueTask IFindUsagesContext.ReportProgressAsync(int current, int maximum)
            => ReportProgressAsync(current, maximum);
    }
}
