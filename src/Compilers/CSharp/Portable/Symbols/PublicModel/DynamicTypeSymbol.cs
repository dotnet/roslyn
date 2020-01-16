// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class DynamicTypeSymbol : TypeSymbol, IDynamicTypeSymbol
    {
        private readonly Symbols.DynamicTypeSymbol _underlying;

        public DynamicTypeSymbol(Symbols.DynamicTypeSymbol underlying, CodeAnalysis.NullableAnnotation nullableAnnotation)
            : base(nullableAnnotation)
        {
            RoslynDebug.Assert(underlying is object);
            _underlying = underlying;
        }

        protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != _underlying.DefaultNullableAnnotation);
            Debug.Assert(nullableAnnotation != this.NullableAnnotation);
            return new DynamicTypeSymbol(_underlying, nullableAnnotation);
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitDynamicType(this);
        }

        [return: MaybeNull]
        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitDynamicType(this);
        }

        #endregion
    }
}
