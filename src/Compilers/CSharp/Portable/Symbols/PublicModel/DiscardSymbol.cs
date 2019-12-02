// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class DiscardSymbol : Symbol, IDiscardSymbol
    {
        private readonly Symbols.DiscardSymbol _underlying;
        private ITypeSymbol _lazyType;

        public DiscardSymbol(Symbols.DiscardSymbol underlying)
        {
            Debug.Assert(underlying != null);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        ITypeSymbol IDiscardSymbol.Type
        {
            get
            {
                if (_lazyType is null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _underlying.TypeWithAnnotations.GetPublicSymbol(), null);
                }

                return _lazyType;
            }
        }

        CodeAnalysis.NullableAnnotation IDiscardSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        protected override void Accept(SymbolVisitor visitor) => visitor.VisitDiscard(this);
        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitDiscard(this);
    }
}
