// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationAbstractNamedTypeSymbol : CodeGenerationTypeSymbol, INamedTypeSymbol
    {
        public new INamedTypeSymbol OriginalDefinition { get; protected set; }

        internal readonly IList<CodeGenerationAbstractNamedTypeSymbol> TypeMembers;

        protected CodeGenerationAbstractNamedTypeSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType,
            IList<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
            : base(containingType, attributes, declaredAccessibility, modifiers, name, specialType)
        {
            this.TypeMembers = typeMembers;

            foreach (var member in typeMembers)
            {
                member.ContainingType = this;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.NamedType;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        public INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments)
        {
            if (typeArguments.Length == 0)
            {
                return this;
            }

            return new CodeGenerationConstructedNamedTypeSymbol(this, typeArguments, this.TypeMembers);
        }

        public abstract int Arity { get; }
        public abstract bool IsGenericType { get; }
        public abstract bool IsUnboundGenericType { get; }
        public abstract bool IsScriptClass { get; }
        public abstract bool IsImplicitClass { get; }
        public abstract IEnumerable<string> MemberNames { get; }
        public abstract IMethodSymbol DelegateInvokeMethod { get; }
        public abstract INamedTypeSymbol EnumUnderlyingType { get; }
        public abstract INamedTypeSymbol ConstructedFrom { get; }
        public abstract INamedTypeSymbol ConstructUnboundGenericType();
        public abstract ImmutableArray<IMethodSymbol> InstanceConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> StaticConstructors { get; }
        public abstract ImmutableArray<IMethodSymbol> Constructors { get; }
        public abstract ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public abstract ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        public override string MetadataName
        {
            get
            {
                return this.Arity > 0
                    ? this.Name + "`" + Arity
                    : base.MetadataName;
            }
        }

        public ISymbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public bool MightContainExtensionMethods
        {
            get { return false; }
        }
    }
}
