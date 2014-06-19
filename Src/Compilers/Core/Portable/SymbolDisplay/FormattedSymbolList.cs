// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis
{
    internal sealed class FormattedSymbolList : IMessageSerializable
    {
        private readonly IEnumerable<ISymbol> symbols;
        private readonly SymbolDisplayFormat symbolDisplayFormat;

        internal FormattedSymbolList(IEnumerable<ISymbol> symbols, SymbolDisplayFormat symbolDisplayFormat = null)
        {
            Debug.Assert(symbols != null);

            this.symbols = symbols;
            this.symbolDisplayFormat = symbolDisplayFormat;
        }

        public override string ToString()
        {
            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;

            bool first = true;
            foreach (var symbol in symbols)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(symbol.ToDisplayString(symbolDisplayFormat));
            }

            return pooled.ToStringAndFree();
        }
    }
}
