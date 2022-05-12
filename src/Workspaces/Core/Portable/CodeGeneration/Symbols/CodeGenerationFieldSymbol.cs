// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationFieldSymbol : CodeGenerationSymbol, IFieldSymbol
    {
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
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
            : base(containingType?.ContainingAssembly, containingType, attributes, accessibility, modifiers, name)
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
            => visitor.VisitField(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitField(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitField(this, argument);

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

        public int FixedSize => 0;

        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public ISymbol AssociatedSymbol => null;

        public bool IsExplicitlyNamedTupleElement => false;
    }
}
