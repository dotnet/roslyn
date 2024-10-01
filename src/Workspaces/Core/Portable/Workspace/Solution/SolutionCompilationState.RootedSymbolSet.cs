// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis;

using SecondaryReferencedSymbol = (int hashCode, ISymbol symbol, SolutionCompilationState.MetadataReferenceInfo referenceInfo);

internal sealed partial class SolutionCompilationState
{
    internal readonly record struct MetadataReferenceInfo(MetadataReferenceProperties Properties, string? FilePath)
    {
        internal static MetadataReferenceInfo From(MetadataReference reference)
            => new(reference.Properties, (reference as PortableExecutableReference)?.FilePath);
    }

    /// <summary>
    /// Information maintained for unrooted symbols. 
    /// </summary>
    /// <param name="ProjectId">
    /// The project the symbol originated from, i.e. the symbol is defined in the project or its metadata reference.
    /// </param>
    /// <param name="Compilation">
    /// The Compilation that produced the symbol.
    /// </param>
    /// <param name="ReferencedThrough">
    /// If the symbol is defined in a metadata reference of <paramref name="ProjectId"/>, information about the
    /// reference.
    /// </param>
    internal sealed record class OriginatingProjectInfo(
        ProjectId ProjectId,
        Compilation? Compilation,
        MetadataReferenceInfo? ReferencedThrough);

    /// <summary>
    /// A helper type for mapping <see cref="ISymbol"/> back to an originating <see cref="Project"/>/<see
    /// cref="Compilation"/>.
    /// </summary>
    /// <remarks>
    /// In IDE scenarios we have the need to map from an <see cref="ISymbol"/> to the <see cref="Project"/> that
    /// contained a <see cref="Compilation"/> that could have produced that symbol.  This is especially needed with OOP
    /// scenarios where we have to communicate to OOP from VS (And vice versa) what symbol we are referring to. To do
    /// this, we pass along a project where this symbol could be found, and enough information (a <see
    /// cref="SymbolKey"/>) to resolve that symbol back in that that <see cref="Project"/>.
    /// </remarks>
    private readonly struct RootedSymbolSet
    {
        public readonly Compilation Compilation;

        /// <summary>
        /// The <see cref="IAssemblySymbol"/>s or <see cref="IModuleSymbol"/>s produced through <see
        /// cref="Compilation.GetAssemblyOrModuleSymbol(MetadataReference)"/> for all the references exposed by <see
        /// cref="Compilation.References"/>.  Sorted by the hash code produced by <see
        /// cref="ReferenceEqualityComparer.GetHashCode(object?)"/> so that it can be binary searched efficiently.
        /// </summary>
        public readonly ImmutableArray<SecondaryReferencedSymbol> SecondaryReferencedSymbols;

        private RootedSymbolSet(
            Compilation compilation,
            ImmutableArray<SecondaryReferencedSymbol> secondaryReferencedSymbols)
        {
            Compilation = compilation;
            SecondaryReferencedSymbols = secondaryReferencedSymbols;
        }

        public static RootedSymbolSet Create(Compilation compilation)
        {
            // PERF: Preallocate this array so we don't have to resize it as we're adding assembly symbols.
            using var _ = ArrayBuilder<SecondaryReferencedSymbol>.GetInstance(
                compilation.ExternalReferences.Length + compilation.DirectiveReferences.Length, out var secondarySymbols);

            foreach (var reference in compilation.References)
            {
                var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                if (symbol == null)
                    continue;

                secondarySymbols.Add((ReferenceEqualityComparer.GetHashCode(symbol), symbol, MetadataReferenceInfo.From(reference)));
            }

            // Sort all the secondary symbols by their hash.  This will allow us to easily binary search for them
            // afterwards. Note: it is fine for multiple symbols to have the same reference hash.  The search algorithm
            // will account for that.
            secondarySymbols.Sort(static (x, y) => x.hashCode.CompareTo(y.hashCode));
            return new RootedSymbolSet(compilation, secondarySymbols.ToImmutable());
        }

        public bool ContainsAssemblyOrModuleOrDynamic(
            ISymbol symbol, bool primary,
            [NotNullWhen(true)] out Compilation? compilation,
            out MetadataReferenceInfo? referencedThrough)
        {
            if (primary)
            {
                if (this.Compilation.Assembly.Equals(symbol))
                {
                    compilation = this.Compilation;
                    referencedThrough = null;
                    return true;
                }

                if (this.Compilation.Language == LanguageNames.CSharp &&
                    this.Compilation.DynamicType.Equals(symbol))
                {
                    compilation = this.Compilation;
                    referencedThrough = null;
                    return true;
                }
            }
            else
            {
                var secondarySymbols = this.SecondaryReferencedSymbols;

                var symbolHash = ReferenceEqualityComparer.GetHashCode(symbol);

                // The secondary symbol array is sorted by the symbols' hash codes.  So do a binary search to find
                // the location we should start looking at.
                var index = secondarySymbols.BinarySearch(symbolHash, static (item, symbolHash) => item.hashCode.CompareTo(symbolHash));
                if (index >= 0)
                {
                    // Could have multiple symbols with the same hash.  They will all be placed next to each other,
                    // so walk backward to hit the first.
                    while (index > 0 && secondarySymbols[index - 1].hashCode == symbolHash)
                        index--;

                    // Now, walk forward through the stored symbols with the same hash looking to see if any are a reference match.
                    while (index < secondarySymbols.Length && secondarySymbols[index].hashCode == symbolHash)
                    {
                        var cached = secondarySymbols[index];
                        if (cached.symbol.Equals(symbol))
                        {
                            referencedThrough = cached.referenceInfo;
                            compilation = this.Compilation;
                            return true;
                        }

                        index++;
                    }
                }
            }

            compilation = null;
            referencedThrough = null;
            return false;
        }
    }
}
