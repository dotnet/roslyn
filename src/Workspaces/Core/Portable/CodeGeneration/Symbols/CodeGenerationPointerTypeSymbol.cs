// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPointerTypeSymbol : CodeGenerationTypeSymbol, IPointerTypeSymbol
    {
        public ITypeSymbol PointedAtType { get; }

        public CodeGenerationPointerTypeSymbol(ITypeSymbol pointedAtType)
            : base(null, default, Accessibility.NotApplicable, default, string.Empty, SpecialType.None)
        {
            this.PointedAtType = pointedAtType;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationPointerTypeSymbol(this.PointedAtType);
        }

        public override TypeKind TypeKind => TypeKind.Pointer;

        public override SymbolKind Kind => SymbolKind.PointerType;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitPointerType(this);
        }

        [return: MaybeNull]
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return visitor.VisitPointerType(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }
    }
}
