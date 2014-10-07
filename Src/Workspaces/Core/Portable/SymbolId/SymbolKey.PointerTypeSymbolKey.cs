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
        private class PointerTypeSymbolKey : AbstractSymbolKey<PointerTypeSymbolKey>
        {
            private readonly SymbolKey pointedAtKey;

            public PointerTypeSymbolKey(IPointerTypeSymbol symbol, Visitor visitor)
            {
                this.pointedAtKey = GetOrCreate(symbol.PointedAtType, visitor);
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var elementInfo = pointedAtKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(elementInfo).Select(compilation.CreatePointerTypeSymbol));
            }

            internal override bool Equals(PointerTypeSymbolKey other, ComparisonOptions options)
            {
                return other.pointedAtKey.Equals(this.pointedAtKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(1, this.pointedAtKey.GetHashCode(options));
            }
        }
    }
}