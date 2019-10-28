// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class PointerTypeSymbol : TypeSymbol, IPointerTypeSymbol
    {
        private readonly Symbols.PointerTypeSymbol _underlying;
        private ITypeSymbol _lazyPointedAtType;

        public PointerTypeSymbol(Symbols.PointerTypeSymbol underlying, CodeAnalysis.NullableAnnotation nullableAnnotation)
            : base(nullableAnnotation)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != _underlying.DefaultNullableAnnotation);
            Debug.Assert(nullableAnnotation != this.NullableAnnotation);
            return new PointerTypeSymbol(_underlying, nullableAnnotation);
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
        internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;

        ITypeSymbol IPointerTypeSymbol.PointedAtType
        {
            get
            {
                if (_lazyPointedAtType is null)
                {
                    Interlocked.CompareExchange(ref _lazyPointedAtType, _underlying.PointedAtTypeWithAnnotations.GetPublicSymbol(), null);
                }

                return _lazyPointedAtType;
            }
        }

        ImmutableArray<CustomModifier> IPointerTypeSymbol.CustomModifiers
        {
            get { return _underlying.PointedAtTypeWithAnnotations.CustomModifiers; }
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitPointerType(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitPointerType(this);
        }

        #endregion
    }
}
