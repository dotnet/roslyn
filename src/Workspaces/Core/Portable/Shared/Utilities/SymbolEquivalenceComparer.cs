// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Provides a way to test two symbols for equivalence.  While there are ways to ask for
    /// different sorts of equivalence, the following must hold for two symbols to be considered
    /// equivalent.
    /// 
    /// 1) The kinds of the two symbols must match.
    /// 
    /// 2) The names of the two symbols must match.
    /// 
    /// 3) The arity of the two symbols must match.
    /// 
    /// 4) If the symbols are methods or parameterized properties, then the signatures of the two
    /// symbols must match.
    /// 
    /// 5) Both symbols must be definitions or must be instantiations.  If they are instantiations,
    /// then they must be instantiated in the same manner.
    /// 
    /// 6) The containing symbols of the two symbols must be equivalent.
    /// 
    /// Note: equivalence does not concern itself with whole symbols.  Two types are considered
    /// equivalent if the above hold, even if one type has different members than the other.  Note:
    /// type parameters, and signature parameters are not considered 'children' when comparing
    /// symbols.
    /// 
    /// Options are provided to tweak the above slightly.  For example, by default, symbols are
    /// equivalent only if they come from the same assembly or different assemblies of the same simple name.
    /// However, one can ask if two symbols are equivalent even if their assemblies differ.
    /// </summary>
    internal partial class SymbolEquivalenceComparer :
        IEqualityComparer<ISymbol>
    {
        private readonly ImmutableArray<EquivalenceVisitor> _equivalenceVisitors;
        private readonly ImmutableArray<GetHashCodeVisitor> _getHashCodeVisitors;

        public static readonly SymbolEquivalenceComparer Instance = new SymbolEquivalenceComparer(SimpleNameAssemblyComparer.Instance, distinguishRefFromOut: false);
        public static readonly SymbolEquivalenceComparer IgnoreAssembliesInstance = new SymbolEquivalenceComparer(assemblyComparerOpt: null, distinguishRefFromOut: false);

        private readonly IEqualityComparer<IAssemblySymbol> _assemblyComparerOpt;

        public ParameterSymbolEqualityComparer ParameterEquivalenceComparer { get; }
        public SignatureTypeSymbolEquivalenceComparer SignatureTypeEquivalenceComparer { get; }

        internal SymbolEquivalenceComparer(IEqualityComparer<IAssemblySymbol> assemblyComparerOpt, bool distinguishRefFromOut)
        {
            _assemblyComparerOpt = assemblyComparerOpt;

            this.ParameterEquivalenceComparer = new ParameterSymbolEqualityComparer(this, distinguishRefFromOut);
            this.SignatureTypeEquivalenceComparer = new SignatureTypeSymbolEquivalenceComparer(this);

            // There are only so many EquivalenceVisitors and GetHashCodeVisitors we can have.
            // Create them all up front.
            var equivalenceVisitorsBuilder = ImmutableArray.CreateBuilder<EquivalenceVisitor>();
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(this, compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(this, compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: false));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(this, compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: true));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(this, compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false));
            _equivalenceVisitors = equivalenceVisitorsBuilder.ToImmutable();

            var getHashCodeVisitorsBuilder = ImmutableArray.CreateBuilder<GetHashCodeVisitor>();
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(this, compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(this, compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: false));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(this, compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: true));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(this, compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false));
            _getHashCodeVisitors = getHashCodeVisitorsBuilder.ToImmutable();
        }

        // Very subtle logic here.  When checking if two parameters are the same, we can end up with
        // a tricky infinite loop.  Specifically, consider the case if the parameter refers to a
        // method type parameter.  i.e. "void Goo<T>(IList<T> arg)".  If we compare two method type
        // parameters for equality, then we'll end up asking if their methods are the same.  And that
        // will cause us to check if their parameters are the same.  And then we'll be right back
        // here.  So, instead, when asking if parameters are equal, we pass an appropriate flag so
        // that method type parameters are just compared by index and nothing else.
        private EquivalenceVisitor GetEquivalenceVisitor(
            bool compareMethodTypeParametersByIndex = false, bool objectAndDynamicCompareEqually = false)
        {
            var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually);
            return _equivalenceVisitors[visitorIndex];
        }

        private GetHashCodeVisitor GetGetHashCodeVisitor(
            bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually)
        {
            var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually);
            return _getHashCodeVisitors[visitorIndex];
        }

        private static int GetVisitorIndex(
            bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually)
        {
            if (compareMethodTypeParametersByIndex)
            {
                if (objectAndDynamicCompareEqually)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                if (objectAndDynamicCompareEqually)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }
        }

        public bool ReturnTypeEquals(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies = null)
        {
            return GetEquivalenceVisitor().ReturnTypesAreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);
        }

        /// <summary>
        /// Compares given symbols <paramref name="x"/> and <paramref name="y"/> for equivalence.
        /// </summary>
        public bool Equals(ISymbol x, ISymbol y)
        {
            return EqualsCore(x, y, null);
        }

        /// <summary>
        /// Compares given symbols <paramref name="x"/> and <paramref name="y"/> for equivalence and populates <paramref name="equivalentTypesWithDifferingAssemblies"/>
        /// with equivalent non-nested named type key-value pairs that are contained in different assemblies.
        /// These equivalent named type key-value pairs represent possibly equivalent forwarded types, but this API doesn't perform any type forwarding equivalence checks. 
        /// </summary>
        /// <remarks>This API is only supported for <see cref="SymbolEquivalenceComparer.IgnoreAssembliesInstance"/>.</remarks>
        public bool Equals(ISymbol x, ISymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
        {
            Debug.Assert(_assemblyComparerOpt == null);
            return EqualsCore(x, y, equivalentTypesWithDifferingAssemblies);
        }

        private bool EqualsCore(ISymbol x, ISymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
        {
            return GetEquivalenceVisitor().AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);
        }

        public int GetHashCode(ISymbol x)
        {
            return GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false).GetHashCode(x, currentHash: 0);
        }

        private static ISymbol UnwrapAlias(ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Alias)
            {
                return ((IAliasSymbol)symbol).Target;
            }
            else
            {
                return symbol;
            }
        }

        private static SymbolKind GetKindAndUnwrapAlias(ref ISymbol symbol)
        {
            var k = symbol.Kind;
            if (k == SymbolKind.Alias)
            {
                symbol = ((IAliasSymbol)symbol).Target;
                k = symbol.Kind;
            }

            return k;
        }

        private static bool IsConstructedFromSelf(INamedTypeSymbol symbol)
        {
            return symbol.Equals(symbol.ConstructedFrom);
        }

        private static bool IsConstructedFromSelf(IMethodSymbol symbol)
        {
            return symbol.Equals(symbol.ConstructedFrom);
        }

        private static bool IsObjectType(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.NamedType && ((ITypeSymbol)symbol).SpecialType == SpecialType.System_Object;
        }

        private static bool CheckContainingType(IMethodSymbol x)
        {
            if (x.MethodKind == MethodKind.DelegateInvoke &&
                x.ContainingType != null &&
                x.ContainingType.IsAnonymousType)
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<INamedTypeSymbol> Unwrap(INamedTypeSymbol namedType)
        {
            yield return namedType;


            if (namedType is IErrorTypeSymbol errorType)
            {
                foreach (var type in errorType.CandidateSymbols.OfType<INamedTypeSymbol>())
                {
                    yield return type;
                }
            }
        }

        private static bool IsPartialMethodDefinitionPart(IMethodSymbol symbol)
        {
            return symbol.PartialImplementationPart != null;
        }

        private static bool IsPartialMethodImplementationPart(IMethodSymbol symbol)
        {
            return symbol.PartialDefinitionPart != null;
        }

        private static TypeKind GetTypeKind(INamedTypeSymbol x)
        {
            // Treat static classes as modules.
            var k = x.TypeKind;
            return k == TypeKind.Module ? TypeKind.Class : k;
        }
    }
}
