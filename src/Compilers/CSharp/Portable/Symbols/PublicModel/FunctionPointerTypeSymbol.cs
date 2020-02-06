// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class FunctionPointerTypeSymbol : TypeSymbol, IFunctionPointerTypeSymbol
    {
        private readonly Symbols.FunctionPointerTypeSymbol _underlying;

        public FunctionPointerTypeSymbol(Symbols.FunctionPointerTypeSymbol underlying, CodeAnalysis.NullableAnnotation nullableAnnotation)
            : base(nullableAnnotation)
        {
            RoslynDebug.Assert(underlying is object);
            _underlying = underlying;
        }

        public IMethodSymbol Signature => _underlying.Signature.GetPublicSymbol();
        internal override Symbols.TypeSymbol UnderlyingTypeSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        protected override void Accept(SymbolVisitor visitor)
            => visitor.VisitFunctionPointerType(this);

        [return: MaybeNull]
        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitFunctionPointerType(this);
        }

        protected override ITypeSymbol WithNullableAnnotation(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != this.NullableAnnotation);
            Debug.Assert(nullableAnnotation != _underlying.DefaultNullableAnnotation);
            return new FunctionPointerTypeSymbol(_underlying, nullableAnnotation);
        }
    }
}
