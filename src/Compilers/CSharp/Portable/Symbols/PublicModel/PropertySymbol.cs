// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class PropertySymbol : Symbol, IPropertySymbol
    {
        private readonly Symbols.PropertySymbol _underlying;
        private ITypeSymbol _lazyType;

        public PropertySymbol(Symbols.PropertySymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        bool IPropertySymbol.IsIndexer
        {
            get { return _underlying.IsIndexer; }
        }

        ITypeSymbol IPropertySymbol.Type
        {
            get
            {
                if (_lazyType is null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _underlying.TypeWithAnnotations.GetITypeSymbol(), null);
                }

                return _lazyType;
            }
        }

        CodeAnalysis.NullableAnnotation IPropertySymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        ImmutableArray<IParameterSymbol> IPropertySymbol.Parameters
        {
            get { return _underlying.Parameters.GetPublicSymbols(); }
        }

        IMethodSymbol IPropertySymbol.GetMethod
        {
            get { return _underlying.GetMethod.GetPublicSymbol(); }
        }

        IMethodSymbol IPropertySymbol.SetMethod
        {
            get { return _underlying.SetMethod.GetPublicSymbol(); }
        }

        IPropertySymbol IPropertySymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        IPropertySymbol IPropertySymbol.OverriddenProperty
        {
            get { return _underlying.OverriddenProperty.GetPublicSymbol(); }
        }

        ImmutableArray<IPropertySymbol> IPropertySymbol.ExplicitInterfaceImplementations
        {
            get { return _underlying.ExplicitInterfaceImplementations.GetPublicSymbols(); }
        }

        bool IPropertySymbol.IsReadOnly
        {
            get { return _underlying.IsReadOnly; }
        }

        bool IPropertySymbol.IsWriteOnly
        {
            get { return _underlying.IsWriteOnly; }
        }

        bool IPropertySymbol.IsWithEvents
        {
            get { return false; }
        }

        ImmutableArray<CustomModifier> IPropertySymbol.TypeCustomModifiers
        {
            get { return _underlying.TypeWithAnnotations.CustomModifiers; }
        }

        ImmutableArray<CustomModifier> IPropertySymbol.RefCustomModifiers
        {
            get { return _underlying.RefCustomModifiers; }
        }

        bool IPropertySymbol.ReturnsByRef => _underlying.ReturnsByRef;

        bool IPropertySymbol.ReturnsByRefReadonly => _underlying.ReturnsByRefReadonly;

        RefKind IPropertySymbol.RefKind => _underlying.RefKind;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitProperty(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitProperty(this);
        }

        #endregion
    }
}
