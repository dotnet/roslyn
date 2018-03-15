// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class FormattedSymbolList : IFormattable
    {
        private readonly IEnumerable<ISymbol> _symbols;
        private readonly SymbolDisplayFormat _symbolDisplayFormat;

        internal FormattedSymbolList(IEnumerable<ISymbol> symbols, SymbolDisplayFormat symbolDisplayFormat = null)
        {
            Debug.Assert(symbols != null);

            _symbols = symbols;
            _symbolDisplayFormat = symbolDisplayFormat;
        }

        public override string ToString()
        {
            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;

            bool first = true;
            foreach (var symbol in _symbols)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(symbol.ToDisplayString(_symbolDisplayFormat));
            }

            return pooled.ToStringAndFree();
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }
    }
}
