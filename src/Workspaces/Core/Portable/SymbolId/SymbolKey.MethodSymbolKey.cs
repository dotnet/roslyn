// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class ReducedExtensionMethodSymbolKey : AbstractSymbolKey<ReducedExtensionMethodSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _reducedFrom;
            [JsonProperty] private readonly SymbolKey _receiverType;

            [JsonConstructor]
            internal ReducedExtensionMethodSymbolKey(
                SymbolKey _reducedFrom, SymbolKey _receiverType)
            {
                this._reducedFrom = _reducedFrom;
                this._receiverType = _receiverType;
            }

            internal ReducedExtensionMethodSymbolKey(IMethodSymbol symbol, Visitor visitor)
            {
                Debug.Assert(symbol.Equals(symbol.ConstructedFrom));

                _reducedFrom = GetOrCreate(symbol.ReducedFrom, visitor);
                _receiverType = GetOrCreate(symbol.ReceiverType, visitor);
            }

            internal override bool Equals(ReducedExtensionMethodSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);

                return _reducedFrom.Equals(other._reducedFrom, options) &&
                    _receiverType.Equals(other._receiverType, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(_reducedFrom.GetHashCode(options), _receiverType.GetHashCode(options));
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                var q = from m in _reducedFrom.Resolve(compilation, ignoreAssemblyKey, cancellationToken).GetAllSymbols().OfType<IMethodSymbol>()
                        from t in _receiverType.Resolve(compilation, ignoreAssemblyKey, cancellationToken).GetAllSymbols().OfType<ITypeSymbol>()
                        let r = m.ReduceExtensionMethod(t)
                        where r != null
                        select r;

                return CreateSymbolInfo(q.ToArray());
            }
        }
    }

    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class ConstructedMethodSymbolKey : AbstractSymbolKey<ConstructedMethodSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _constructedFrom;
            [JsonProperty] private readonly SymbolKey[] _typeArgumentKeys;

            [JsonConstructor]
            internal ConstructedMethodSymbolKey(
                SymbolKey _constructedFrom, SymbolKey[] _typeArgumentKeys)
            {
                this._constructedFrom = _constructedFrom;
                this._typeArgumentKeys = _typeArgumentKeys;
            }

            internal ConstructedMethodSymbolKey(IMethodSymbol symbol, Visitor visitor)
            {
                _constructedFrom = GetOrCreate(symbol.ConstructedFrom, visitor);
                _typeArgumentKeys = symbol.TypeArguments.Select(t => GetOrCreate(t, visitor)).ToArray();
            }

            internal override bool Equals(ConstructedMethodSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);

                return _constructedFrom.Equals(other._constructedFrom, options) &&
                    SequenceEquals(this._typeArgumentKeys, other._typeArgumentKeys, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return _constructedFrom.GetHashCode(options);
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                var typeArguments = _typeArgumentKeys.Select(a => a.Resolve(compilation, ignoreAssemblyKey, cancellationToken: cancellationToken).Symbol as ITypeSymbol).ToArray();
                if (typeArguments.Any(a => a == null))
                {
                    return default(SymbolKeyResolution);
                }

                var constructedFroms = _constructedFrom.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var constructeds = constructedFroms.GetAllSymbols()
                                                   .OfType<IMethodSymbol>()
                                                   .Select(m => m.Construct(typeArguments));

                return CreateSymbolInfo(constructeds.ToArray());
            }
        }
    }

    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class MethodSymbolKey : AbstractSymbolKey<MethodSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _containerKey;
            [JsonProperty] private readonly string _metadataName;
            [JsonProperty] private readonly RefKind[] _refKinds;
            [JsonProperty] private readonly SymbolKey[] _originalParameterTypeKeys;
            [JsonProperty] private readonly bool _isPartialMethodImplementationPart;
            [JsonProperty] private readonly SymbolKey _returnType;

            /// <summary>
            /// There is a circularity between methods and their type parameters.  As such
            /// we may generate the type parameter when processing the method.  Or we may
            /// generate the method when processing the type parameter.  Because we may 
            /// be create before our contianing method is available, we may start out with
            /// a null containerKey.  However, once our containing method finishes being
            /// created it will set itself as our container.
            /// </summary>
            [JsonProperty] internal readonly MethodTypeParameterSymbolKey[] _typeParameterKeysOpt;

            [JsonConstructor]
            internal MethodSymbolKey(
                SymbolKey _containerKey, string _metadataName,
                MethodTypeParameterSymbolKey[] _typeParameterKeysOpt,
                RefKind[] _refKinds, 
                SymbolKey[] _originalParameterTypeKeys,
                bool _isPartialMethodImplementationPart, 
                SymbolKey _returnType)
            {
                this._containerKey = _containerKey;
                this._metadataName = _metadataName;
                this._typeParameterKeysOpt = _typeParameterKeysOpt;
                this._refKinds = _refKinds;
                this._originalParameterTypeKeys = _originalParameterTypeKeys;
                this._isPartialMethodImplementationPart = _isPartialMethodImplementationPart;
                this._returnType = _returnType;

                // Let our type parameters know about us in case they were created before we were.
                if (_typeParameterKeysOpt != null)
                {
                    foreach (var typeParameter in _typeParameterKeysOpt)
                    {
                        if (typeParameter != null)
                        {
                            typeParameter._containerKey = this;
                        }
                    }
                }
            }

            internal MethodSymbolKey(IMethodSymbol symbol, Visitor visitor)
            {
                Debug.Assert(symbol.Equals(symbol.ConstructedFrom));

                // First thing we do when creating the symbol ID for a method is create symbol IDs
                // for it's type parameters.  However, we pass ourselves to that type parameter so
                // that it does not recurse back to us.
                List<MethodTypeParameterSymbolKey> typeParameters = null;
                foreach (var typeParameter in symbol.TypeParameters)
                {
                    var symbolId = new MethodTypeParameterSymbolKey(this, typeParameter);

                    typeParameters = typeParameters ?? new List<MethodTypeParameterSymbolKey>();
                    typeParameters.Add(symbolId);

                    visitor.SymbolCache.Add(typeParameter, symbolId);
                }

                _typeParameterKeysOpt = typeParameters?.ToArray();

                _isPartialMethodImplementationPart = symbol.PartialDefinitionPart != null;

                _containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                _metadataName = symbol.MetadataName;
                _refKinds = symbol.Parameters.Select(p => p.RefKind).ToArray();
                _originalParameterTypeKeys = symbol.OriginalDefinition.Parameters.Select(p => GetOrCreate(p.Type, visitor)).ToArray();

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

            private int Arity => _typeParameterKeysOpt?.Length ?? 0;

            private IEnumerable<IMethodSymbol> Resolve(Compilation compilation, INamedTypeSymbol container, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var comparisonOptions = new ComparisonOptions(compilation.IsCaseSensitive, ignoreAssemblyKey, compareMethodTypeParametersByName: true);

                foreach (var method in container.GetMembers().OfType<IMethodSymbol>())
                {
                    // Quick checks first
                    if (method.MetadataName != _metadataName || method.Parameters.Length != _originalParameterTypeKeys.Length)
                    {
                        continue;
                    }

                    if (method.Arity != this.Arity)
                    {
                        continue;
                    }

                    // Is this a conversion operator? If so, we must also compare the return type.
                    if (_returnType != null)
                    {
                        if (!_returnType.Equals(SymbolKey.Create(method.ReturnType, cancellationToken), comparisonOptions))
                        {
                            continue;
                        }
                    }

                    if (!ParametersMatch(comparisonOptions, method.OriginalDefinition.Parameters, _refKinds, _originalParameterTypeKeys, cancellationToken))
                    {
                        continue;
                    }

                    // It matches, so let's return it, but we might have to do some construction first
                    var methodToReturn = method;

                    if (_isPartialMethodImplementationPart)
                    {
                        methodToReturn = methodToReturn.PartialImplementationPart ?? methodToReturn;
                    }

                    yield return methodToReturn;
                }
            }

            internal override bool Equals(MethodSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);
                return
                    other._refKinds.Length == _refKinds.Length &&
                    other._isPartialMethodImplementationPart == _isPartialMethodImplementationPart &&
                    other.Arity == this.Arity &&
                    Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    other._containerKey.Equals(_containerKey, options) &&
                    other._refKinds.SequenceEqual(_refKinds) &&
                    other._originalParameterTypeKeys.SequenceEqual(_originalParameterTypeKeys, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO: Consider hashing the parameters as well
                return
                    Hash.Combine(this.Arity,
                    Hash.Combine(_refKinds.Length,
                    Hash.Combine(_isPartialMethodImplementationPart,
                    Hash.Combine(GetHashCode(options.IgnoreCase, _metadataName),
                                 _containerKey.GetHashCode(options)))));
            }
        }
    }
}