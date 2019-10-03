// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class DiscardSymbol : Symbol, IDiscardSymbol
    {
        public DiscardSymbol(TypeWithAnnotations typeWithAnnotations)
        {
            Debug.Assert(typeWithAnnotations.Type is object);
            TypeWithAnnotations = typeWithAnnotations;
        }

        ITypeSymbol IDiscardSymbol.Type => TypeWithAnnotations.Type;
        CodeAnalysis.NullableAnnotation IDiscardSymbol.NullableAnnotation => TypeWithAnnotations.ToPublicAnnotation();
        public TypeWithAnnotations TypeWithAnnotations { get; }

        public override Symbol ContainingSymbol => null;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override bool IsAbstract => false;
        public override bool IsExtern => false;
        public override bool IsImplicitlyDeclared => true;
        public override bool IsOverride => false;
        public override bool IsSealed => false;
        public override bool IsStatic => false;
        public override bool IsVirtual => false;
        public override SymbolKind Kind => SymbolKind.Discard;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        internal override ObsoleteAttributeData ObsoleteAttributeData => null;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitDiscard(this, a);
        public override void Accept(SymbolVisitor visitor) => visitor.VisitDiscard(this);
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitDiscard(this);
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitDiscard(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitDiscard(this);

        public override bool Equals(Symbol obj, TypeCompareKind compareKind) => obj is DiscardSymbol other && this.TypeWithAnnotations.Equals(other.TypeWithAnnotations, compareKind);
        public override int GetHashCode() => this.TypeWithAnnotations.GetHashCode();
    }
}
