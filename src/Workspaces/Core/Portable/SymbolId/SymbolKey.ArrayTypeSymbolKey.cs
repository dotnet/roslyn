// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly SymbolKey _elementKey;
            private readonly int _rank;

            internal ArrayTypeSymbolKey(IArrayTypeSymbol symbol, Visitor visitor)
            {
                _elementKey = GetOrCreate(symbol.ElementType, visitor);
                _rank = symbol.Rank;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var elementInfo = _elementKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(elementInfo).Select(s => compilation.CreateArrayTypeSymbol(s, _rank)));
            }

            internal override bool Equals(ArrayTypeSymbolKey other, ComparisonOptions options)
            {
                return
                    other._rank == _rank &&
                    other._elementKey.Equals(_elementKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    _rank,
                    _elementKey.GetHashCode(options));
            }
        }
    }
}
