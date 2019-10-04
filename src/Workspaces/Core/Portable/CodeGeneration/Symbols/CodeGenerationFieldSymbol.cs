// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

        public IFieldSymbol? CorrespondingTupleField => null;

        public override SymbolKind Kind => SymbolKind.Field;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitField(this);
        }

        [return: MaybeNull]
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return visitor.VisitField(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
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

        public ISymbol? AssociatedSymbol => null;
    }
}
