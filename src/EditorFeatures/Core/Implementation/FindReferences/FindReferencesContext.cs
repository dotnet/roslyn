// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindReferences;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class FindReferencesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        protected FindReferencesContext()
        {
        }

        public virtual void SetSearchLabel(string displayName)
        {
        }

        public virtual Task OnCompletedAsync() => SpecializedTasks.EmptyTask;

        public virtual Task OnDefinitionFoundAsync(DefinitionItem definition) => SpecializedTasks.EmptyTask;

        public virtual Task OnReferenceFoundAsync(SourceReferenceItem reference) => SpecializedTasks.EmptyTask;

        public virtual Task ReportProgressAsync(int current, int maximum) => SpecializedTasks.EmptyTask;
    }
}