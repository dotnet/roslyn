// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <para>
    /// A <see cref="SymbolKey"/> is a lightweight identifier for a symbol that can be used to 
    /// resolve the "same" symbol across compilations.  Different symbols have different concepts 
    /// of "same-ness". Same-ness is recursively defined as follows:
    /// <list type="number">
    ///   <item>Two <see cref="IArrayTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="IArrayTypeSymbol.ElementType"/> and 
    ///         equal <see cref="IArrayTypeSymbol.Rank"/>.</item>
    ///   <item>Two <see cref="IAssemblySymbol"/>s are the "same" if 
    ///         they have equal <see cref="IAssemblySymbol.Identity"/>.Name</item>
    ///   <item>Two <see cref="IEventSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingType"/> and 
    ///         equal <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IMethodSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingType"/>,
    ///         equal <see cref="ISymbol.MetadataName"/>,
    ///         equal <see cref="IMethodSymbol.Arity"/>, 
    ///         the "same" <see cref="IMethodSymbol.TypeArguments"/>, and have
    ///         the "same" <see cref="IParameterSymbol.Type"/>s and  
    ///         equal <see cref="IParameterSymbol.RefKind"/>s.</item>
    ///   <item>Two <see cref="IModuleSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingAssembly"/>.
    ///         <see cref="ISymbol.MetadataName"/> is not used because module identity is not important in practice.</item>
    ///   <item>Two <see cref="INamedTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/>,
    ///         equal <see cref="ISymbol.MetadataName"/>,
    ///         equal <see cref="INamedTypeSymbol.Arity"/> and 
    ///         the "same" <see cref="INamedTypeSymbol.TypeArguments"/>.</item>
    ///   <item>Two <see cref="INamespaceSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and
    ///         equal <see cref="ISymbol.MetadataName"/>.
    ///     If the <see cref="INamespaceSymbol"/> is the global namespace for a
    ///     compilation, then it will only match another
    ///     global namespace of another compilation.</item>
    ///   <item>Two <see cref="IParameterSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         equal <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IPointerTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="IPointerTypeSymbol.PointedAtType"/>.</item>
    ///   <item>Two <see cref="IPropertySymbol"/>s are the "same" if they have 
    ///         the "same" the "same" <see cref="ISymbol.ContainingType"/>, 
    ///         the "same" <see cref="ISymbol.MetadataName"/>, and have 
    ///         the "same" <see cref="IParameterSymbol.Type"/>s and  
    ///         the "same" <see cref="IParameterSymbol.RefKind"/>s.</item>
    ///   <item>Two <see cref="ITypeParameterSymbol"/> are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         the "same" <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IFieldSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         the "same" <see cref="ISymbol.MetadataName"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    ///     Due to issues arising from errors and ambiguity, it's possible for a SymbolKey to resolve to
    ///     multiple symbols. For example, in the following type:
    ///     <code>
    ///     class C
    ///     {
    ///        int M();
    ///        bool M();
    ///     }
    ///     </code>
    ///     The SymbolKey for both 'M' methods will be the same.  The SymbolKey will then resolve to both methods.
    /// </para>
    /// <para>
    /// <see cref="SymbolKey"/>s are not guaranteed to work across different versions of Roslyn.
    /// They can be persisted in their <see cref="ToString()"/> form and used across sessions with
    /// the same version of Roslyn. However, future versions may change the encoded format and may
    /// no longer be able to <see cref="Resolve(Compilation, bool, bool, CancellationToken)"/> previous
    /// keys.  As such, only persist if using for a cache that can be regenerated if necessary.
    /// </para>
    /// </summary>
    internal partial struct SymbolKey
    {
        /// <summary>
        /// Current format version.  Any time we change anything about our format, we should
        /// change this.  This will help us detect and reject any cases where a person serializes
        /// out a SymbolKey from a previous version of Roslyn and then attempt to use it in a 
        /// newer version where the encoding has changed.
        /// </summary>
        internal const int FormatVersion = 1;

        private readonly string _symbolKeyData;

        /// <summary>
        /// Constructs a new <see cref="SymbolKey"/> using the result of a previous call to
        /// <see cref="ToString()"/> from this same session.  Instantiating with a string 
        /// from any other source is not supported.
        /// </summary>
        public SymbolKey(string data)
            => _symbolKeyData = data ?? throw new ArgumentNullException();

        /// <summary>
        /// Constructs a new <see cref="SymbolKey"/> representing the provided <paramref name="symbol"/>.
        /// </summary>
        internal static SymbolKey Create(ISymbol symbol, CancellationToken cancellationToken = default)
            => new SymbolKey(CreateString(symbol, cancellationToken));

        /// <summary>
        /// Returns an <see cref="IEqualityComparer{T}"/> that determines if two <see cref="SymbolKey"/>s
        /// represent the same effective symbol.
        /// </summary>
        /// <param name="ignoreCase">Whether or not casing should be considered when comparing keys. 
        /// For example, with <c>ignoreCase=true</c> then <c>X.SomeClass</c> and <c>X.Someclass</c> would be 
        /// considered the same effective symbol</param>
        /// <param name="ignoreAssemblyKeys">Whether or not the originating assembly of referenced
        /// symbols should be compared when determining if two symbols are effectively the same.
        /// For example, with <c>ignoreAssemblyKeys=true</c> then an <c>X.SomeClass</c> from assembly 
        /// <c>A</c> and <c>X.SomeClass</c> from assembly <c>B</c> will be considered the same
        /// effective symbol.
        /// </param>
        internal static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase = false, bool ignoreAssemblyKeys = false)
            => SymbolKeyComparer.GetComparer(ignoreCase, ignoreAssemblyKeys);

        internal static SymbolKeyResolution ResolveString(
            string symbolKey, Compilation compilation,
            bool ignoreAssemblyKey = false, bool resolveLocations = false,
            CancellationToken cancellationToken = default)
        {
            using var reader = SymbolKeyReader.GetReader(
                symbolKey, compilation, ignoreAssemblyKey, resolveLocations, cancellationToken);
            var version = reader.ReadFormatVersion();
            if (version != FormatVersion)
            {
                return default;
            }

            var result = reader.ReadSymbolKey();
            Debug.Assert(reader.Position == symbolKey.Length);
            return result;
        }

        internal static string CreateString(ISymbol symbol, CancellationToken cancellationToken = default)
            => CreateStringWorker(FormatVersion, symbol, cancellationToken);

        // Internal for testing purposes.
        internal static string CreateStringWorker(int version, ISymbol symbol, CancellationToken cancellationToken = default)
        {
            using var writer = SymbolKeyWriter.GetWriter(cancellationToken);
            writer.WriteFormatVersion(version);
            writer.WriteSymbolKey(symbol);
            return writer.CreateKey();
        }

        /// <summary>
        /// Tries to resolve this <see cref="SymbolKey"/> in the given 
        /// <paramref name="compilation"/> to a matching symbol.  <paramref name="resolveLocations"/>
        /// should only be given <see langword="true"/> if the symbol was produced from a compilation
        /// that has the exact same source as the compilation we're resolving against.  Otherwise
        /// the locations resolved may not actually be correct in the final compilation.
        /// </summary>
        public SymbolKeyResolution Resolve(
            Compilation compilation, bool ignoreAssemblyKey = false, bool resolveLocations = false, CancellationToken cancellationToken = default)
        {
            return ResolveString(_symbolKeyData, compilation, ignoreAssemblyKey, resolveLocations, cancellationToken);
        }

        /// <summary>
        /// Returns this <see cref="SymbolKey"/> encoded as a string.  This can be persisted
        /// and used later with <see cref="SymbolKey(string)"/> to then try to resolve back
        /// to the corresponding <see cref="ISymbol"/> in a future <see cref="Compilation"/>.
        /// 
        /// This string form is not guaranteed to be reusable across all future versions of 
        /// Roslyn.  As such it should only be used for caching data, with the knowledge that
        /// the data may need to be recomputed if the cached data can no longer be used.
        /// </summary>
        public override string ToString()
            => _symbolKeyData;

        private static SymbolKeyResolution CreateResolution<TSymbol>(PooledArrayBuilder<TSymbol> symbols)
            where TSymbol : class, ISymbol
        {
#if DEBUG
            foreach (var symbol in symbols)
            {
                Debug.Assert(symbol != null);
            }
#endif

            if (symbols.Builder.Count == 0)
            {
                return default;
            }
            else if (symbols.Builder.Count == 1)
            {
                return new SymbolKeyResolution(symbols.Builder[0]);
            }
            else
            {
                return new SymbolKeyResolution(
                    ImmutableArray<ISymbol>.CastUp(symbols.Builder.ToImmutable()),
                    CandidateReason.Ambiguous);
            }
        }

        private static bool Equals(Compilation compilation, string name1, string name2)
            => Equals(compilation.IsCaseSensitive, name1, name2);

        private static bool Equals(bool isCaseSensitive, string name1, string name2)
            => string.Equals(name1, name2, isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        private static string GetName(string metadataName)
        {
            var index = metadataName.IndexOf('`');
            return index > 0
                ? metadataName.Substring(0, index)
                : metadataName;
        }

        private static bool ParameterRefKindsMatch(
            ImmutableArray<IParameterSymbol> parameters,
            PooledArrayBuilder<RefKind> refKinds)
        {
            if (parameters.Length != refKinds.Count)
            {
                return false;
            }

            for (var i = 0; i < refKinds.Count; i++)
            {
                // The ref-out distinction is not interesting for SymbolKey because you can't overload
                // based on the difference.
                if (!SymbolEquivalenceComparer.AreRefKindsEquivalent(
                        refKinds[i], parameters[i].RefKind, distinguishRefFromOut: false))
                {
                    return false;
                }
            }

            return true;
        }

        private static PooledArrayBuilder<TSymbol> GetMembersOfNamedType<TSymbol>(
            SymbolKeyResolution containingTypeResolution,
            string metadataNameOpt) where TSymbol : ISymbol
        {
            var result = PooledArrayBuilder<TSymbol>.GetInstance();
            foreach (var containingType in containingTypeResolution.OfType<INamedTypeSymbol>())
            {
                var members = metadataNameOpt == null
                    ? containingType.GetMembers()
                    : containingType.GetMembers(metadataNameOpt);

                foreach (var member in members)
                {
                    if (member is TSymbol symbol)
                    {
                        result.AddIfNotNull(symbol);
                    }
                }
            }

            return result;
        }

        private static T FirstOrDefault<T>(ImmutableArray<T> values)
            => values.IsDefaultOrEmpty ? default : values[0];
    }
}
