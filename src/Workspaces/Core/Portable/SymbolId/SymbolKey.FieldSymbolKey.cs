// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class FieldSymbolKey : AbstractSymbolKey<FieldSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;

            internal FieldSymbolKey(IFieldSymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingType, visitor);
                this.metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var containerInfo = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var fields = GetAllSymbols<INamedTypeSymbol>(containerInfo).SelectMany(t => t.GetMembers(this.metadataName)).OfType<IFieldSymbol>();
                return CreateSymbolInfo(fields);
            }

            internal override bool Equals(FieldSymbolKey other, ComparisonOptions options)
            {
                return
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    other.containerKey.Equals(this.containerKey, options);
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