// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class LabelSymbol : Symbol, ILabelSymbol
    {
        private readonly Symbols.LabelSymbol _underlying;

        public LabelSymbol(Symbols.LabelSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        IMethodSymbol ILabelSymbol.ContainingMethod
        {
            get
            {
                return _underlying.ContainingMethod.GetPublicSymbol();
            }
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitLabel(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitLabel(this);
        }

        #endregion
    }
}
