// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Represents a symbol and the project it was acquired from.
    /// It should always be the case that if you have the original solution
    /// that this symbol came from, that you'll be able to find this symbol
    /// in the compilation for the specified project.
    /// 
    /// Note that the 'Same' symbol could be acquired from many different projects
    /// (after all, each project sees, at least, all the public symbols for all the 
    /// projects it references).  As such, a single ISymbol could be found in many
    /// places.  The ProjectId at least gives us a single place to look for it again.
    /// 
    /// The purpose of this type is to support serializing/deserializing symbols
    /// and allowing features to work out-of-process (OOP).  In OOP scenarios, 
    /// we will need to marshal <see cref="ISymbol"/>s to and from the host and 
    /// the external process.  That means being able to recover the <see cref="ISymbol"/> 
    /// on either side.  With the <see cref="ProjectId"/> this becomes possible.
    /// 
    /// Accordingly, it is ok to have a <see cref="SymbolAndProjectId"/> that does
    /// not have a <see cref="ProjectId"/>.  It just means that that data cannot
    /// be marshalled in an OOP scenario.  Existing features, and third party clients
    /// will then have code that still works (albeit just in-process).  However,
    /// code that updates to use this can then opt-into working OOP.
    /// 
    /// Note: for purposes of Equality/Hashing, all that we use is the underlying
    /// Symbol.  That's because nearly all IDE features only care if they're looking
    /// at the same symbol, they don't care if hte symbol came from a different 
    /// project or not.  i.e. a feature like FAR doesn't want to cascade into the 
    /// "same" symbol even if it hits it in another project.  As such, we do not
    /// include the ProjectId when computing the result.
    /// </summary>
    internal struct SymbolAndProjectId
    {
        public readonly ISymbol Symbol;
        public readonly ProjectId ProjectId;

        public SymbolAndProjectId(ISymbol symbol, ProjectId projectId)
        {
            Symbol = symbol;
            ProjectId = projectId;
        }

        public override bool Equals(object obj) => Equals((SymbolAndProjectId)obj);

        public bool Equals(SymbolAndProjectId other)
        {
            // See class comment on why we only use Symbol and ignore ProjectId.
            return Equals(this.Symbol, other.Symbol);
        }

        public override int GetHashCode()
        {
            // See class comment on why we only use Symbol and ignore ProjectId.
            return this.Symbol.GetHashCode();
        }

        public static SymbolAndProjectId Create(
            ISymbol symbol, ProjectId projectId)
        {
            return new SymbolAndProjectId(symbol, projectId);
        }

        public static SymbolAndProjectId<TSymbol> Create<TSymbol>(
            TSymbol symbol, ProjectId projectId) where TSymbol : ISymbol
        {
            return new SymbolAndProjectId<TSymbol>(symbol, projectId);
        }

        public SymbolAndProjectId<TOther> WithSymbol<TOther>(TOther other)
            where TOther : ISymbol
        {
            return new SymbolAndProjectId<TOther>(other, this.ProjectId);
        }

        public SymbolAndProjectId WithSymbol(ISymbol other)
        {
            return new SymbolAndProjectId(other, this.ProjectId);
        }
    }

    internal struct SymbolAndProjectId<TSymbol> where TSymbol : ISymbol
    {
        public readonly TSymbol Symbol;
        public readonly ProjectId ProjectId;

        public SymbolAndProjectId(TSymbol symbol, ProjectId projectId)
        {
            Symbol = symbol;
            ProjectId = projectId;
        }

        public static implicit operator SymbolAndProjectId(SymbolAndProjectId<TSymbol> value)
        {
            return new SymbolAndProjectId(value.Symbol, value.ProjectId);
        }

        public SymbolAndProjectId<TOther> WithSymbol<TOther>(TOther other)
            where TOther : ISymbol
        {
            return new SymbolAndProjectId<TOther>(other, this.ProjectId);
        }

        public SymbolAndProjectId WithSymbol(ISymbol other)
        {
            return new SymbolAndProjectId(other, this.ProjectId);
        }
    }

    internal static class SymbolAndProjectIdExtensions
    {
        public static IEnumerable<SymbolAndProjectId<TConvert>> Convert<TOriginal, TConvert>(
            this IEnumerable<SymbolAndProjectId<TOriginal>> list)
            where TOriginal : ISymbol
            where TConvert : ISymbol
        {
            return list.Select(s => SymbolAndProjectId.Create((TConvert)(object)s.Symbol, s.ProjectId));
        }
    }

    /// <summary>
    /// Provides a way for us to store and compare SymbolAndProjectId in the
    /// sets that we're using.  For the purposes of the operations in 
    /// <see cref="DependentTypeFinder"/> these entities are the same if they
    /// point to Symbols that are considered the same.  For example, if
    /// we find a derived type of 'X' called 'Y' in a metadata assembly 'M'
    /// in project A and we also find a derived type of 'X' called 'Y' in a 
    /// metadata assembly 'M' in project B, then we consider these the same.
    /// What project we were searching in does not matter to us in terms of
    /// deciding if these symbols are the same or not.  We're only keeping
    /// the projects to return to the caller information about what project
    /// we were searching when we found the symbol.
    /// </summary>
    internal class SymbolAndProjectIdComparer<TSymbol> : IEqualityComparer<SymbolAndProjectId<TSymbol>>
        where TSymbol : ISymbol
    {
        public static readonly SymbolAndProjectIdComparer<TSymbol> SymbolEquivalenceInstance = new SymbolAndProjectIdComparer<TSymbol>();

        /// <summary>
        /// Note(cyrusn): We're using SymbolEquivalenceComparer.Instance as the underlying 
        /// way of comparing symbols.  That's probably not correct as it won't appropriately
        /// deal with forwarded types.  However, that's the behavior that we've already had
        /// in this type for a while, so this is just preserving that logic.  If this is an 
        /// issue in the future, this underlying comparer can absolutely be changed to something
        /// more appropriate.
        /// </summary>
        private static readonly IEqualityComparer<ISymbol> _underlyingComparer =
            SymbolEquivalenceComparer.Instance;

        private SymbolAndProjectIdComparer()
        {
        }

        public bool Equals(SymbolAndProjectId<TSymbol> x, SymbolAndProjectId<TSymbol> y)
        {
            return _underlyingComparer.Equals(x.Symbol, y.Symbol);
        }

        public int GetHashCode(SymbolAndProjectId<TSymbol> obj)
        {
            return _underlyingComparer.GetHashCode(obj.Symbol);
        }
    }

    internal class SymbolAndProjectIdComparer : IEqualityComparer<SymbolAndProjectId>
    {
        /// <summary>
        /// Note(cyrusn): We're using SymbolEquivalenceComparer.Instance as the underlying 
        /// way of comparing symbols.  That's probably not correct as it won't appropriately
        /// deal with forwarded types.  However, that's the behavior that we've already had
        /// in this type for a while, so this is just preserving that logic.  If this is an 
        /// issue in the future, this underlying comparer can absolutely be changed to something
        /// more appropriate.
        /// </summary>
        public static readonly SymbolAndProjectIdComparer SymbolEquivalenceInstance =
            new SymbolAndProjectIdComparer(SymbolEquivalenceComparer.Instance);

        private readonly IEqualityComparer<ISymbol> _underlyingComparer;

        public SymbolAndProjectIdComparer(IEqualityComparer<ISymbol> underlyingComparer)
        {
            _underlyingComparer = underlyingComparer;
        }

        public bool Equals(SymbolAndProjectId x, SymbolAndProjectId y)
        {
            return _underlyingComparer.Equals(x.Symbol, y.Symbol);
        }

        public int GetHashCode(SymbolAndProjectId obj)
        {
            return _underlyingComparer.GetHashCode(obj.Symbol);
        }
    }
}
