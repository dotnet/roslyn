// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPointerTypeSymbol : CodeGenerationTypeSymbol, IPointerTypeSymbol
    {
        public ITypeSymbol PointedAtType { get; }

        public CodeGenerationPointerTypeSymbol(ITypeSymbol pointedAtType)
            : base(null, default(ImmutableArray<AttributeData>), Accessibility.NotApplicable, default(DeclarationModifiers), string.Empty, SpecialType.None)
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

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitPointerType(this);
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
