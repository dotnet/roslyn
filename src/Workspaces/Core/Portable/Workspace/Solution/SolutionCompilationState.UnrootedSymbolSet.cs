// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionCompilationState
    {
        /// <summary>
        /// A helper type for mapping <see cref="ISymbol"/> back to an originating <see cref="Project"/>.
        /// </summary>
        /// <remarks>
        /// In IDE scenarios we have the need to map from an <see cref="ISymbol"/> to the <see cref="Project"/> that
        /// contained a <see cref="Compilation"/> that could have produced that symbol.  This is especially needed with
        /// OOP scenarios where we have to communicate to OOP from VS (And vice versa) what symbol we are referring to.
        /// To do this, we pass along a project where this symbol could be found, and enough information (a <see
        /// cref="SymbolKey"/>) to resolve that symbol back in that that <see cref="Project"/>.
        /// <para>
        /// This is challenging however as symbols do not necessarily have back-pointers to <see cref="Compilation"/>s,
        /// and as such, we can't just see which Project produced the <see cref="Compilation"/> that produced that <see
        /// cref="ISymbol"/>.  In other words, the <see cref="ISymbol"/> doesn't <c>root</c> the compilation.  Because
        /// of that we keep track of those symbols per project in a <em>weak</em> fashion.  Then, we can later see if a
        /// symbol came from a particular project by checking if it is one of those weak symbols.  We use weakly held
        /// symbols to that a <see cref="ProjectState"/> instance doesn't hold symbols alive.  But, we know if we are
        /// holding the symbol itself, then the weak-ref will stay alive such that we can do this containment check.
        /// </para>
        /// </remarks>
        private readonly struct UnrootedSymbolSet
        {
            /// <summary>
            /// The <see cref="IAssemblySymbol"/> produced directly by <see cref="Compilation.Assembly"/>.
            /// </summary>
            public readonly WeakReference<IAssemblySymbol> PrimaryAssemblySymbol;

            /// <summary>
            /// The <see cref="IDynamicTypeSymbol"/> produced directly by <see cref="Compilation.DynamicType"/>.  Only
            /// valid for <see cref="LanguageNames.CSharp"/>.
            /// </summary>
            public readonly WeakReference<ITypeSymbol?> PrimaryDynamicSymbol;

            /// <summary>
            /// The <see cref="IAssemblySymbol"/>s or <see cref="IModuleSymbol"/>s produced through <see
            /// cref="Compilation.GetAssemblyOrModuleSymbol(MetadataReference)"/> for all the references exposed by <see
            /// cref="Compilation.References"/>.  Sorted by the hash code produced by <see
            /// cref="ReferenceEqualityComparer.GetHashCode(object?)"/> so that it can be binary searched efficiently.
            /// </summary>
            public readonly ImmutableArray<(int hashCode, WeakReference<ISymbol> symbol)> SecondaryReferencedSymbols;

            private UnrootedSymbolSet(
                WeakReference<IAssemblySymbol> primaryAssemblySymbol,
                WeakReference<ITypeSymbol?> primaryDynamicSymbol,
                ImmutableArray<(int hashCode, WeakReference<ISymbol> symbol)> secondaryReferencedSymbols)
            {
                PrimaryAssemblySymbol = primaryAssemblySymbol;
                PrimaryDynamicSymbol = primaryDynamicSymbol;
                SecondaryReferencedSymbols = secondaryReferencedSymbols;
            }

            public static UnrootedSymbolSet Create(Compilation compilation)
            {
                var primaryAssembly = new WeakReference<IAssemblySymbol>(compilation.Assembly);

                // The dynamic type is also unrooted (i.e. doesn't point back at the compilation or source
                // assembly).  So we have to keep track of it so we can get back from it to a project in case the 
                // underlying compilation is GC'ed.
                var primaryDynamic = new WeakReference<ITypeSymbol?>(
                    compilation.Language == LanguageNames.CSharp ? compilation.DynamicType : null);

                // PERF: Preallocate this array so we don't have to resize it as we're adding assembly symbols.
                using var _ = ArrayBuilder<(int hashcode, WeakReference<ISymbol> symbol)>.GetInstance(
                    compilation.ExternalReferences.Length + compilation.DirectiveReferences.Length, out var secondarySymbols);

                foreach (var reference in compilation.References)
                {
                    var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                    if (symbol == null)
                        continue;

                    secondarySymbols.Add((ReferenceEqualityComparer.GetHashCode(symbol), new WeakReference<ISymbol>(symbol)));
                }

                // Sort all the secondary symbols by their hash.  This will allow us to easily binary search for
                // them afterwards. Note: it is fine for multiple symbols to have the same reference hash.  The
                // search algorithm will account for that.
                secondarySymbols.Sort(WeakSymbolComparer.Instance);
                return new UnrootedSymbolSet(primaryAssembly, primaryDynamic, secondarySymbols.ToImmutable());
            }

            public bool ContainsAssemblyOrModuleOrDynamic(ISymbol symbol, bool primary)
            {
                if (primary)
                {
                    return symbol.Equals(this.PrimaryAssemblySymbol.GetTarget()) ||
                           symbol.Equals(this.PrimaryDynamicSymbol.GetTarget());
                }
                else
                {
                    var secondarySymbols = this.SecondaryReferencedSymbols;

                    var symbolHash = ReferenceEqualityComparer.GetHashCode(symbol);

                    // The secondary symbol array is sorted by the symbols' hash codes.  So do a binary search to find
                    // the location we should start looking at.
                    var index = secondarySymbols.BinarySearch((symbolHash, null!), WeakSymbolComparer.Instance);
                    if (index < 0)
                        return false;

                    // Could have multiple symbols with the same hash.  They will all be placed next to each other,
                    // so walk backward to hit the first.
                    while (index > 0 && secondarySymbols[index - 1].hashCode == symbolHash)
                        index--;

                    // Now, walk forward through the stored symbols with the same hash looking to see if any are a reference match.
                    while (index < secondarySymbols.Length && secondarySymbols[index].hashCode == symbolHash)
                    {
                        var cached = secondarySymbols[index].symbol;
                        if (cached.TryGetTarget(out var otherSymbol) && otherSymbol == symbol)
                            return true;

                        index++;
                    }

                    return false;
                }
            }

            private class WeakSymbolComparer : IComparer<(int hashcode, WeakReference<ISymbol> symbol)>
            {
                public static readonly WeakSymbolComparer Instance = new WeakSymbolComparer();

                private WeakSymbolComparer()
                {
                }

                public int Compare((int hashcode, WeakReference<ISymbol> symbol) x, (int hashcode, WeakReference<ISymbol> symbol) y)
                    => x.hashcode - y.hashcode;
            }
        }
    }
}
