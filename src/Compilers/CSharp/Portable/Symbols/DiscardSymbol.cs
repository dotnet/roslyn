// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class DiscardSymbol : Symbol, IDiscardSymbol
    {
        private readonly TypeSymbol _type;

        public DiscardSymbol(TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            _type = type;
        }

        ITypeSymbol IDiscardSymbol.Type => _type;
        public TypeSymbol Type => _type;

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
    }
}
