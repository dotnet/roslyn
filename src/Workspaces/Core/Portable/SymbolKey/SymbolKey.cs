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
    /// </para>
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
    ///     compilation (and thus does not have a containing symbol) then it will only match another
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
    /// </summary>
    internal partial struct SymbolKey : IEquatable<SymbolKey>
    {
        private readonly static Func<ITypeSymbol, bool> s_typeIsNull = t => t == null;

        public string EncodedSymbolData { get; }
        public bool IsDefault => EncodedSymbolData == null;

        private SymbolKey(string encodedSymbolData)
        {
            EncodedSymbolData = encodedSymbolData ?? throw new ArgumentNullException();
        }

        internal static string RemoveAssemblyKeys(string data)
        {
            var reader = new RemoveAssemblySymbolKeysReader();
            reader.Initialize(data);
            return reader.RemoveAssemblySymbolKeys();
        }

        public static SymbolKey From(ISymbol symbol, CancellationToken cancellationToken = default)
            => new SymbolKey(GetEncodedSymbolData(symbol, cancellationToken));

        public static SymbolKey From(string encodedSymbolData)
            => new SymbolKey(encodedSymbolData);

        public static string GetEncodedSymbolData(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            using (var writer = SymbolKeyWriter.GetWriter(cancellationToken))
            {
                writer.WriteFirstSymbolKey(symbol);
                return writer.CreateKey();
            }
        }

        /// <summary>
        /// Resolves this <see cref="SymbolKey"/> in the given <paramref name="compilation"/>.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to resolve this <see cref="SymbolKey"/> within.</param>
        /// <param name="ignoreAssemblyNames">If <see langword="true"/>, assembly names will be ignored while resolving
        /// this <see cref="SymbolKey"/>. The default value is false.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process.</param>
        public SymbolKeyResolution Resolve(
            Compilation compilation,
            bool ignoreAssemblyNames = false,
            CancellationToken cancellationToken = default)
            => ResolveCore(EncodedSymbolData, compilation, ignoreAssemblyNames, resolveLocations: false, cancellationToken);

        /// <summary>
        /// Resolves this <see cref="SymbolKey"/> in the given <paramref name="compilation"/>.
        /// This should only be called if the symbol that this <see cref="SymbolKey"/> represents was produced
        /// from a compilation that has the exact same source as the given <paramref name="compilation"/>.
        /// Otherwise, the locations resolved may be incorrect in the final compilation.
        /// </summary>
        internal SymbolKeyResolution ResolveWithLocations(
            Compilation compilation,
            CancellationToken cancellationToken = default)
            => ResolveCore(EncodedSymbolData, compilation, ignoreAssemblyNames: false, resolveLocations: true, cancellationToken);

        private static SymbolKeyResolution ResolveCore(
            string encodedSymbolData,
            Compilation compilation,
            bool ignoreAssemblyNames, bool resolveLocations,
            CancellationToken cancellationToken)
        {
            if (encodedSymbolData == null)
            {
                throw new InvalidOperationException(WorkspacesResources.Could_not_resolve_SymbolKey_because_it_does_not_contain_encoded_symbol_data);
            }

            using (var reader = SymbolKeyReader.GetReader(
                encodedSymbolData, compilation, ignoreAssemblyNames, resolveLocations, cancellationToken))
            {
                var result = reader.ReadFirstSymbolKey();
                Debug.Assert(reader.Position == encodedSymbolData.Length, "Did not fully read encodedSymbolData");
                return result;
            }
        }

        public override bool Equals(object obj)
            => obj is SymbolKey && Equals((SymbolKey)obj);

        public bool Equals(SymbolKey other)
            => string.Equals(EncodedSymbolData, other.EncodedSymbolData);

        public override int GetHashCode()
            => unchecked(0x59354BD2 + EncodedSymbolData.GetHashCode());

        public override string ToString()
            => EncodedSymbolData;

        public static bool operator ==(SymbolKey key1, SymbolKey key2)
            => key1.Equals(key2);

        public static bool operator !=(SymbolKey key1, SymbolKey key2)
            => !(key1 == key2);

        private static bool AreNamesEqual(Compilation compilation, string name1, string name2)
            => string.Equals(name1, name2, compilation.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        private static bool ParameterRefKindsMatch(
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<RefKind> refKinds)
        {
            if (parameters.Length != refKinds.Length)
            {
                return false;
            }

            for (int i = 0; i < refKinds.Length; i++)
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
    }
}
