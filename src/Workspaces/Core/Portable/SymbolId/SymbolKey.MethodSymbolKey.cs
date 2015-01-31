// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class MethodSymbolKey : AbstractSymbolKey<MethodSymbolKey>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _metadataName;
            private readonly int _arity;
            private readonly SymbolKey[] _typeArgumentKeysOpt;
            private readonly RefKind[] _refKinds;
            private readonly SymbolKey[] _originalParameterTypeKeys;
            private readonly bool _isPartialMethodImplementationPart;
            private readonly SymbolKey _returnType;

            internal MethodSymbolKey(IMethodSymbol symbol, Visitor visitor)
            {
                // First thing we do when creating the symbol ID for a method is create symbol IDs
                // for it's type parameters.  However, we pass ourselves to that type parameter so
                // that it does not recurse back to us.
                foreach (var typeParameter in symbol.TypeParameters)
                {
                    var symbolId = new MethodTypeParameterSymbol(this, typeParameter);
                    visitor.SymbolCache.Add(typeParameter, symbolId);
                }

                _isPartialMethodImplementationPart = symbol.PartialDefinitionPart != null;

                _containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                _metadataName = symbol.MetadataName;
                _arity = symbol.Arity;
                _refKinds = symbol.Parameters.Select(p => p.RefKind).ToArray();
                _originalParameterTypeKeys = symbol.OriginalDefinition.Parameters.Select(p => GetOrCreate(p.Type, visitor)).ToArray();

                if (!symbol.Equals(symbol.ConstructedFrom))
                {
                    _typeArgumentKeysOpt = symbol.TypeArguments.Select(t => GetOrCreate(t, visitor)).ToArray();
                }

                // If this is conversion operator, we must also compare the return type. Otherwise, we'll return
                // an ambiguous result in the case that there are multiple conversions from the same type to different
                // types.
                _returnType = symbol.MethodKind == MethodKind.Conversion
                    ? GetOrCreate(symbol.ReturnType, visitor)
                    : null;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);

                var methods = GetAllSymbols<INamedTypeSymbol>(container).SelectMany(t => Resolve(compilation, t, ignoreAssemblyKey, cancellationToken));
                return CreateSymbolInfo(methods);
            }

            private IEnumerable<IMethodSymbol> Resolve(Compilation compilation, INamedTypeSymbol container, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var comparisonOptions = new ComparisonOptions(compilation.IsCaseSensitive, ignoreAssemblyKey, compareMethodTypeParametersByName: true);
                ITypeSymbol[] typeArguments = null;

                if (_typeArgumentKeysOpt != null)
                {
                    typeArguments = _typeArgumentKeysOpt.Select(a => a.Resolve(compilation, cancellationToken: cancellationToken).Symbol as ITypeSymbol).ToArray();

                    if (typeArguments.Any(a => a == null))
                    {
                        yield break;
                    }
                }

                foreach (var method in container.GetMembers().OfType<IMethodSymbol>())
                {
                    // Quick checks first
                    if (method.MetadataName != _metadataName || method.Arity != _arity || method.Parameters.Length != _originalParameterTypeKeys.Length)
                    {
                        continue;
                    }

                    // Is this a conversion operator? If so, we must also compare the return type.
                    if (_returnType != null)
                    {
                        if (!_returnType.Equals(SymbolKey.Create(method.ReturnType, compilation, cancellationToken), comparisonOptions))
                        {
                            continue;
                        }
                    }

                    if (!ParametersMatch(comparisonOptions, compilation, method.OriginalDefinition.Parameters, _refKinds, _originalParameterTypeKeys, cancellationToken))
                    {
                        continue;
                    }

                    // It matches, so let's return it, but we might have to do some construction first
                    var methodToReturn = method;

                    if (_isPartialMethodImplementationPart)
                    {
                        methodToReturn = methodToReturn.PartialImplementationPart ?? methodToReturn;
                    }

                    if (typeArguments != null)
                    {
                        methodToReturn = methodToReturn.Construct(typeArguments);
                    }

                    yield return methodToReturn;
                }
            }

            internal override bool Equals(MethodSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);
                return
                    other._arity == _arity &&
                    other._refKinds.Length == _refKinds.Length &&
                    other._isPartialMethodImplementationPart == _isPartialMethodImplementationPart &&
                    Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    other._containerKey.Equals(_containerKey, options) &&
                    other._refKinds.SequenceEqual(_refKinds) &&
                    other._originalParameterTypeKeys.SequenceEqual(_originalParameterTypeKeys, comparer) &&
                    SequenceEquals(other._typeArgumentKeysOpt, _typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO: Consider hashing the parameters as well
                return
                    Hash.Combine(_arity,
                    Hash.Combine(_refKinds.Length,
                    Hash.Combine(_isPartialMethodImplementationPart,
                    Hash.Combine(GetHashCode(options.IgnoreCase, _metadataName),
                                 _containerKey.GetHashCode(options)))));
            }
        }
    }
}
