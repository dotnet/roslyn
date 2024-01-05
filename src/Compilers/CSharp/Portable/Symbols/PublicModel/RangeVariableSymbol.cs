// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRangeVariable(this, argument);
        }
    }
}
