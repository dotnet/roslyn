// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// Provides a way to test two symbols for equivalence.  While there are ways to ask for
/// different sorts of equivalence, the following must hold for two symbols to be considered
/// equivalent.
/// <list type="number">
/// <item>The kinds of the two symbols must match.</item>
/// <item>The names of the two symbols must match.</item>
/// <item>The arity of the two symbols must match.</item>
/// <item>If the symbols are methods or parameterized properties, then the signatures of the two
/// symbols must match.</item>
/// <item>Both symbols must be definitions or must be instantiations.  If they are instantiations,
/// then they must be instantiated in the same manner.</item>
/// <item>The containing symbols of the two symbols must be equivalent.</item>
/// <item>Nullability of symbols is not involved in the comparison.</item>
/// </list>
/// Note: equivalence does not concern itself with whole symbols.  Two types are considered
/// equivalent if the above hold, even if one type has different members than the other.  Note:
/// type parameters, and signature parameters are not considered 'children' when comparing
/// symbols.
/// 
/// Options are provided to tweak the above slightly.  For example, by default, symbols are
/// equivalent only if they come from the same assembly or different assemblies of the same simple name.
/// However, one can ask if two symbols are equivalent even if their assemblies differ.
/// </summary>
internal sealed partial class SymbolEquivalenceComparer : IEqualityComparer<ISymbol?>
{
    private readonly ImmutableArray<EquivalenceVisitor> _equivalenceVisitors;
    private readonly ImmutableArray<GetHashCodeVisitor> _getHashCodeVisitors;

    public static readonly SymbolEquivalenceComparer Instance = Create(distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
    public static readonly SymbolEquivalenceComparer TupleNamesMustMatchInstance = Create(distinguishRefFromOut: false, tupleNamesMustMatch: true, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
    public static readonly SymbolEquivalenceComparer IgnoreAssembliesInstance = new(assemblyComparer: null, distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

    private readonly IEqualityComparer<IAssemblySymbol>? _assemblyComparer;

    private readonly bool _distinguishRefFromOut;
    private readonly bool _tupleNamesMustMatch;
    private readonly bool _ignoreNullableAnnotations;
    private readonly bool _objectAndDynamicCompareEqually;
    private readonly bool _arrayAndReadOnlySpanCompareEqually;

    public ParameterSymbolEqualityComparer ParameterEquivalenceComparer { get; }
    public SignatureTypeSymbolEquivalenceComparer SignatureTypeEquivalenceComparer { get; }

    public SymbolEquivalenceComparer(
        IEqualityComparer<IAssemblySymbol>? assemblyComparer,
        bool distinguishRefFromOut,
        bool tupleNamesMustMatch,
        bool ignoreNullableAnnotations,
        bool objectAndDynamicCompareEqually,
        bool arrayAndReadOnlySpanCompareEqually)
    {
        _assemblyComparer = assemblyComparer;
        _distinguishRefFromOut = distinguishRefFromOut;
        _tupleNamesMustMatch = tupleNamesMustMatch;
        _ignoreNullableAnnotations = ignoreNullableAnnotations;
        _objectAndDynamicCompareEqually = objectAndDynamicCompareEqually;
        _arrayAndReadOnlySpanCompareEqually = arrayAndReadOnlySpanCompareEqually;

        this.ParameterEquivalenceComparer = new ParameterSymbolEqualityComparer(this, distinguishRefFromOut);
        this.SignatureTypeEquivalenceComparer = new SignatureTypeSymbolEquivalenceComparer(this);

        // There are only so many EquivalenceVisitors and GetHashCodeVisitors we can have.
        // Create them all up front.
        using var equivalenceVisitors = TemporaryArray<EquivalenceVisitor>.Empty;
        using var getHashCodeVisitors = TemporaryArray<GetHashCodeVisitor>.Empty;

        foreach (var compareMethodTypeParametersByIndex in new[] { true, false })
        {
            foreach (var objectAndDynamicCompareEquallySwitch in new[] { true, false })
            {
                foreach (var arrayAndReadOnlySpanCompareEquallySwitch in new[] { true, false })
                    AddVisitors(compareMethodTypeParametersByIndex, objectAndDynamicCompareEquallySwitch, arrayAndReadOnlySpanCompareEquallySwitch);
            }
        }

        _equivalenceVisitors = equivalenceVisitors.ToImmutableAndClear();
        _getHashCodeVisitors = getHashCodeVisitors.ToImmutableAndClear();

        return;

        void AddVisitors(bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually, bool arrayAndReadOnlySpanCompareEquallySwitch)
        {
            equivalenceVisitors.Add(new(this, compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEquallySwitch));
            getHashCodeVisitors.Add(new(this, compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEquallySwitch));
        }
    }

    public static SymbolEquivalenceComparer Create(
        bool distinguishRefFromOut,
        bool tupleNamesMustMatch,
        bool ignoreNullableAnnotations,
        bool objectAndDynamicCompareEqually,
        bool arrayAndReadOnlySpanCompareEqually)
    {
        return new(SimpleNameAssemblyComparer.Instance, distinguishRefFromOut, tupleNamesMustMatch, ignoreNullableAnnotations, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEqually);
    }

    public SymbolEquivalenceComparer With(
        Optional<bool> distinguishRefFromOut = default,
        Optional<bool> tupleNamesMustMatch = default,
        Optional<bool> ignoreNullableAnnotations = default,
        Optional<bool> objectAndDynamicCompareEqually = default,
        Optional<bool> arrayAndReadOnlySpanCompareEqually = default)
    {
        var newDistinguishRefFromOut = distinguishRefFromOut.HasValue ? distinguishRefFromOut.Value : _distinguishRefFromOut;
        var newTupleNamesMustMatch = tupleNamesMustMatch.HasValue ? tupleNamesMustMatch.Value : _tupleNamesMustMatch;
        var newIgnoreNullableAnnotations = ignoreNullableAnnotations.HasValue ? ignoreNullableAnnotations.Value : _ignoreNullableAnnotations;
        var newObjectAndDynamicCompareEqually = objectAndDynamicCompareEqually.HasValue ? objectAndDynamicCompareEqually.Value : _objectAndDynamicCompareEqually;
        var newArrayAndReadOnlySpanCompareEqually = arrayAndReadOnlySpanCompareEqually.HasValue ? arrayAndReadOnlySpanCompareEqually.Value : _arrayAndReadOnlySpanCompareEqually;

        return new(_assemblyComparer, newDistinguishRefFromOut, newTupleNamesMustMatch, newIgnoreNullableAnnotations, newObjectAndDynamicCompareEqually, newArrayAndReadOnlySpanCompareEqually);
    }

    // Very subtle logic here.  When checking if two parameters are the same, we can end up with
    // a tricky infinite loop.  Specifically, consider the case if the parameter refers to a
    // method type parameter.  i.e. "void Goo<T>(IList<T> arg)".  If we compare two method type
    // parameters for equality, then we'll end up asking if their methods are the same.  And that
    // will cause us to check if their parameters are the same.  And then we'll be right back
    // here.  So, instead, when asking if parameters are equal, we pass an appropriate flag so
    // that method type parameters are just compared by index and nothing else.
    private EquivalenceVisitor GetEquivalenceVisitor(
        bool compareMethodTypeParametersByIndex = false, bool objectAndDynamicCompareEqually = false, bool arrayAndReadOnlySpanCompareEqually = false)
    {
        var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEqually);
        return _equivalenceVisitors[visitorIndex];
    }

    private GetHashCodeVisitor GetGetHashCodeVisitor(
        bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually, bool arrayAndReadOnlySpanCompareEqually)
    {
        var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEqually);
        return _getHashCodeVisitors[visitorIndex];
    }

    private static int GetVisitorIndex(bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually)
        => (compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually, arrayAndReadOnlySpanCompareEqually) switch
        {
            (true, true) => 0,
            (true, false) => 1,
            (false, true) => 2,
            (false, false) => 3,
        };

    public bool ReturnTypeEquals(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies = null)
        => GetEquivalenceVisitor().ReturnTypesAreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);

    /// <summary>
    /// Compares given symbols <paramref name="x"/> and <paramref name="y"/> for equivalence.
    /// </summary>
    public bool Equals(ISymbol? x, ISymbol? y)
        => EqualsCore(x, y, equivalentTypesWithDifferingAssemblies: null);

    /// <summary>
    /// Compares given symbols <paramref name="x"/> and <paramref name="y"/> for equivalence and populates <paramref name="equivalentTypesWithDifferingAssemblies"/>
    /// with equivalent non-nested named type key-value pairs that are contained in different assemblies.
    /// These equivalent named type key-value pairs represent possibly equivalent forwarded types, but this API doesn't perform any type forwarding equivalence checks. 
    /// </summary>
    /// <remarks>This API is only supported for <see cref="SymbolEquivalenceComparer.IgnoreAssembliesInstance"/>.</remarks>
    public bool Equals(ISymbol? x, ISymbol? y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
    {
        Debug.Assert(_assemblyComparer == null);
        return EqualsCore(x, y, equivalentTypesWithDifferingAssemblies);
    }

    private bool EqualsCore(ISymbol? x, ISymbol? y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        => GetEquivalenceVisitor().AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);

    public int GetHashCode(ISymbol? x)
        => GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false).GetHashCode(x, currentHash: 0);

    private static ISymbol UnwrapAlias(ISymbol symbol)
        => symbol.IsKind(SymbolKind.Alias, out IAliasSymbol? alias) ? alias.Target : symbol;

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
        => symbol.Equals(symbol.ConstructedFrom);

    private static bool IsConstructedFromSelf(IMethodSymbol symbol)
        => symbol.Equals(symbol.ConstructedFrom);

    private static bool IsObjectType(ISymbol symbol)
        => symbol.IsKind(SymbolKind.NamedType, out ITypeSymbol? typeSymbol) && typeSymbol.SpecialType == SpecialType.System_Object;

    private static bool CheckContainingType(IMethodSymbol x)
    {
        if (x is { MethodKind: MethodKind.DelegateInvoke, ContainingType.IsAnonymousType: true })
            return false;

        // We use the signature of a function pointer type to determine equivalence, but
        // function pointer types do not have containing types.
        if (x.MethodKind == MethodKind.FunctionPointerSignature)
            return false;

        return true;
    }

    private static OneOrMany<INamedTypeSymbol> Unwrap(INamedTypeSymbol namedType)
    {
        // Make the common case non-allocating.
        if (namedType is not IErrorTypeSymbol errorType)
            return OneOrMany.Create(namedType);

        using var builder = TemporaryArray<INamedTypeSymbol>.Empty;
        builder.Add(namedType);

        foreach (var candidate in errorType.CandidateSymbols)
        {
            if (candidate is INamedTypeSymbol candidateType)
                builder.Add(candidateType);
        }

        return OneOrMany.Create(builder.ToImmutableAndClear());
    }

    private static bool IsPartialMethodDefinitionPart(IMethodSymbol symbol)
        => symbol.PartialImplementationPart != null;

    private static bool IsPartialMethodImplementationPart(IMethodSymbol symbol)
        => symbol.PartialDefinitionPart != null;

    private static bool IsPartialMethodDefinitionPart(IPropertySymbol symbol)
        => symbol.PartialImplementationPart != null;

    private static bool IsPartialMethodImplementationPart(IPropertySymbol symbol)
        => symbol.PartialDefinitionPart != null;

    private static TypeKind GetTypeKind(INamedTypeSymbol x)
        => x.TypeKind switch
        {
            // Treat static classes and modules as equivalent.
            TypeKind.Module => TypeKind.Class,
            var v => v,
        };
}
