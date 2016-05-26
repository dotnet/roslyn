// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class ModuleSymbolKey : AbstractSymbolKey<ModuleSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _containerKey;
            [JsonProperty] private readonly string _metadataName;

            [JsonConstructor]
            internal ModuleSymbolKey(SymbolKey _containerKey, string _metadataName)
            {
                this._containerKey = _containerKey;
                this._metadataName = _metadataName;
            }

            internal ModuleSymbolKey(IModuleSymbol symbol, Visitor visitor)
            {
                _containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                _metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);

                // Don't check ModuleIds for equality because in practice, no-one uses them,
                // and there is no way to set netmodule name programmatically using Roslyn
                var modules = GetAllSymbols<IAssemblySymbol>(container).SelectMany(a => a.Modules);

                return CreateSymbolInfo(modules);
            }

            internal override bool Equals(ModuleSymbolKey other, ComparisonOptions options)
            {
                return other._containerKey.Equals(_containerKey, options);
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