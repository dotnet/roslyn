// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class DiscardSymbol : Symbol
    {
        public DiscardSymbol(TypeWithAnnotations typeWithAnnotations)
        {
            Debug.Assert(typeWithAnnotations.Type is object);
            TypeWithAnnotations = typeWithAnnotations;
        }

        public TypeWithAnnotations TypeWithAnnotations { get; }

        public override Symbol? ContainingSymbol => null;
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
        public override int LocationsCount => SymbolLocationHelper.Empty.LocationsCount;
        public override Location GetCurrentLocation(int slot, int index) => SymbolLocationHelper.Empty.GetCurrentLocation(slot, index);
        public override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex) => SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex);
        public override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex) => SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex);
        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitDiscard(this, a);
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitDiscard(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitDiscard(this);

        public override bool Equals(Symbol? obj, TypeCompareKind compareKind) => obj is DiscardSymbol other && this.TypeWithAnnotations.Equals(other.TypeWithAnnotations, compareKind);
        public override int GetHashCode() => this.TypeWithAnnotations.GetHashCode();

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.DiscardSymbol(this);
        }
    }
}
