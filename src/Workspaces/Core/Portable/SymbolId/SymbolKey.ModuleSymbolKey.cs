// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class ModuleSymbolKey : AbstractSymbolKey<ModuleSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;

            internal ModuleSymbolKey(IModuleSymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                this.metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);

                // Don't check ModuleIds for equality because in practice, no one uses them,
                // and there is no way to set netmodule name programmatically using Roslyn
                var modules = GetAllSymbols<IAssemblySymbol>(container).SelectMany(a => a.Modules);

                return CreateSymbolInfo(modules);
            }

            internal override bool Equals(ModuleSymbolKey other, ComparisonOptions options)
            {
                return other.containerKey.Equals(this.containerKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    GetHashCode(options.IgnoreCase, this.metadataName),
                    this.containerKey.GetHashCode(options));
            }
        }
    }
}