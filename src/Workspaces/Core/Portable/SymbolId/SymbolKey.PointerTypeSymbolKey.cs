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
        private class PointerTypeSymbolKey : AbstractSymbolKey<PointerTypeSymbolKey>
        {
            private readonly SymbolKey _pointedAtKey;

            public PointerTypeSymbolKey(IPointerTypeSymbol symbol, Visitor visitor)
            {
                _pointedAtKey = GetOrCreate(symbol.PointedAtType, visitor);
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var elementInfo = _pointedAtKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(elementInfo).Select(compilation.CreatePointerTypeSymbol));
            }

            internal override bool Equals(PointerTypeSymbolKey other, ComparisonOptions options)
            {
                return other._pointedAtKey.Equals(_pointedAtKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(1, _pointedAtKey.GetHashCode(options));
            }
        }
    }
}
