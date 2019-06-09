// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

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
    /// </para>
    /// </summary>
    internal partial struct SymbolKey
    {
        private readonly static Func<ITypeSymbol, bool> s_typeIsNull = t => t == null;

        private readonly string _symbolKeyData;

        public SymbolKey(string symbolKeyData)
        {
            _symbolKeyData = symbolKeyData ?? throw new ArgumentNullException();
        }

        public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase, bool ignoreAssemblyKeys)
        {
            return SymbolKeyComparer.GetComparer(ignoreCase, ignoreAssemblyKeys);
        }

        private static readonly Func<string, string> s_removeAssemblyKeys = (string data) =>
        {
            var reader = new RemoveAssemblySymbolKeysReader();
            reader.Initialize(data);
            return reader.RemoveAssemblySymbolKeys();
        };

        /// <summary>
        /// Tries to resolve the provided <paramref name="symbolKey"/> in the given 
        /// <paramref name="compilation"/> to a matching symbol.  <paramref name="resolveLocations"/>
        /// should only be given <see langword="true"/> if the symbol was produced from a compilation
        /// that has the exact same source as the compilation we're resolving against.  Otherwise
        /// the locations resolved may not actually be correct in the final compilation.
        /// </summary>
        public static SymbolKeyResolution Resolve(
            string symbolKey, Compilation compilation,
            bool ignoreAssemblyKey = false, bool resolveLocations = false,
            CancellationToken cancellationToken = default)
        {
            using (var reader = SymbolKeyReader.GetReader(
                symbolKey, compilation, ignoreAssemblyKey, resolveLocations, cancellationToken))
            {
                var result = reader.ReadFirstSymbolKey();
                Debug.Assert(reader.Position == symbolKey.Length);
                return result;
            }
        }

        public static SymbolKey Create(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            return new SymbolKey(ToString(symbol, cancellationToken));
        }

        public static string ToString(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            using (var writer = SymbolKeyWriter.GetWriter(cancellationToken))
            {
                writer.WriteFirstSymbolKey(symbol);
                return writer.CreateKey();
            }
        }

        public SymbolKeyResolution Resolve(
            Compilation compilation,
            bool ignoreAssemblyKey = false, bool resolveLocations = false,
            CancellationToken cancellationToken = default)
        {
            return Resolve(
                _symbolKeyData, compilation,
                ignoreAssemblyKey, resolveLocations,
                cancellationToken);
        }

        public override string ToString()
            => _symbolKeyData;

        private static IEnumerable<ISymbol> GetAllSymbols(SymbolKeyResolution info)
        {
            if (info.Symbol != null)
            {
                yield return info.Symbol;
            }
            else
            {
                foreach (var symbol in info.CandidateSymbols)
                {
                    yield return symbol;
                }
            }
        }

        private static IEnumerable<TType> GetAllSymbols<TType>(SymbolKeyResolution info)
            => GetAllSymbols(info).OfType<TType>();

        private static SymbolKeyResolution CreateSymbolInfo(IEnumerable<ISymbol> symbols)
        {
            return symbols == null
                ? default
                : CreateSymbolInfo(symbols.WhereNotNull().ToArray());
        }

        private static SymbolKeyResolution CreateSymbolInfo(ISymbol[] symbols)
        {
            return symbols.Length == 0
                ? default
                : symbols.Length == 1
                    ? new SymbolKeyResolution(symbols[0])
                    : new SymbolKeyResolution(ImmutableArray.Create<ISymbol>(symbols), CandidateReason.Ambiguous);
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

        private static IEnumerable<INamedTypeSymbol> InstantiateTypes(
            Compilation compilation,
            bool ignoreAssemblyKey,
            ImmutableArray<INamedTypeSymbol> types,
            int arity,
            ImmutableArray<SymbolKeyResolution> typeArgumentKeys)
        {
            if (arity == 0 || typeArgumentKeys.IsDefault || typeArgumentKeys.IsEmpty)
            {
                return types;
            }

            // TODO(cyrusn): We're only accepting a type argument if it resolves unambiguously.
            // However, we could consider the case where they resolve ambiguously and return
            // different named type instances when that happens.
            var typeArguments = typeArgumentKeys.Select(a => GetFirstSymbol<ITypeSymbol>(a)).ToArray();
            return typeArguments.Any(s_typeIsNull)
                ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                : types.Select(t => t.Construct(typeArguments));
        }

        private static TSymbol GetFirstSymbol<TSymbol>(SymbolKeyResolution resolution)
            where TSymbol : ISymbol
        {
            return resolution.GetAllSymbols().OfType<TSymbol>().FirstOrDefault();
        }

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
