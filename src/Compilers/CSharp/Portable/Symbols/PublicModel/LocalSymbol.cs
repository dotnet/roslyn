// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class LocalSymbol : Symbol, ILocalSymbol
    {
        private readonly Symbols.LocalSymbol _underlying;
        private ITypeSymbol _lazyType;

        public LocalSymbol(Symbols.LocalSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        ITypeSymbol ILocalSymbol.Type
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

        CodeAnalysis.NullableAnnotation ILocalSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        bool ILocalSymbol.IsFunctionValue
        {
            get
            {
                return false;
            }
        }

        bool ILocalSymbol.IsConst => _underlying.IsConst;

        bool ILocalSymbol.IsRef => _underlying.IsRef;

        RefKind ILocalSymbol.RefKind => _underlying.RefKind;

        bool ILocalSymbol.HasConstantValue => _underlying.HasConstantValue;

        object ILocalSymbol.ConstantValue => _underlying.ConstantValue;

        bool ILocalSymbol.IsFixed => _underlying.IsFixed;

        #region ISymbol Members

        protected sealed override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitLocal(this);
        }

        protected sealed override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitLocal(this);
        }

        #endregion
    }
}
