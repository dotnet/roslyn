// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public int GetHashCode(ISymbol x, int currentHash)
            {
                if (x == null)
                {
                    return 0;
                }

                x = UnwrapAlias(x);

                // Special case.  If we're comparing signatures then we want to compare 'object'
                // and 'dynamic' as the same.  However, since they're different types, we don't
                // want to bail out using the above check.

                if (x.Kind == SymbolKind.DynamicType ||
                    (_objectAndDynamicCompareEqually && IsObjectType(x)))
                {
                    return Hash.Combine(typeof(IDynamicTypeSymbol), currentHash);
                }

                return GetHashCodeWorker(x, currentHash);
            }

            private int GetHashCodeWorker(ISymbol x, int currentHash)
            {
                switch (x.Kind)
                {
                    case SymbolKind.ArrayType:
                        return CombineHashCodes((IArrayTypeSymbol)x, currentHash);
                    case SymbolKind.Assembly:
                        return CombineHashCodes((IAssemblySymbol)x, currentHash);
                    case SymbolKind.Event:
                        return CombineHashCodes((IEventSymbol)x, currentHash);
                    case SymbolKind.Field:
                        return CombineHashCodes((IFieldSymbol)x, currentHash);
                    case SymbolKind.Label:
                        return CombineHashCodes((ILabelSymbol)x, currentHash);
                    case SymbolKind.Local:
                        return CombineHashCodes((ILocalSymbol)x, currentHash);
                    case SymbolKind.Method:
                        return CombineHashCodes((IMethodSymbol)x, currentHash);
                    case SymbolKind.NetModule:
                        return CombineHashCodes((IModuleSymbol)x, currentHash);
                    case SymbolKind.NamedType:
                        return CombineHashCodes((INamedTypeSymbol)x, currentHash);
                    case SymbolKind.Namespace:
                        return CombineHashCodes((INamespaceSymbol)x, currentHash);
                    case SymbolKind.Parameter:
                        return CombineHashCodes((IParameterSymbol)x, currentHash);
                    case SymbolKind.PointerType:
                        return CombineHashCodes((IPointerTypeSymbol)x, currentHash);
                    case SymbolKind.Property:
                        return CombineHashCodes((IPropertySymbol)x, currentHash);
                    case SymbolKind.RangeVariable:
                        return CombineHashCodes((IRangeVariableSymbol)x, currentHash);
                    case SymbolKind.TypeParameter:
                        return CombineHashCodes((ITypeParameterSymbol)x, currentHash);
                    case SymbolKind.Preprocessing:
                        return CombineHashCodes((IPreprocessingSymbol)x, currentHash);
                    default:
                        return -1;
                }
            }

            private int CombineHashCodes(IArrayTypeSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Rank,
                    GetHashCode(x.ElementType, currentHash));
            }

            private int CombineHashCodes(IAssemblySymbol x, int currentHash)
            {
                return Hash.Combine(_symbolEquivalenceComparer._assemblyComparerOpt?.GetHashCode(x) ?? 0, currentHash);
            }

            private int CombineHashCodes(IFieldSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash));
            }

            private int CombineHashCodes(ILabelSymbol x, int currentHash)
            {
                return
                    Hash.Combine(x.Name,
                    Hash.Combine(x.Locations.FirstOrDefault(), currentHash));
            }

            private int CombineHashCodes(ILocalSymbol x, int currentHash)
            {
                return Hash.Combine(x.Locations.FirstOrDefault(), currentHash);
            }

            private static int CombineHashCodes<T>(ImmutableArray<T> array, int currentHash, Func<int, T, int> func)
            {
                return array.Aggregate<int, T>(currentHash, func);
            }

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
            {
                return CombineHashCodes(x.ContainingAssembly, Hash.Combine(x.Name, currentHash));
            }

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
                    Hash.Combine(x.IsDefinition,
                    Hash.Combine(IsConstructedFromSelf(x),
                    Hash.Combine(x.Arity,
                    Hash.Combine((int)GetTypeKind(x),
                    Hash.Combine(x.Name,
                    Hash.Combine(x.IsAnonymousType,
                    Hash.Combine(x.IsUnboundGenericType,
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
                if (x.IsGlobalNamespace && _symbolEquivalenceComparer._assemblyComparerOpt == null)
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
                    (x.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(x.DeclaringMethod)) ||
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

            private int CombineHashCodes(IRangeVariableSymbol x, int currentHash)
            {
                return Hash.Combine(x.Locations.FirstOrDefault(), currentHash);
            }

            private int CombineHashCodes(IPreprocessingSymbol x, int currentHash)
            {
                return Hash.Combine(x.GetHashCode(), currentHash);
            }
        }
    }
}
