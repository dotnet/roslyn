// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class MethodTypeParameterSymbolKey : AbstractSymbolKey<MethodTypeParameterSymbolKey>
        {
            /// <summary>
            /// There is a circularity between methods and their type parameters.  As such
            /// we may generate the type parameter when processing the method.  Or we may
            /// generate the method when processing the type parameter.  Because we may 
            /// be created before our containing method is available, we may start out with
            /// a null containerKey.  However, once our containing method finishes being
            /// created it will set itself as our container.
            /// </summary>
            [JsonProperty] internal MethodSymbolKey _containerKey;

            [JsonProperty] private readonly string _metadataName;
            [JsonProperty] private readonly int _ordinal;

            [JsonConstructor]
            internal MethodTypeParameterSymbolKey(
                MethodSymbolKey _containerKey, string _metadataName, int _ordinal)
            {
                this._containerKey = _containerKey;
                this._metadataName = _metadataName;
                this._ordinal = _ordinal;

                // Let out method know about us in case it was created before we were.
                if (_containerKey != null && _containerKey._typeParameterKeysOpt != null)
                {
                    _containerKey._typeParameterKeysOpt[_ordinal] = this;
                }
            }

            public MethodTypeParameterSymbolKey(
                MethodSymbolKey containerKey, ITypeParameterSymbol symbol)
            {
                _containerKey = containerKey;
                _metadataName = symbol.MetadataName;
                _ordinal = symbol.Ordinal;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var typeParameters = GetAllSymbols<IMethodSymbol>(container).SelectMany(s => Resolve(compilation, s));
                return CreateSymbolInfo(typeParameters);
            }

            private IEnumerable<ITypeParameterSymbol> Resolve(Compilation compilation, IMethodSymbol container)
            {
                return container.TypeParameters.Where(t => Equals(compilation, t.MetadataName, _metadataName));
            }

            internal override bool Equals(MethodTypeParameterSymbolKey other, ComparisonOptions options)
            {
                return Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    (options.CompareMethodTypeParametersByName || other._containerKey.Equals(_containerKey, options));
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                     GetHashCode(options.IgnoreCase, _metadataName),
                     _containerKey.GetHashCode(options));
            }
        }
    }
}