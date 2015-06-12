// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <para>
    /// A SymbolKey is a lightweight identifier for a symbol that can be used to resolve the "same"
    /// symbol across compilations.  Different symbols have different concepts of "same-ness".
    /// Same-ness is recursively defined as follows:
    /// <list type="number">
    ///   <item>Two IArraySymbol's are the "same" if they have the "same" element type and the same rank.</item>
    ///   <item>Two IAssemblySymbol's are the "same" if they have the same <see cref="ISymbol.Name"/>.</item>
    ///   <item>Two IEventSymbol's are the "same" if they have the "same" containing type and the same <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two IMethodSymbol's are the "same" if they have the "same" containing type, the same
    ///     <see cref="ISymbol.MetadataName"/>, the same <see cref="IMethodSymbol.Arity"/>, the "same"
    ///     <see cref="IMethodSymbol.TypeArguments"/>, and have same parameter types and 
    ///     <see cref="IParameterSymbol.RefKind"/>.</item>
    ///   <item>IModuleSymbol's are the "same" if they have the same containing assembly.
    ///     <see cref="ISymbol.MetadataName"/> is not used because module identity is not important in practice.</item>
    ///   <item>Two INamedTypeSymbol's are the "same" if they have "same" containing symbol, the same
    ///     <see cref="ISymbol.MetadataName"/>, the same <see cref="INamedTypeSymbol.Arity"/> and the "same"
    ///     <see cref="INamedTypeSymbol.TypeArguments"/>.</item>
    ///   <item>Two INamespaceSymbol's are the "same" if they have the "same" containing symbol and the
    ///     same <see cref="ISymbol.MetadataName"/>.  If the INamespaceSymbol is the global namespace for a
    ///     compilation (and thus does not have a containing symbol) then it will only match another
    ///     global namespace of another compilation.</item>
    ///   <item>Two IParameterSymbol's are the "same" if they have the "same" containing symbol and the <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two IPointerTypeSymbol's are the "same" if they have the "same" <see cref="IPointerTypeSymbol.PointedAtType"/>.</item>
    ///   <item>Two IPropertySymbol's are the "same" if they have the "same" containing type, the same
    ///     <see cref="ISymbol.MetadataName"/>,  and have same parameter types and <see cref="IParameterSymbol.RefKind"/>.</item>
    ///   <item>Two ITypeParameterSymbol's are the "same" if they have the "same" containing symbol and the <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two IFieldSymbol's are the "same" if they have the "same" containing symbol and the <see cref="ISymbol.MetadataName"/>.</item>
    /// </list>    
    /// A SymbolID for an IAliasSymbol will <see cref="SymbolKey.Resolve"/> back to the ISymbol for
    /// the <see cref="IAliasSymbol.Target"/>.
    /// </para>
    /// <para>
    /// Due to issues arising from errors and ambiguity, it's possible for a SymbolKey to resolve to
    /// multiple symbols. For example, in the following type:
    ///  <code>
    /// class C
    /// {
    ///    int Foo();
    ///    bool Foo();
    /// }
    /// </code>
    /// The SymbolKey for both Foo methods will be the same.  The SymbolId will then resolve to both methods.
    /// </para>
    /// </summary>
    internal abstract partial class SymbolKey
    {
        private static readonly SymbolKey s_null = new NullSymbolKey();

        public abstract SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken));

        public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase, bool ignoreAssemblyKeys)
        {
            return SymbolKeyComparer.GetComparer(ignoreCase, ignoreAssemblyKeys, compareMethodTypeParametersByName: false);
        }

        /// <summary>
        /// <para>
        /// This entry point should only be called from the actual Symbol classes. It should not be
        /// used internally inside this type.  Instead, any time we need to get the <see cref="SymbolKey"/> for a
        /// related symbol (i.e. the containing namespace of a namespace) we should call
        /// <see cref="GetOrCreate"/>.  The benefit of this is twofold.  First of all, it keeps the size of the
        /// <see cref="SymbolKey"/> small by allowing up to reuse parts we've already created.  For example, if we
        /// have the <see cref="SymbolKey"/> for <c>Foo(int, int)</c>, then we will reuse the <see cref="SymbolKey"/>s for both <c>int</c>s.
        /// Second, this allows us to deal with the recursive nature of MethodSymbols and
        /// TypeParameterSymbols.  Specifically, a MethodSymbol is defined by its signature.  However,
        /// it's signature may refer to type parameters of that method.  Unfortunately, the type
        /// parameters depend on their containing method.
        /// </para>
        /// <para>
        /// For example, if there is <c><![CDATA[Foo<T>(T t)]]></c>, then we must avoid the situation where we:
        /// <list type="number">
        /// <item>try to get the symbol ID for the type parameter <c>T</c>, which in turn</item>
        /// <item>tries to get the symbol ID for the method <c>T</c>, which in turn</item>
        /// <item>tries to get the symbol IDs for the parameter types, which in turn</item>
        /// <item>tries to get the symbol ID for the type parameter <c>T</c>, which leads back to 1 and infinitely loops.</item>
        /// </list>
        /// </para>
        /// <para>
        /// In order to break this circularity we do not create the SymbolIDs for a method's type
        /// parameters directly in the visitor.  Instead, we create the SymbolID for the method
        /// itself.  When the MethodSymbolId is created it will directly instantiate the SymbolIDs
        /// for the type parameters, and directly assign the type parameter's method ID to itself.
        /// It will also then directly store the mapping from the type parameter to its SymbolID in
        /// the visitor cache.  Then when we try to create the symbol IDs for the parameter types,
        /// any reference to the type parameters can be found in the cache.
        /// </para>
        /// <para>
        /// It is for this reason that it is essential that all calls to get related symbol IDs goes
        /// through GetOrCreate and not Create.
        /// </para>
        /// </summary>
        internal static SymbolKey Create(ISymbol symbol, Compilation compilation = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetOrCreate(symbol, new Visitor(compilation, cancellationToken));
        }

        private static SymbolKey GetOrCreate(ISymbol symbol, Visitor visitor)
        {
            if (symbol == null)
            {
                return s_null;
            }

            SymbolKey result;
            if (!visitor.SymbolCache.TryGetValue(symbol, out result))
            {
                result = symbol.Accept(visitor);
                visitor.SymbolCache[symbol] = result;
            }

            return result;
        }

        /// <summary>
        /// <para>
        /// When comparing symbols we need to handle recursion between method type parameters and
        /// methods.  For example, if we have two methods with the signature <c><![CDATA[Foo<T>(T t)]]></c> and we
        /// try to test for equality we must avoid the situation where we:
        /// <list type="number">
        ///   <item>First test if the methods are the same, which will in turn</item>
        ///   <item>test if the method's parameter types are the same, which will in turn</item>
        ///   <item>test if the type parameters are the same, which will in turn</item>
        ///   <item>test if the methods are the same, which causes infinite recursion.</item>
        /// </list>
        /// To avoid this we distinguish the cases where we're testing if two type parameters
        /// actually refer to the same thing, versus type parameters being referenced by parameters.
        /// For example, if we have:
        /// <code><![CDATA[ 
        /// Foo<T>(T t) 
        /// Bar<T>(T t) 
        /// ]]></code>
        /// then clearly the type parameter <c>T</c> in <c><![CDATA[Foo<T>]]></c> is different from the type parameter <c>T</c>
        /// in <c><![CDATA[Bar<T>]]></c>. When testing these type parameters for equality we *will* test to see
        /// if they have the same parent. This will end up returning false, and so we will consider
        /// them different.
        /// </para>
        /// <para>
        /// However, when we are testing if two signatures are the same, if we hit a method type
        /// parameter then we only need to compare by metadataName.  That's because we know we'll
        /// already have checked if the method and it's parents are the same, so we don't need to
        /// recurse through them again.
        /// </para>
        /// </summary>
        internal abstract bool Equals(SymbolKey other, ComparisonOptions options);
        internal abstract int GetHashCode(ComparisonOptions options);

        private static bool Equals(Compilation compilation, string name1, string name2)
        {
            return Equals(compilation.IsCaseSensitive, name1, name2);
        }

        private static bool Equals(bool isCaseSensitive, string name1, string name2)
        {
            return string.Equals(name1, name2, isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        private static int GetHashCode(bool isCaseSensitive, string metadataName)
        {
            return isCaseSensitive
                ? metadataName.GetHashCode()
                : StringComparer.OrdinalIgnoreCase.GetHashCode(metadataName);
        }

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
        {
            return GetAllSymbols(info).OfType<TType>();
        }

        private static SymbolKeyResolution CreateSymbolInfo(IEnumerable<ISymbol> symbols)
        {
            return symbols == null
                ? default(SymbolKeyResolution)
                : CreateSymbolInfo(symbols.WhereNotNull().ToArray());
        }

        private static SymbolKeyResolution CreateSymbolInfo(ISymbol[] symbols)
        {
            return symbols.Length == 0
                ? default(SymbolKeyResolution)
                : symbols.Length == 1
                    ? new SymbolKeyResolution(symbols[0])
                    : new SymbolKeyResolution(ImmutableArray.Create<ISymbol>(symbols), CandidateReason.Ambiguous);
        }

        private static bool ParametersMatch(
            ComparisonOptions options,
            Compilation compilation,
            ImmutableArray<IParameterSymbol> parameters,
            RefKind[] refKinds,
            SymbolKey[] typeKeys,
            CancellationToken cancellationToken)
        {
            if (parameters.Length != refKinds.Length)
            {
                return false;
            }

            for (int i = 0; i < refKinds.Length; i++)
            {
                // The ref-out distinction is not interesting for SymbolKey because you can't overload
                // based on the difference.
                var parameter = parameters[i];
                if (!SymbolEquivalenceComparer.AreRefKindsEquivalent(refKinds[i], parameter.RefKind, distinguishRefFromOut: false))
                {
                    return false;
                }

                // We are checking parameters for equality, if they refer to method type parameters,
                // then we don't want to recurse through the method (which would then recurse right
                // back into the parameters).  So we ask that type parameters only be checked by
                // metadataName to prevent that.
                var newOptions = new ComparisonOptions(
                    options.IgnoreCase,
                    options.IgnoreAssemblyKey,
                    compareMethodTypeParametersByName: true);

                if (!typeKeys[i].Equals(SymbolKey.Create(parameter.Type, compilation, cancellationToken), newOptions))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SequenceEquals<T>(T[] array1, T[] array2, IEqualityComparer<T> comparer)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1 == null || array2 == null)
            {
                return false;
            }

            return array1.SequenceEqual(array2, comparer);
        }

        private static bool SequenceEquals<T>(ImmutableArray<T> array1, ImmutableArray<T> array2, IEqualityComparer<T> comparer)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1.IsDefault || array2.IsDefault)
            {
                return false;
            }

            return array1.SequenceEqual(array2, comparer);
        }

        private static string GetName(string metadataName)
        {
            var index = metadataName.IndexOf('`');
            return index > 0
                ? metadataName.Substring(0, index)
                : metadataName;
        }
    }
}
