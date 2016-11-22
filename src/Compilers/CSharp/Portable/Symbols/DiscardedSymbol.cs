// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class DiscardedSymbol : Symbol, IDiscardedSymbol
    {
        private readonly TypeSymbol _type;

        public DiscardedSymbol(TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            _type = type;
        }

        ITypeSymbol IDiscardedSymbol.Type => _type;
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
        public override SymbolKind Kind => SymbolKind.Discarded;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        internal override ObsoleteAttributeData ObsoleteAttributeData => null;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitDiscarded(this, a);
        public override void Accept(SymbolVisitor visitor) => visitor.VisitDiscarded(this);
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitDiscarded(this);
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitDiscarded(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitDiscarded(this);

        // Need to figure out the correct behavior for the following methods.
        // Tracked by https://github.com/dotnet/roslyn/issues/15449
        //public override string GetDocumentationCommentId() => TODO;
        //public override string GetDocumentationCommentXml(CultureInfo preferredCulture, bool expandIncludes, CancellationToken cancellationToken) => TODO;
    }
}
