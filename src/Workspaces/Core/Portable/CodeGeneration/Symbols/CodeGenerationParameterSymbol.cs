// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationParameterSymbol : CodeGenerationSymbol, IParameterSymbol
    {
        public RefKind RefKind { get; }
        public bool IsParams { get; }
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.GetNullability();
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
            : base(containingType, attributes, Accessibility.NotApplicable, new DeclarationModifiers(), name)
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

        public bool IsThis => false;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray.Create<CustomModifier>();

        public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray.Create<CustomModifier>();
    }
}
