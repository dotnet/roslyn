// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public virtual Task ReportMessageAsync(string message) => SpecializedTasks.EmptyTask;

        public virtual Task SetSearchTitleAsync(string title) => SpecializedTasks.EmptyTask;

        public virtual Task OnCompletedAsync() => SpecializedTasks.EmptyTask;

        public virtual Task OnDefinitionFoundAsync(DefinitionItem definition) => SpecializedTasks.EmptyTask;

        public virtual Task OnReferenceFoundAsync(SourceReferenceItem reference) => SpecializedTasks.EmptyTask;

        public virtual Task ReportProgressAsync(int current, int maximum) => SpecializedTasks.EmptyTask;
    }
}
