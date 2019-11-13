// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class ParameterSymbol : Symbol, IParameterSymbol
    {
        private readonly Symbols.ParameterSymbol _underlying;
        private ITypeSymbol _lazyType;

        public ParameterSymbol(Symbols.ParameterSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        ITypeSymbol IParameterSymbol.Type
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

        CodeAnalysis.NullableAnnotation IParameterSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        ImmutableArray<CustomModifier> IParameterSymbol.CustomModifiers
        {
            get { return _underlying.TypeWithAnnotations.CustomModifiers; }
        }

        ImmutableArray<CustomModifier> IParameterSymbol.RefCustomModifiers
        {
            get { return _underlying.RefCustomModifiers; }
        }

        IParameterSymbol IParameterSymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        RefKind IParameterSymbol.RefKind => _underlying.RefKind;

        bool IParameterSymbol.IsDiscard => _underlying.IsDiscard;

        bool IParameterSymbol.IsParams => _underlying.IsParams;

        bool IParameterSymbol.IsOptional => _underlying.IsOptional;

        bool IParameterSymbol.IsThis => _underlying.IsThis;

        int IParameterSymbol.Ordinal => _underlying.Ordinal;

        bool IParameterSymbol.HasExplicitDefaultValue => _underlying.HasExplicitDefaultValue;

        object IParameterSymbol.ExplicitDefaultValue => _underlying.ExplicitDefaultValue;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitParameter(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitParameter(this);
        }

        #endregion
    }
}
