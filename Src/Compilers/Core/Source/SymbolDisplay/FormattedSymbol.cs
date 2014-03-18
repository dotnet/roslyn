// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This class associates a symbol with particular format for display.
    /// It can be passed as an argument for an error message in place where symbol display should go, 
    /// which allows to defer building strings and doing many other things (like loading metadata) 
    /// associated with that until the error message is actually requested.
    /// </summary>
    internal sealed class FormattedSymbol : IMessageSerializable
    {
        private readonly ISymbol symbol;
        private readonly SymbolDisplayFormat symbolDisplayFormat;

        internal FormattedSymbol(ISymbol symbol, SymbolDisplayFormat symbolDisplayFormat)
        {
            Debug.Assert(symbol != null && symbolDisplayFormat != null);

            this.symbol = symbol;
            this.symbolDisplayFormat = symbolDisplayFormat;
        }

        public override string ToString()
        {
            return symbol.ToDisplayString(symbolDisplayFormat);
        }
    }
}
