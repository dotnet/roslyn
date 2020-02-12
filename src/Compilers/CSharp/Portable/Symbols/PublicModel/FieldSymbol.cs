﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class FieldSymbol : Symbol, IFieldSymbol
    {
        private readonly Symbols.FieldSymbol _underlying;
        private ITypeSymbol _lazyType;

        public FieldSymbol(Symbols.FieldSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        ISymbol IFieldSymbol.AssociatedSymbol
        {
            get
            {
                return _underlying.AssociatedSymbol.GetPublicSymbol();
            }
        }

        ITypeSymbol IFieldSymbol.Type
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

        CodeAnalysis.NullableAnnotation IFieldSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        ImmutableArray<CustomModifier> IFieldSymbol.CustomModifiers
        {
            get { return _underlying.TypeWithAnnotations.CustomModifiers; }
        }

        IFieldSymbol IFieldSymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        IFieldSymbol IFieldSymbol.CorrespondingTupleField
        {
            get
            {
                return _underlying.CorrespondingTupleField.GetPublicSymbol();
            }
        }

        bool IFieldSymbol.IsConst => _underlying.IsConst;

        bool IFieldSymbol.IsReadOnly => _underlying.IsReadOnly;

        bool IFieldSymbol.IsVolatile => _underlying.IsVolatile;

        bool IFieldSymbol.IsFixedSizeBuffer => _underlying.IsFixedSizeBuffer;

        bool IFieldSymbol.HasConstantValue => _underlying.HasConstantValue;

        object IFieldSymbol.ConstantValue => _underlying.ConstantValue;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitField(this);
        }

        #endregion
    }
}
