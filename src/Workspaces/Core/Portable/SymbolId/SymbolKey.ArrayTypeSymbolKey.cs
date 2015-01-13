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
        private class ArrayTypeSymbolKey : AbstractSymbolKey<ArrayTypeSymbolKey>
        {
            private readonly SymbolKey elementKey;
            private readonly int rank;

            internal ArrayTypeSymbolKey(IArrayTypeSymbol symbol, Visitor visitor)
            {
                this.elementKey = GetOrCreate(symbol.ElementType, visitor);
                this.rank = symbol.Rank;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var elementInfo = elementKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(elementInfo).Select(s => compilation.CreateArrayTypeSymbol(s, rank)));
            }

            internal override bool Equals(ArrayTypeSymbolKey other, ComparisonOptions options)
            {
                return
                    other.rank == this.rank &&
                    other.elementKey.Equals(this.elementKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    this.rank,
                    this.elementKey.GetHashCode(options));
            }
        }
    }
}