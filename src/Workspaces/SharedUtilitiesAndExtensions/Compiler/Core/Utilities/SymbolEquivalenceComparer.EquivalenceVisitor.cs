// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal partial class SymbolEquivalenceComparer
{
    private sealed class EquivalenceVisitor(
        SymbolEquivalenceComparer symbolEquivalenceComparer,
        bool compareMethodTypeParametersByIndex)
    {
        public bool AreEquivalent(ISymbol? x, ISymbol? y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null)
                return false;

            var xKind = GetKindAndUnwrapAlias(ref x);
            var yKind = GetKindAndUnwrapAlias(ref y);

            // Normally, if they're different types, then they're not the same.
            if (xKind != yKind)
            {
                // Special case.  If we're comparing signatures then we want to compare 'object' and 'dynamic' as the
                // same.  However, since they're different types, we don't want to bail out using the above check.
                if (symbolEquivalenceComparer._objectAndDynamicCompareEqually)
                {
                    if ((xKind == SymbolKind.DynamicType && IsObjectType(y)) ||
                        (yKind == SymbolKind.DynamicType && IsObjectType(x)))
                    {
                        return true;
                    }
                }

                if (symbolEquivalenceComparer._arrayAndReadOnlySpanCompareEqually)
                {
                    if (xKind == SymbolKind.ArrayType && y.IsReadOnlySpan())
                    {
                        return AreArrayAndReadOnlySpanEquivalent((IArrayTypeSymbol)x, (INamedTypeSymbol)y, equivalentTypesWithDifferingAssemblies);
                    }
                    else if (x.IsReadOnlySpan() && yKind == SymbolKind.ArrayType)
                    {
                        return AreArrayAndReadOnlySpanEquivalent((IArrayTypeSymbol)y, (INamedTypeSymbol)x, equivalentTypesWithDifferingAssemblies);
                    }
                }

                return false;
            }

            return AreEquivalentWorker(x, y, xKind, equivalentTypesWithDifferingAssemblies);
        }

        private bool AreArrayAndReadOnlySpanEquivalent(IArrayTypeSymbol array, INamedTypeSymbol readOnlySpanType, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            if (array.Rank != 1)
                return false;

            return AreEquivalent(array.ElementType, readOnlySpanType.TypeArguments.Single(), equivalentTypesWithDifferingAssemblies);
        }

        internal bool AreEquivalent(CustomModifier x, CustomModifier y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
            => x.IsOptional == y.IsOptional && AreEquivalent(x.Modifier, y.Modifier, equivalentTypesWithDifferingAssemblies);

        internal bool AreEquivalent(ImmutableArray<CustomModifier> x, ImmutableArray<CustomModifier> y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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

        private bool NullableAnnotationsEquivalent(ITypeSymbol x, ITypeSymbol y)
        {
            if (symbolEquivalenceComparer._ignoreNullableAnnotations)
                return true;

            if (x.NullableAnnotation == y.NullableAnnotation)
                return true;

            // Workaround compiler issues where sometimes a particular symbol will have 'none' for the annotation
            return (x.NullableAnnotation, y.NullableAnnotation) switch
            {
                (NullableAnnotation.None, NullableAnnotation.NotAnnotated) => true,
                (NullableAnnotation.NotAnnotated, NullableAnnotation.None) => true,
                _ => false,
            };
        }

        private bool AreEquivalentWorker(ISymbol x, ISymbol y, SymbolKind k, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            Debug.Assert(x.Kind == y.Kind && x.Kind == k);
            return k switch
            {
                SymbolKind.ArrayType => ArrayTypesAreEquivalent((IArrayTypeSymbol)x, (IArrayTypeSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Assembly => AssembliesAreEquivalent((IAssemblySymbol)x, (IAssemblySymbol)y),
                SymbolKind.DynamicType => NullableAnnotationsEquivalent((IDynamicTypeSymbol)x, (IDynamicTypeSymbol)y),
                SymbolKind.Event => EventsAreEquivalent((IEventSymbol)x, (IEventSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Field => FieldsAreEquivalent((IFieldSymbol)x, (IFieldSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Label => LabelsAreEquivalent((ILabelSymbol)x, (ILabelSymbol)y),
                SymbolKind.Local => LocalsAreEquivalent((ILocalSymbol)x, (ILocalSymbol)y),
                SymbolKind.Method => MethodsAreEquivalent((IMethodSymbol)x, (IMethodSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.NetModule => ModulesAreEquivalent((IModuleSymbol)x, (IModuleSymbol)y),
                SymbolKind.NamedType => NamedTypesAreEquivalent((INamedTypeSymbol)x, (INamedTypeSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.ErrorType => NamedTypesAreEquivalent((INamedTypeSymbol)x, (INamedTypeSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Namespace => NamespacesAreEquivalent((INamespaceSymbol)x, (INamespaceSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Parameter => ParametersAreEquivalent((IParameterSymbol)x, (IParameterSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.PointerType => PointerTypesAreEquivalent((IPointerTypeSymbol)x, (IPointerTypeSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Property => PropertiesAreEquivalent((IPropertySymbol)x, (IPropertySymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.RangeVariable => RangeVariablesAreEquivalent((IRangeVariableSymbol)x, (IRangeVariableSymbol)y),
                SymbolKind.TypeParameter => TypeParametersAreEquivalent((ITypeParameterSymbol)x, (ITypeParameterSymbol)y, equivalentTypesWithDifferingAssemblies),
                SymbolKind.Preprocessing => PreprocessingSymbolsAreEquivalent((IPreprocessingSymbol)x, (IPreprocessingSymbol)y),
                SymbolKind.FunctionPointerType => FunctionPointerTypesAreEquivalent((IFunctionPointerTypeSymbol)x, (IFunctionPointerTypeSymbol)y, equivalentTypesWithDifferingAssemblies),
                _ => false,
            };
        }

        private bool ArrayTypesAreEquivalent(IArrayTypeSymbol x, IArrayTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            return
                x.Rank == y.Rank &&
                AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.ElementType, y.ElementType, equivalentTypesWithDifferingAssemblies) &&
                NullableAnnotationsEquivalent(x, y);
        }

        private bool AssembliesAreEquivalent(IAssemblySymbol x, IAssemblySymbol y)
            => symbolEquivalenceComparer._assemblyComparer?.Equals(x, y) ?? true;

        private bool FieldsAreEquivalent(IFieldSymbol x, IFieldSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            return
                x.Name == y.Name &&
                AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
        }

        private static bool LabelsAreEquivalent(ILabelSymbol x, ILabelSymbol y)
        {
            return
                x.Name == y.Name &&
                HaveSameLocation(x, y);
        }

        private static bool LocalsAreEquivalent(ILocalSymbol x, ILocalSymbol y)
            => HaveSameLocation(x, y);

        private bool MethodsAreEquivalent(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies, bool considerReturnRefKinds = false)
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
                if (x.MethodKind is MethodKind.AnonymousFunction or
                    MethodKind.LocalFunction)
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

                if (considerReturnRefKinds && !AreRefKindsEquivalent(x.RefKind, y.RefKind, distinguishRefFromOut: false))
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

        private static bool AreCompatibleMethodKinds(MethodKind kind1, MethodKind kind2)
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
            => AssembliesAreEquivalent(x.ContainingAssembly, y.ContainingAssembly) && x.Name == y.Name;

        private bool NamedTypesAreEquivalent(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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

        private bool NamedTypesAreEquivalentError(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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
        /// This map is populated only if we are ignoring assemblies for symbol equivalence comparison, i.e. <see cref="_assemblyComparer"/> is true.
        /// </param>
        /// <returns>True if the two types are equivalent.</returns>
        private bool HandleNamedTypesWorker(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            Debug.Assert(GetTypeKind(x) == GetTypeKind(y));

            // If one is a tuple, both must be tuples.
            if (x.IsTupleType != y.IsTupleType)
                return false;

            // If one is nint/nuint, the other must be as well.
            if (x.IsNativeIntegerType != y.IsNativeIntegerType)
                return false;

            // If one is void, the other must be as well
            if (x.IsSystemVoid() != y.IsSystemVoid())
                return false;

            // If a tuple, make sure the members are equivalent.
            if (x.IsTupleType)
                return HandleTupleTypes(x, y, equivalentTypesWithDifferingAssemblies);

            // If a native int, make sure the sign matches.
            if (x.IsNativeIntegerType)
                return x.SpecialType == y.SpecialType;

            // If both are void, they're equivalent.
            if (x.IsSystemVoid())
                return true;

            if (IsConstructedFromSelf(x) != IsConstructedFromSelf(y) ||
                x.Arity != y.Arity ||
                x.Name != y.Name ||
                x.IsAnonymousType != y.IsAnonymousType ||
                x.IsUnboundGenericType != y.IsUnboundGenericType ||
                !NullableAnnotationsEquivalent(x, y))
            {
                return false;
            }

            if (x.Kind == SymbolKind.ErrorType &&
                x.ContainingSymbol is INamespaceSymbol xNamespace &&
                y.ContainingSymbol is INamespaceSymbol yNamespace)
            {
                Debug.Assert(y.Kind == SymbolKind.ErrorType);

                // For error types, we just ensure that the containing namespaces are equivalent up to the root.
                while (true)
                {
                    if (xNamespace.Name != yNamespace.Name)
                        return false;

                    // Error namespaces don't set the IsGlobalNamespace bit unfortunately.  So we just do the
                    // nominal check to see if we've actually hit the root.
                    if (xNamespace.Name == "")
                        break;

                    xNamespace = xNamespace.ContainingNamespace;
                    yNamespace = yNamespace.ContainingNamespace;
                }
            }
            else
            {
                if (!AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies))
                    return false;

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
            }

            if (x.IsAnonymousType)
                return HandleAnonymousTypes(x, y, equivalentTypesWithDifferingAssemblies);

            // They look very similar at this point.  In the case of non constructed types, we're
            // done.  However, if they are constructed, then their type arguments have to match
            // as well.
            return
                IsConstructedFromSelf(x) ||
                x.IsUnboundGenericType ||
                TypeArgumentsAreEquivalent(x.TypeArguments, y.TypeArguments, equivalentTypesWithDifferingAssemblies);
        }

        private bool HandleTupleTypes(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            Debug.Assert(y.IsTupleType);

            var xElements = x.TupleElements;
            var yElements = y.TupleElements;

            if (xElements.Length != yElements.Length)
                return false;

            // Check names first if necessary.
            if (symbolEquivalenceComparer._tupleNamesMustMatch)
            {
                for (var i = 0; i < xElements.Length; i++)
                {
                    var xElement = xElements[i];
                    var yElement = yElements[i];
                    if (xElement.Name != yElement.Name)
                        return false;
                }
            }

            // If we're validating the actual unconstructed ValueTuple type itself, we're done at this point.  No
            // need to check field types.
            //
            // For VB we have to unwrap tuples to their underlying types to do this check.
            // https://github.com/dotnet/roslyn/issues/42860
            if (IsConstructedFromSelf(x.TupleUnderlyingType ?? x))
                return true;

            for (var i = 0; i < xElements.Length; i++)
            {
                var xElement = xElements[i];
                var yElement = yElements[i];

                if (!AreEquivalent(xElement.Type, yElement.Type, equivalentTypesWithDifferingAssemblies))
                    return false;
            }

            return true;
        }

        private bool ParametersAreEquivalent(
            ImmutableArray<IParameterSymbol> xParameters,
            ImmutableArray<IParameterSymbol> yParameters,
            Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies,
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
                if (!symbolEquivalenceComparer.ParameterEquivalenceComparer.Equals(xParameters[i], yParameters[i], equivalentTypesWithDifferingAssemblies, compareParameterName, isParameterNameCaseSensitive))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool ReturnTypesAreEquivalent(IMethodSymbol x, IMethodSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies = null)
        {
            return symbolEquivalenceComparer.SignatureTypeEquivalenceComparer.Equals(x.ReturnType, y.ReturnType, equivalentTypesWithDifferingAssemblies) &&
                   AreEquivalent(x.ReturnTypeCustomModifiers, y.ReturnTypeCustomModifiers, equivalentTypesWithDifferingAssemblies);
        }

        private bool TypeArgumentsAreEquivalent(ImmutableArray<ITypeSymbol> xTypeArguments, ImmutableArray<ITypeSymbol> yTypeArguments, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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

        private bool HandleAnonymousTypes(INamedTypeSymbol x, INamedTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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

        private bool NamespacesAreEquivalent(INamespaceSymbol x, INamespaceSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            if (x.IsGlobalNamespace != y.IsGlobalNamespace ||
                x.Name != y.Name)
            {
                return false;
            }

            if (x.IsGlobalNamespace && symbolEquivalenceComparer._assemblyComparer == null)
            {
                // No need to compare the containers of global namespace when assembly identities are ignored.
                return true;
            }

            return AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
        }

        private bool ParametersAreEquivalent(IParameterSymbol x, IParameterSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            return
                x.IsRefOrOut() == y.IsRefOrOut() &&
                x.Name == y.Name &&
                AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.Type, y.Type, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
        }

        private bool PointerTypesAreEquivalent(IPointerTypeSymbol x, IPointerTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            return
                AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.PointedAtType, y.PointedAtType, equivalentTypesWithDifferingAssemblies);
        }

        private bool FunctionPointerTypesAreEquivalent(IFunctionPointerTypeSymbol x, IFunctionPointerTypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
            => MethodsAreEquivalent(x.Signature, y.Signature, equivalentTypesWithDifferingAssemblies);

        private bool PropertiesAreEquivalent(IPropertySymbol x, IPropertySymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
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
                IsPartialMethodDefinitionPart(x) == IsPartialMethodDefinitionPart(y) &&
                IsPartialMethodImplementationPart(x) == IsPartialMethodImplementationPart(y) &&
                ParametersAreEquivalent(x.Parameters, y.Parameters, equivalentTypesWithDifferingAssemblies) &&
                AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
        }

        private bool EventsAreEquivalent(IEventSymbol x, IEventSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            return
                x.Name == y.Name &&
                AreEquivalent(x.ContainingSymbol, y.ContainingSymbol, equivalentTypesWithDifferingAssemblies);
        }

        private bool TypeParametersAreEquivalent(ITypeParameterSymbol x, ITypeParameterSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
        {
            Debug.Assert(
                (x.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(x.DeclaringMethod!)) ||
                (x.TypeParameterKind == TypeParameterKind.Type && IsConstructedFromSelf(x.ContainingType)) ||
                x.TypeParameterKind == TypeParameterKind.Cref);
            Debug.Assert(
                (y.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(y.DeclaringMethod!)) ||
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
            if (x.TypeParameterKind == TypeParameterKind.Method && compareMethodTypeParametersByIndex)
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

        private static bool RangeVariablesAreEquivalent(IRangeVariableSymbol x, IRangeVariableSymbol y)
            => HaveSameLocation(x, y);

        private static bool PreprocessingSymbolsAreEquivalent(IPreprocessingSymbol x, IPreprocessingSymbol y)
            => x.Name == y.Name;
    }
}
