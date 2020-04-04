// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Represents an <see cref="ISymbol"/> known to have originated from a particular <see cref="Project"/>. <see
    /// name="Symbol"/> will either be a source symbol from <see name="Project"/> or a metadata symbol from one of <see
    /// name="Project"/>'s <see cref="Project.MetadataReferences"/>.
    /// </summary>
    /// <remarks>
    /// For purposes of Equality/Hashing, all that is uses is the underlying <see cref="Symbol"/>.  In general, most
    /// features only care if they're looking at the same symbol, and do not care if the symbol came from a different
    /// project or not.  For example, most features will view <c>System.String</c> from the metadata from one project
    /// equivalent to <c>System.String</c> from the metadata from another project.
    /// </remarks>
    public readonly struct SymbolDefinition : IEquatable<SymbolDefinition>
    {
        public ISymbol Symbol { get; }
        public Project Project { get; }

        internal SymbolDefinition(ISymbol symbol, Project project)
        {
            Symbol = symbol;
            Project = project;
        }

        public override bool Equals(object obj)
            => obj is SymbolDefinition symbolDefinition && Equals(symbolDefinition);

        public bool Equals(SymbolDefinition other)
        {
            // See class comment on why we only use Symbol and ignore ProjectId.
            return Equals(this.Symbol, other.Symbol);
        }

        public override int GetHashCode()
        {
            // See class comment on why we only use Symbol and ignore ProjectId.
            return this.Symbol.GetHashCode();
        }

        internal static SymbolDefinition Create(Solution solution, SymbolAndProjectId symbolAndProjectId)
            => new SymbolDefinition(symbolAndProjectId.Symbol, solution.GetProject(symbolAndProjectId.ProjectId));

        internal static ImmutableArray<SymbolDefinition> Create(Solution solution, ImmutableArray<SymbolAndProjectId> array)
        {
            using var _ = ArrayBuilder<SymbolDefinition>.GetInstance(out var result);
            foreach (var symbolAndProjectId in array)
                result.Add(Create(solution, symbolAndProjectId));

            return result.ToImmutable();
        }

        internal static ImmutableArray<SymbolDefinition> Create<TSymbol>(Solution solution, ImmutableArray<SymbolAndProjectId<TSymbol>> array) where TSymbol : ISymbol
        {
            using var _ = ArrayBuilder<SymbolDefinition>.GetInstance(out var result);
            foreach (var symbolAndProjectId in array)
                result.Add(Create(solution, symbolAndProjectId));

            return result.ToImmutable();
        }
    }
}
