// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    /// <summary>
    /// Simple implementation of a <see cref="FindUsagesContext"/> that just aggregates the results
    /// for consumers that just want the data once it is finally computed.
    /// </summary>
    internal class SimpleFindUsagesContext : FindUsagesContext
    {
        private readonly object _gate = new object();
        private readonly ImmutableArray<DefinitionItem>.Builder _definitionItems =
            ImmutableArray.CreateBuilder<DefinitionItem>();

        private readonly ImmutableArray<SourceReferenceItem>.Builder _referenceItems =
            ImmutableArray.CreateBuilder<SourceReferenceItem>();

        public override CancellationToken CancellationToken { get; }

        public SimpleFindUsagesContext(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        public string Message { get; private set; }
        public string SearchTitle { get; private set; }

        public override Task ReportMessageAsync(string message)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public override Task SetSearchTitleAsync(string title)
        {
            SearchTitle = title;
            return Task.CompletedTask;
        }

        public ImmutableArray<DefinitionItem> GetDefinitions()
        {
            lock (_gate)
            {
                return _definitionItems.ToImmutable();
            }
        }

        public ImmutableArray<SourceReferenceItem> GetReferences()
        {
            lock (_gate)
            {
                return _referenceItems.ToImmutable();
            }
        }

        public override Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            lock (_gate)
            {
                _definitionItems.Add(definition);
            }

            return Task.CompletedTask;
        }

        public override Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            lock (_gate)
            {
                _referenceItems.Add(reference);
            }

            return Task.CompletedTask;
        }
    }
}
