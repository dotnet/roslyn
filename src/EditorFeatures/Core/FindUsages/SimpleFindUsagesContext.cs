// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => CancellationToken = cancellationToken;

        public string Message { get; private set; }
        public string SearchTitle { get; private set; }

        public override ValueTask ReportMessageAsync(string message)
        {
            Message = message;
            return default;
        }

        public override ValueTask SetSearchTitleAsync(string title)
        {
            SearchTitle = title;
            return default;
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

        public override ValueTask OnDefinitionFoundAsync(DefinitionItem definition)
        {
            lock (_gate)
            {
                _definitionItems.Add(definition);
            }

            return default;
        }

        public override ValueTask OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            lock (_gate)
            {
                _referenceItems.Add(reference);
            }

            return default;
        }
    }
}
