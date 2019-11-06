// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

////#define TRACKDEPTH

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        private class EquivalenceVisitor
        {
            private readonly bool _compareMethodTypeParametersByIndex;
            private readonly bool _objectAndDynamicCompareEqually;
            private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;

            public EquivalenceVisitor(
                SymbolEquivalenceComparer symbolEquivalenceComparer,
                bool compareMethodTypeParametersByIndex,
                bool objectAndDynamicCompareEqually)
            {
                _symbolEquivalenceComparer = symbolEquivalenceComparer;
                _compareMethodTypeParametersByIndex = compareMethodTypeParametersByIndex;
                _objectAndDynamicCompareEqually = objectAndDynamicCompareEqually;
            }

#if TRACKDEPTH
            private int depth = 0;
#endif
            public bool AreEquivalent(ISymbol x, ISymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
#if TRACKDEPTH
                try
                { 
                this.depth++;
                if (depth > 100)
                {
                    throw new InvalidOperationException("Stack too deep.");
                }
#endif
                // https://github.com/dotnet/roslyn/issues/39643 This is a temporary workaround sufficient to get existing tests passing.
                // This component should be modified to properly deal with differences caused by nullability.
                if (x is ITypeSymbol xType && y is ITypeSymbol yType && xType.IsDefinition != yType.IsDefinition)
                {
                    if (x.IsDefinition)
                    {
                        y = yType.WithNullableAnnotation(xType.NullableAnnotation);
                    }
                    else
                    {
                        x = xType.WithNullableAnnotation(yType.NullableAnnotation);
                    }
                }

                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                var xKind = GetKindAndUnwrapAlias(ref x);
                var yKind = GetKindAndUnwrapAlias(ref y);

                // Normally, if they're different types, then they're not the same.
                if (xKind != yKind)
                {
                    // Special case.  If we're comparing signatures then we want to compare 'object'
                    // and 'dynamic' as the same.  However, since they're different types, we don't
                    // want to bail out using the above check.
                    return _objectAndDynamicCompareEqually &&
                           ((yKind == SymbolKind.DynamicType && xKind == SymbolKind.NamedType && ((ITypeSymbol)x).SpecialType == SpecialType.System_Object) ||
                            (xKind == SymbolKind.DynamicType && yKind == SymbolKind.NamedType && ((ITypeSymbol)y).SpecialType == SpecialType.System_Object));
                }

                return AreEquivalentWorker(x, y, xKind, equivalentTypesWithDifferingAssemblies);

#if TRACKDEPTH
            }
            finally
            {
                this.depth--;
            }
#endif
            }

            internal bool AreEquivalent(CustomModifier x, CustomModifier y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return x.IsOptional == y.IsOptional && AreEquivalent(x.Modifier, y.Modifier, equivalentTypesWithDifferingAssemblies);
            }

            internal bool AreEquivalent(ImmutableArray<CustomModifier> x, ImmutableArray<CustomModifier> y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                Debug.Assert(!x.IsDefault && !y.IsDefault);
                if (x.Length != y.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Length; i++)
                {
                    if (!AreEquivalent(x[i], y[i], equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool AreEquivalentWorker(ISymbol x, ISymbol y, SymbolKind k, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                Debug.Assert(x.Kind == y.Kind && x.Kind == k);
                switch (k)
                {
                    case SymbolKind.ArrayType:
                        return ArrayTypesAreEquivalent((IArrayTypeSymbol)x, (IArrayTypeSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Assembly:
                        return AssembliesAreEquivalent((IAssemblySymbol)x, (IAssemblySymbol)y);
                    case SymbolKind.DynamicType:
                        return DynamicTypesAreEquivalent((IDynamicTypeSymbol)x, (IDynamicTypeSymbol)y);
                    case SymbolKind.Event:
                        return EventsAreEquivalent((IEventSymbol)x, (IEventSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Field:
                        return FieldsAreEquivalent((IFieldSymbol)x, (IFieldSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Label:
                        return LabelsAreEquivalent((ILabelSymbol)x, (ILabelSymbol)y);
                    case SymbolKind.Local:
                        return LocalsAreEquivalent((ILocalSymbol)x, (ILocalSymbol)y);
                    case SymbolKind.Method:
                        return MethodsAreEquivalent((IMethodSymbol)x, (IMethodSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.NetModule:
                        return ModulesAreEquivalent((IModuleSymbol)x, (IModuleSymbol)y);
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType: // ErrorType is handled in NamedTypesAreEquivalent
                        return NamedTypesAreEquivalent((INamedTypeSymbol)x, (INamedTypeSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Namespace:
                        return NamespacesAreEquivalent((INamespaceSymbol)x, (INamespaceSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Parameter:
                        return ParametersAreEquivalent((IParameterSymbol)x, (IParameterSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.PointerType:
                        return PointerTypesAreEquivalent((IPointerTypeSymbol)x, (IPointerTypeSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Property:
                        return PropertiesAreEquivalent((IPropertySymbol)x, (IPropertySymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.RangeVariable:
                        return RangeVariablesAreEquivalent((IRangeVariableSymbol)x, (IRangeVariableSymbol)y);
                    case SymbolKind.TypeParameter:
                        return TypeParametersAreEquivalent((ITypeParameterSymbol)x, (ITypeParameterSymbol)y, equivalentTypesWithDifferingAssemblies);
                    case SymbolKind.Preprocessing:
                        return PreprocessingSymbolsAreEquivalent((IPreprocessingSymbol)x, (IPreprocessingSymbol)y);
                    default:
                        return false;
                }
            }

            private bool ArrayTypesAreEquivalent(IArrayTypeSymbol x, IArrayTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return
                    x.Rank == y.Rank &&
                    AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.ElementType, y.ElementType, equivalentTypesWithDifferingAssemblies);
            }

            private bool AssembliesAreEquivalent(IAssemblySymbol x, IAssemblySymbol y)
            {
                return _symbolEquivalenceComparer._assemblyComparerOpt?.Equals(x, y) ?? true;
            }

            private bool DynamicTypesAreEquivalent(IDynamicTypeSymbol x, IDynamicTypeSymbol y)
            {
                return true;
            }

            private bool FieldsAreEquivalent(IFieldSymbol x, IFieldSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return
                    x.Name == y.Name &&
                    AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool LabelsAreEquivalent(ILabelSymbol x, ILabelSymbol y)
            {
                return
                    x.Name == y.Name &&
                    HaveSameLocation(x, y);
            }

            private bool LocalsAreEquivalent(ILocalSymbol x, ILocalSymbol y)
            {
                return HaveSameLocation(x, y);
            }

            private bool MethodsAreEquivalent(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                if (!AreCompatibleMethodKinds(x.MethodKind, y.MethodKind))
                {
                    return false;
                }

                if (x.MethodKind == MethodKind.ReducedExtension)
                {
                    var rx = x.ReducedFrom;
                    var ry = y.ReducedFrom;

                    // reduced from symbols are equivalent
                    if (!AreEquivalent(rx, ry, equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }

                    // receiver types are equivalent
                    if (!AreEquivalent(x.ReceiverType, y.ReceiverType, equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }
                }
                else
                {
                    if (x.MethodKind == MethodKind.AnonymousFunction ||
                        x.MethodKind == MethodKind.LocalFunction)
                    {
                        // Treat local and anonymous functions just like we do ILocalSymbols.  
                        // They're only equivalent if they have the same location.
                        return HaveSameLocation(x, y);
                    }

                    if (IsPartialMethodDefinitionPart(x) != IsPartialMethodDefinitionPart(y) ||
                        IsPartialMethodImplementationPart(x) != IsPartialMethodImplementationPart(y) ||
                        x.IsDefinition != y.IsDefinition ||
                        IsConstructedFromSelf(x) != IsConstructedFromSelf(y) ||
                        x.Arity != y.Arity ||
                        x.Parameters.Length != y.Parameters.Length ||
                        x.Name != y.Name)
                    {
                        return false;
                    }

                    var checkContainingType = CheckContainingType(x);
                    if (checkContainingType)
                    {
                        if (!AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies))
                        {
                            return false;
                        }
                    }

                    if (!ParametersAreEquivalent(x.Parameters, y.Parameters, equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }

                    if (!ReturnTypesAreEquivalent(x, y, equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }
                }

                // If it's an unconstructed method, then we don't need to check the type arguments.
                if (IsConstructedFromSelf(x))
                {
                    return true;
                }

                return TypeArgumentsAreEquivalent(x.TypeArguments, y.TypeArguments, equivalentTypesWithDifferingAssemblies);
            }

            private bool AreCompatibleMethodKinds(MethodKind kind1, MethodKind kind2)
            {
                if (kind1 == kind2)
                {
                    return true;
                }

                if ((kind1 == MethodKind.Ordinary && kind2.IsPropertyAccessor()) ||
                    (kind1.IsPropertyAccessor() && kind2 == MethodKind.Ordinary))
                {
                    return true;
                }

                // User-defined and Built-in operators are comparable
                if ((kind1 == MethodKind.BuiltinOperator && kind2 == MethodKind.UserDefinedOperator) ||
                    (kind1 == MethodKind.UserDefinedOperator && kind2 == MethodKind.BuiltinOperator))
                {
                    return true;
                }

                return false;
            }

            private static bool HaveSameLocation(ISymbol x, ISymbol y)
            {
                return x.Locations.Length == 1 && y.Locations.Length == 1 &&
                    x.Locations.First().Equals(y.Locations.First());
            }

            private bool ModulesAreEquivalent(IModuleSymbol x, IModuleSymbol y)
            {
                return AssembliesAreEquivalent(x.ContainingAssembly, y.ContainingAssembly) && x.Name == y.Name;
            }

            private bool NamedTypesAreEquivalent(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                // PERF: Avoid multiple virtual calls to fetch the TypeKind property
                var xTypeKind = GetTypeKind(x);
                var yTypeKind = GetTypeKind(y);

                if (xTypeKind == TypeKind.Error ||
                    yTypeKind == TypeKind.Error)
                {
                    // Slow path: x or y is an error type. We need to compare
                    // all the candidates in both.
                    return NamedTypesAreEquivalentError(x, y, equivalentTypesWithDifferingAssemblies);
                }

                // Fast path: we can compare the symbols directly,
                // avoiding any allocations associated with the Unwrap()
                // enumerator.
                return xTypeKind == yTypeKind && HandleNamedTypesWorker(x, y, equivalentTypesWithDifferingAssemblies);
            }

            private bool NamedTypesAreEquivalentError(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                foreach (var type1 in Unwrap(x))
                {
                    var typeKind1 = GetTypeKind(type1);
                    foreach (var type2 in Unwrap(y))
                    {
                        var typeKind2 = GetTypeKind(type2);
                        if (typeKind1 == typeKind2 && HandleNamedTypesWorker(type1, type2, equivalentTypesWithDifferingAssemblies))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Worker for comparing two named types for equivalence. Note: The two
            /// types must have the same TypeKind.
            /// </summary>
            /// <param name="x">The first type to compare</param>
            /// <param name="y">The second type to compare</param>
            /// <param name="equivalentTypesWithDifferingAssemblies">
            /// Map of equivalent non-nested types to be populated, such that each key-value pair of named types are equivalent but reside in different assemblies.
            /// This map is populated only if we are ignoring assemblies for symbol equivalence comparison, i.e. <see cref="_assemblyComparerOpt"/> is true.
            /// </param>
            /// <returns>True if the two types are equivalent.</returns>
            private bool HandleNamedTypesWorker(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                Debug.Assert(GetTypeKind(x) == GetTypeKind(y));

                if (x.IsTupleType || y.IsTupleType)
                {
                    if (x.IsTupleType != y.IsTupleType)
                    {
                        return false;
                    }

                    var xElements = x.TupleElements;
                    var yElements = y.TupleElements;

                    if (xElements.Length != yElements.Length)
                    {
                        return false;
                    }

                    for (var i = 0; i < xElements.Length; i++)
                    {
                        if (!AreEquivalent(xElements[i].Type, yElements[i].Type, equivalentTypesWithDifferingAssemblies))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (x.IsDefinition != y.IsDefinition ||
                    IsConstructedFromSelf(x) != IsConstructedFromSelf(y) ||
                    x.Arity != y.Arity ||
                    x.Name != y.Name ||
                    x.IsAnonymousType != y.IsAnonymousType ||
                    x.IsUnboundGenericType != y.IsUnboundGenericType)
                {
                    return false;
                }

                if (!AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies))
                {
                    return false;
                }

                // Above check makes sure that the containing assemblies are considered the same by the assembly comparer being used.
                // If they are in fact not the same (have different name) and the caller requested to know about such types add {x, y} 
                // to equivalentTypesWithDifferingAssemblies map.
                if (equivalentTypesWithDifferingAssemblies != null &&
                    x.ContainingType == null &&
                    x.ContainingAssembly != null &&
                    !AssemblyIdentityComparer.SimpleNameComparer.Equals(x.ContainingAssembly.Name, y.ContainingAssembly.Name) &&
                    !equivalentTypesWithDifferingAssemblies.ContainsKey(x))
                {
                    equivalentTypesWithDifferingAssemblies.Add(x, y);
                }

                if (x.IsAnonymousType)
                {
                    return HandleAnonymousTypes(x, y, equivalentTypesWithDifferingAssemblies);
                }

                // They look very similar at this point.  In the case of non constructed types, we're
                // done.  However, if they are constructed, then their type arguments have to match
                // as well.
                return
                    IsConstructedFromSelf(x) ||
                    x.IsUnboundGenericType ||
                    TypeArgumentsAreEquivalent(x.TypeArguments, y.TypeArguments, equivalentTypesWithDifferingAssemblies);
            }

            private bool ParametersAreEquivalent(
                ImmutableArray<IParameterSymbol> xParameters,
                ImmutableArray<IParameterSymbol> yParameters,
                Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
                bool compareParameterName = false,
                bool isParameterNameCaseSensitive = false)
            {
                // Note the special parameter comparer we pass in.  We do this so we don't end up
                // infinitely looping between parameters -> type parameters -> methods -> parameters
                var count = xParameters.Length;
                if (yParameters.Length != count)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!_symbolEquivalenceComparer.ParameterEquivalenceComparer.Equals(xParameters[i], yParameters[i], equivalentTypesWithDifferingAssemblies, compareParameterName, isParameterNameCaseSensitive))
                    {
                        return false;
                    }
                }

                return true;
            }

            internal bool ReturnTypesAreEquivalent(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies = null)
            {
                return _symbolEquivalenceComparer.SignatureTypeEquivalenceComparer.Equals(x.ReturnType, y.ReturnType, equivalentTypesWithDifferingAssemblies) &&
                       AreEquivalent(x.ReturnTypeCustomModifiers, y.ReturnTypeCustomModifiers, equivalentTypesWithDifferingAssemblies);
            }

            private bool TypeArgumentsAreEquivalent(ImmutableArray<ITypeSymbol> xTypeArguments, ImmutableArray<ITypeSymbol> yTypeArguments, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                var count = xTypeArguments.Length;
                if (yTypeArguments.Length != count)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!AreEquivalent(xTypeArguments[i], yTypeArguments[i], equivalentTypesWithDifferingAssemblies))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool HandleAnonymousTypes(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                if (x.TypeKind == TypeKind.Delegate)
                {
                    return AreEquivalent(x.DelegateInvokeMethod, y.DelegateInvokeMethod, equivalentTypesWithDifferingAssemblies);
                }
                else
                {
                    var xMembers = x.GetValidAnonymousTypeProperties();
                    var yMembers = y.GetValidAnonymousTypeProperties();

                    var xMembersEnumerator = xMembers.GetEnumerator();
                    var yMembersEnumerator = yMembers.GetEnumerator();

                    while (xMembersEnumerator.MoveNext())
                    {
                        if (!yMembersEnumerator.MoveNext())
                        {
                            return false;
                        }

                        var p1 = xMembersEnumerator.Current;
                        var p2 = yMembersEnumerator.Current;

                        if (p1.Name != p2.Name ||
                            p1.IsReadOnly != p2.IsReadOnly ||
                            !AreEquivalent(p1.Type, p2.Type, equivalentTypesWithDifferingAssemblies))
                        {
                            return false;
                        }
                    }

                    return !yMembersEnumerator.MoveNext();
                }
            }

            private bool NamespacesAreEquivalent(INamespaceSymbol x, INamespaceSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                if (x.IsGlobalNamespace != y.IsGlobalNamespace ||
                    x.Name != y.Name)
                {
                    return false;
                }

                if (x.IsGlobalNamespace && _symbolEquivalenceComparer._assemblyComparerOpt == null)
                {
                    // No need to compare the containers of global namespace when assembly identities are ignored.
                    return true;
                }

                return AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool ParametersAreEquivalent(IParameterSymbol x, IParameterSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return
                    x.IsRefOrOut() == y.IsRefOrOut() &&
                    x.Name == y.Name &&
                    AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.Type, y.Type, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool PointerTypesAreEquivalent(IPointerTypeSymbol x, IPointerTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return
                    AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.PointedAtType, y.PointedAtType, equivalentTypesWithDifferingAssemblies);
            }

            private bool PropertiesAreEquivalent(IPropertySymbol x, IPropertySymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                if (x.ContainingType.IsAnonymousType && y.ContainingType.IsAnonymousType)
                {
                    // We can short circuit here and just use the symbols themselves to determine
                    // equality.  This will properly handle things like the VB case where two
                    // anonymous types will be considered the same if they have properties that
                    // differ in casing.
                    if (x.Equals(y))
                    {
                        return true;
                    }
                }

                return
                    x.IsIndexer == y.IsIndexer &&
                    x.MetadataName == y.MetadataName &&
                    x.Parameters.Length == y.Parameters.Length &&
                    ParametersAreEquivalent(x.Parameters, y.Parameters, equivalentTypesWithDifferingAssemblies) &&
                    AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool EventsAreEquivalent(IEventSymbol x, IEventSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                return
                    x.Name == y.Name &&
                    AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool TypeParametersAreEquivalent(ITypeParameterSymbol x, ITypeParameterSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies)
            {
                Debug.Assert(
                    (x.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(x.DeclaringMethod)) ||
                    (x.TypeParameterKind == TypeParameterKind.Type && IsConstructedFromSelf(x.ContainingType)) ||
                    x.TypeParameterKind == TypeParameterKind.Cref);
                Debug.Assert(
                    (y.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(y.DeclaringMethod)) ||
                    (y.TypeParameterKind == TypeParameterKind.Type && IsConstructedFromSelf(y.ContainingType)) ||
                    y.TypeParameterKind == TypeParameterKind.Cref);

                if (x.Ordinal != y.Ordinal ||
                    x.TypeParameterKind != y.TypeParameterKind)
                {
                    return false;
                }

                // If this is a method type parameter, and we are in 'non-recurse' mode (because
                // we're comparing method parameters), then we're done at this point.  The types are
                // equal.
                if (x.TypeParameterKind == TypeParameterKind.Method && _compareMethodTypeParametersByIndex)
                {
                    return true;
                }

                if (x.TypeParameterKind == TypeParameterKind.Type && x.ContainingType.IsAnonymousType)
                {
                    // Anonymous type type parameters compare by index as well to prevent
                    // recursion.
                    return true;
                }

                if (x.TypeParameterKind == TypeParameterKind.Cref)
                {
                    return true;
                }

                return AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
            }

            private bool RangeVariablesAreEquivalent(IRangeVariableSymbol x, IRangeVariableSymbol y)
            {
                return HaveSameLocation(x, y);
            }

            private bool PreprocessingSymbolsAreEquivalent(IPreprocessingSymbol x, IPreprocessingSymbol y)
            {
                return x.Name == y.Name;
            }
        }
    }
}
