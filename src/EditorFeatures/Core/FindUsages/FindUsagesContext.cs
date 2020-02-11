// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal abstract class FindUsagesContext : IFindUsagesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        protected FindUsagesContext()
        {
        }

        public virtual Task ReportMessageAsync(string message) => Task.CompletedTask;

        public virtual Task SetSearchTitleAsync(string title) => Task.CompletedTask;

        public virtual Task OnCompletedAsync() => Task.CompletedTask;

        public virtual Task OnDefinitionFoundAsync(DefinitionItem definition) => Task.CompletedTask;

        public virtual Task OnReferenceFoundAsync(SourceReferenceItem reference) => Task.CompletedTask;

        public virtual Task ReportProgressAsync(int current, int maximum) => Task.CompletedTask;
    }
}
