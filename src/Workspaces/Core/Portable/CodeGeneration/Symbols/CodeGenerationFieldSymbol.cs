// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationFieldSymbol : CodeGenerationSymbol, IFieldSymbol
    {
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.GetNullability();
        public object ConstantValue { get; }
        public bool HasConstantValue { get; }

        public CodeGenerationFieldSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            string name,
            bool hasConstantValue,
            object constantValue)
            : base(containingType, attributes, accessibility, modifiers, name)
        {
            this.Type = type;
            this.HasConstantValue = hasConstantValue;
            this.ConstantValue = constantValue;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationFieldSymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.Type, this.Name, this.HasConstantValue, this.ConstantValue);
        }

        public new IFieldSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public IFieldSymbol CorrespondingTupleField => null;

        public override SymbolKind Kind => SymbolKind.Field;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitField(this);
        }

        public bool IsConst
        {
            get
            {
                return this.Modifiers.IsConst;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.Modifiers.IsReadOnly;
            }
        }

        public bool IsVolatile => false;

        public bool IsFixedSizeBuffer => false;

        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public ISymbol AssociatedSymbol => null;
    }
}
