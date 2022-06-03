// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationParameterSymbol : CodeGenerationSymbol, IParameterSymbol
    {
        public RefKind RefKind { get; }
        public bool IsParams { get; }
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
        public bool IsOptional { get; }
        public int Ordinal { get; }

        public bool HasExplicitDefaultValue { get; }
        public object ExplicitDefaultValue { get; }

        public CodeGenerationParameterSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            RefKind refKind,
            bool isParams,
            ITypeSymbol type,
            string name,
            bool isOptional,
            bool hasDefaultValue,
            object defaultValue)
            : base(containingType?.ContainingAssembly, containingType, attributes, Accessibility.NotApplicable, new DeclarationModifiers(), name)
        {
            this.RefKind = refKind;
            this.IsParams = isParams;
            this.Type = type;
            this.IsOptional = isOptional;
            this.HasExplicitDefaultValue = hasDefaultValue;
            this.ExplicitDefaultValue = defaultValue;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationParameterSymbol(
                this.ContainingType, this.GetAttributes(), this.RefKind,
                this.IsParams, this.Type, this.Name, this.IsOptional, this.HasExplicitDefaultValue,
                this.ExplicitDefaultValue);
        }

        public new IParameterSymbol OriginalDefinition => this;

        public override SymbolKind Kind => SymbolKind.Parameter;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitParameter(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitParameter(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitParameter(this, argument);

        public bool IsThis => false;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray.Create<CustomModifier>();

        public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray.Create<CustomModifier>();

        public bool IsDiscard => false;
    }
}
