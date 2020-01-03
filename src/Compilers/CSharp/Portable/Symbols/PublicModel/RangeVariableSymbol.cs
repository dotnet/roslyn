// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class RangeVariableSymbol : Symbol, IRangeVariableSymbol
    {
        private readonly Symbols.RangeVariableSymbol _underlying;

        public RangeVariableSymbol(Symbols.RangeVariableSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitRangeVariable(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitRangeVariable(this);
        }
    }
}
