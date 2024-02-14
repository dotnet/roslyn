// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        private class GetHashCodeVisitor
        {
            private readonly SymbolEquivalenceComparer _symbolEquivalenceComparer;
            private readonly bool _compareMethodTypeParametersByIndex;
            private readonly bool _objectAndDynamicCompareEqually;
            private readonly Func<int, IParameterSymbol, int> _parameterAggregator;
            private readonly Func<int, ISymbol, int> _symbolAggregator;

            public GetHashCodeVisitor(
                SymbolEquivalenceComparer symbolEquivalenceComparer,
                bool compareMethodTypeParametersByIndex,
                bool objectAndDynamicCompareEqually)
            {
                _symbolEquivalenceComparer = symbolEquivalenceComparer;
                _compareMethodTypeParametersByIndex = compareMethodTypeParametersByIndex;
                _objectAndDynamicCompareEqually = objectAndDynamicCompareEqually;
                _parameterAggregator = (acc, sym) => Hash.Combine(symbolEquivalenceComparer.ParameterEquivalenceComparer.GetHashCode(sym), acc);
                _symbolAggregator = (acc, sym) => GetHashCode(sym, acc);
            }

            public int GetHashCode(ISymbol? x, int currentHash)
            {
                if (x == null)
                    return 0;

                x = UnwrapAlias(x);

                // Special case.  If we're comparing signatures then we want to compare 'object'
                // and 'dynamic' as the same.  However, since they're different types, we don't
                // want to bail out using the above check.

                if (x.Kind == SymbolKind.DynamicType ||
                    (_objectAndDynamicCompareEqually && IsObjectType(x)))
                {
                    return Hash.Combine(GetNullableAnnotationsHashCode((ITypeSymbol)x), Hash.Combine(typeof(IDynamicTypeSymbol), currentHash));
                }

                return GetHashCodeWorker(x, currentHash);
            }

            private int GetNullableAnnotationsHashCode(ITypeSymbol type)
                => _symbolEquivalenceComparer._ignoreNullableAnnotations ? 0 : ((int)type.NullableAnnotation).GetHashCode();

            private int GetHashCodeWorker(ISymbol x, int currentHash)
                => x.Kind switch
                {
                    SymbolKind.ArrayType => CombineHashCodes((IArrayTypeSymbol)x, currentHash),
                    SymbolKind.Assembly => CombineHashCodes((IAssemblySymbol)x, currentHash),
                    SymbolKind.Event => CombineHashCodes((IEventSymbol)x, currentHash),
                    SymbolKind.Field => CombineHashCodes((IFieldSymbol)x, currentHash),
                    SymbolKind.Label => CombineHashCodes((ILabelSymbol)x, currentHash),
                    SymbolKind.Local => CombineHashCodes((ILocalSymbol)x, currentHash),
                    SymbolKind.Method => CombineHashCodes((IMethodSymbol)x, currentHash),
                    SymbolKind.NetModule => CombineHashCodes((IModuleSymbol)x, currentHash),
                    SymbolKind.NamedType => CombineHashCodes((INamedTypeSymbol)x, currentHash),
                    SymbolKind.Namespace => CombineHashCodes((INamespaceSymbol)x, currentHash),
                    SymbolKind.Parameter => CombineHashCodes((IParameterSymbol)x, currentHash),
                    SymbolKind.PointerType => CombineHashCodes((IPointerTypeSymbol)x, currentHash),
                    SymbolKind.Property => CombineHashCodes((IPropertySymbol)x, currentHash),
                    SymbolKind.RangeVariable => CombineHashCodes((IRangeVariableSymbol)x, currentHash),
                    SymbolKind.TypeParameter => CombineHashCodes((ITypeParameterSymbol)x, currentHash),
                    SymbolKind.Preprocessing => CombineHashCodes((IPreprocessingSymbol)x, currentHash),
                    _ => -1,
                };

            private int CombineHashCodes(IArrayTypeSymbol x, int currentHash)
            {
                return
                    Hash.Combine(GetNullableAnnotationsHashCode(x),
                    Hash.Combine(x.Rank,
                    GetHashCode(x.ElementType, currentHash)));
            }

            private int CombineHashCodes(IAssemblySymbol x, int currentHash)
                => Hash.Combine(_symbolEquivalenceComparer._assemblyComparer?.GetHashCode(x) ?? 0, currentHash);

            private int CombineHashCodes(IFieldSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash));
            }

            private static int CombineHashCodes(ILabelSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Name,
                    Hash.Combine(x.Locations.FirstOrDefault(), currentHash));
            }

            private static int CombineHashCodes(ILocalSymbol x, int currentHash)
                => Hash.Combine(x.Locations.FirstOrDefault(), currentHash);

            private static int CombineHashCodes<T>(ImmutableArray<T> array, int currentHash, Func<int, T, int> func)
                => array.Aggregate<int, T>(currentHash, func);

            private int CombineHashCodes(IMethodSymbol x, int currentHash)
            {
                currentHash = Hash.Combine(x.MetadataName, currentHash);
                if (x.MethodKind == MethodKind.AnonymousFunction)
                {
                    return Hash.Combine(x.Locations.FirstOrDefault(), currentHash);
                }

                currentHash =
                    Hash.Combine(IsPartialMethodImplementationPart(x),
                    Hash.Combine(IsPartialMethodDefinitionPart(x),
                    Hash.Combine(x.IsDefinition,
                    Hash.Combine(IsConstructedFromSelf(x),
                    Hash.Combine(x.Arity,
                    Hash.Combine(x.Parameters.Length,
                    Hash.Combine(x.Name, currentHash)))))));

                var checkContainingType = CheckContainingType(x);
                if (checkContainingType)
                {
                    currentHash = GetHashCode(x.ContainingSymbol, currentHash);
                }

                currentHash =
                    CombineHashCodes(x.Parameters, currentHash, _parameterAggregator);

                return IsConstructedFromSelf(x)
                    ? currentHash
                    : CombineHashCodes(x.TypeArguments, currentHash, _symbolAggregator);
            }

            private int CombineHashCodes(IModuleSymbol x, int currentHash)
                => CombineHashCodes(x.ContainingAssembly, Hash.Combine(x.Name, currentHash));

            private int CombineHashCodes(INamedTypeSymbol x, int currentHash)
            {
                currentHash = CombineNamedTypeHashCode(x, currentHash);

                if (x is IErrorTypeSymbol errorType)
                {
                    foreach (var candidate in errorType.CandidateSymbols)
                    {
                        if (candidate is INamedTypeSymbol candidateNamedType)
                        {
                            currentHash = CombineNamedTypeHashCode(candidateNamedType, currentHash);
                        }
                    }
                }

                return currentHash;
            }

            private int CombineNamedTypeHashCode(INamedTypeSymbol x, int currentHash)
            {
                if (x.IsTupleType)
                {
                    return Hash.Combine(currentHash, Hash.CombineValues(x.TupleElements));
                }

                // If we want object and dynamic to be the same, and this is 'object', then return
                // the same hash we do for 'dynamic'.
                currentHash =
                    Hash.Combine((int)GetTypeKind(x),
                    Hash.Combine(IsConstructedFromSelf(x),
                    Hash.Combine(x.Arity,
                    Hash.Combine(x.Name,
                    Hash.Combine(x.IsAnonymousType,
                    Hash.Combine(x.IsUnboundGenericType,
                    Hash.Combine(GetNullableAnnotationsHashCode(x),
                    GetHashCode(x.ContainingSymbol, currentHash))))))));

                if (x.IsAnonymousType)
                {
                    return CombineAnonymousTypeHashCode(x, currentHash);
                }

                return IsConstructedFromSelf(x) || x.IsUnboundGenericType
                    ? currentHash
                    : CombineHashCodes(x.TypeArguments, currentHash, _symbolAggregator);
            }

            private int CombineAnonymousTypeHashCode(INamedTypeSymbol x, int currentHash)
            {
                if (x.TypeKind == TypeKind.Delegate)
                {
                    return GetHashCode(x.DelegateInvokeMethod, currentHash);
                }
                else
                {
                    var xMembers = x.GetValidAnonymousTypeProperties();

                    return xMembers.Aggregate(currentHash, (a, p) =>
                        {
                            return Hash.Combine(p.Name,
                                Hash.Combine(p.IsReadOnly,
                                GetHashCode(p.Type, a)));
                        });
                }
            }

            private int CombineHashCodes(INamespaceSymbol x, int currentHash)
            {
                if (x.IsGlobalNamespace && _symbolEquivalenceComparer._assemblyComparer == null)
                {
                    // Exclude global namespace's container's hash when assemblies can differ.
                    return Hash.Combine(x.Name, currentHash);
                }

                return
                    Hash.Combine(x.IsGlobalNamespace,
                    Hash.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash)));
            }

            private int CombineHashCodes(IParameterSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.IsRefOrOut(),
                    Hash.Combine(x.Name,
                    GetHashCode(x.Type,
                    GetHashCode(x.ContainingSymbol, currentHash))));
            }

            private int CombineHashCodes(IPointerTypeSymbol x, int currentHash)
            {
                return
                    Hash.Combine(typeof(IPointerTypeSymbol).GetHashCode(),
                    GetHashCode(x.PointedAtType, currentHash));
            }

            private int CombineHashCodes(IPropertySymbol x, int currentHash)
            {
                currentHash =
                    Hash.Combine(x.IsIndexer,
                    Hash.Combine(x.Name,
                    Hash.Combine(x.Parameters.Length,
                    GetHashCode(x.ContainingSymbol, currentHash))));

                return CombineHashCodes(x.Parameters, currentHash, _parameterAggregator);
            }

            private int CombineHashCodes(IEventSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash));
            }

            public int CombineHashCodes(ITypeParameterSymbol x, int currentHash)
            {
                Debug.Assert(
                    (x.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(x.DeclaringMethod!)) ||
                    (x.TypeParameterKind == TypeParameterKind.Type && IsConstructedFromSelf(x.ContainingType)) ||
                    x.TypeParameterKind == TypeParameterKind.Cref);

                currentHash =
                    Hash.Combine(x.Ordinal,
                    Hash.Combine((int)x.TypeParameterKind, currentHash));

                if (x.TypeParameterKind == TypeParameterKind.Method && _compareMethodTypeParametersByIndex)
                {
                    return currentHash;
                }

                if (x.TypeParameterKind == TypeParameterKind.Type && x.ContainingType.IsAnonymousType)
                {
                    // Anonymous type type parameters compare by index as well to prevent
                    // recursion.
                    return currentHash;
                }

                if (x.TypeParameterKind == TypeParameterKind.Cref)
                {
                    return currentHash;
                }

                return
                    GetHashCode(x.ContainingSymbol, currentHash);
            }

            private static int CombineHashCodes(IRangeVariableSymbol x, int currentHash)
                => Hash.Combine(x.Locations.FirstOrDefault(), currentHash);

            private static int CombineHashCodes(IPreprocessingSymbol x, int currentHash)
                => Hash.Combine(x.GetHashCode(), currentHash);
        }
    }
}
